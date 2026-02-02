using Common.Models;
using System.Text;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        /// <summary>
        /// 엘리베이터로 보낼 최종 byte[] 프레임 생성
        /// 1) Elevator 명령/상태 문자열 생성
        /// 2) 문자열 길이를 8자리 문자열로 붙임
        /// 3) ASCII byte[] 로 변환
        /// </summary>
        private byte[] MakeSendingData()
        {
            // [0] 송신 원문 문자열 생성
            string sendMsg = SendingElevator_Control();

            // [0-1] 방어 코드: 아무 메시지도 만들어지지 않은 경우
            if (string.IsNullOrEmpty(sendMsg))
            {
                // 상황에 따라 프로토콜 정의에 맞는 동작으로 바꿔도 됨
                // 여기서는 "아무 것도 안 보내지 않는 것"을 방지하기 위해
                // 상태조회 명령으로 대체
                EventLogger.Warn("[Elevator][MakeSendingData] 생성된 송신 메시지가 비어 있어 기본 상태조회 명령(Cmd=10)으로 대체합니다.");
                sendMsg = "Cmd=10&AId=1&DId=1";
            }

            // [1] "본문 길이(8자리 숫자) + 본문" 프레임 조립
            const string lengthFormat = "00000000";                   // 8자리 고정 길이
            string bodyLength = sendMsg.Length.ToString(lengthFormat); // ASCII 기준 문자열 길이
            string frame = bodyLength + sendMsg;                      // 최종 송신 문자열

            // EventLog (파일 로그 / 콘솔 로그 등)
            //EventLogger.Info($"[Elevator][{nameof(MakeSendingData)}] Create frame. Length={bodyLength}, Body=\"{sendMsg}\"");

            // [2] 문자열 → byte[] (ASCII) 변환
            return SendingServerStream(frame);
        }

        private bool ModeCheck = false;

        private string SendingElevator_Control()
        {
            // ============================================================
            // [Sending 정책(HOLD 포함, 레이블/goto 없음)]
            //
            // 0) ElevatorStatus 없으면 스킵
            // 1) PAUSE면 강제 DOOROPEN
            //
            // 2) 후보 Command(PENDING/REQUEST) 조회 후 우선순위:
            //    2-1) DOORCLOSE 최우선
            //         - CLOSE 보내기 전에 HOLD(OPEN_HOLD_*)가 있으면 먼저 COMPLETED 처리
            //    2-2) OPEN_HOLD_* 있으면 DOOROPEN 메시지를 계속 리턴(유지)
            //    2-3) MODECHANGE 우선
            //    2-4) 나머지 FIFO 1개
            //
            // 3) 후보가 없으면 ModeCheck 로직 수행
            //
            // 디버깅 포인트:
            // - CLOSE가 들어왔을 때 HOLD를 COMPLETED로 먼저 바꾸는지
            // - HOLD가 있을 때 매 tick DOOROPEN이 계속 선택되는지
            // - sendMsg empty가 발생하면 actionName 매핑 누락/오타
            // ============================================================

            string sendMsg = string.Empty;

            // ------------------------------------------------------------
            // 0) ElevatorStatus 조회
            // ------------------------------------------------------------
            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator == null)
            {
                EventLogger.Warn("[Sending][SKIP] ElevatorStatus is null");
                return string.Empty;
            }

            // ------------------------------------------------------------
            // 1) PAUSE면 강제 DOOROPEN
            // ------------------------------------------------------------
            if (elevator.state == nameof(State.PAUSE))
            {
                sendMsg = BuildSendMsgByActionName(nameof(CommandAction.PAUSEDOOROPEN));

                if (string.IsNullOrWhiteSpace(sendMsg))
                    EventLogger.Warn("[Sending][PAUSE] DOOROPEN msg empty (mapping fail)");

                return sendMsg;
            }

            // ------------------------------------------------------------
            // 2) 후보 Command 조회 (PENDING/REQUEST)
            // ------------------------------------------------------------
            var all = _repository.Commands.GetAll().OrderBy(c => c.createdAt).ThenBy(t => t.sequence).ToList();
            var runCommand = all.FirstOrDefault(r => r.state == nameof(CommandState.EXECUTING));
            var candidates = all.Where(c => c != null && (c.state == nameof(CommandState.PENDING) || c.state == nameof(CommandState.REQUEST))).OrderBy(c => c.createdAt).ThenBy(t => t.sequence).ToList();

            if (all != null && runCommand == null && candidates != null && candidates.Count > 0)
            {
                // ------------------------------------------------------------
                // 2-1) 층이동 최우선
                // ------------------------------------------------------------

                var sourceAndDestMove = candidates.FirstOrDefault(c =>
                 c.subType == nameof(SubType.SOURCEFLOOR) ||
                 c.subType == nameof(SubType.DESTINATIONFLOOR));

                if (sourceAndDestMove != null)
                {
                    sendMsg = BuildSendMsgByActionName(sourceAndDestMove.actionName);

                    if (string.IsNullOrWhiteSpace(sendMsg))
                    {
                        EventLogger.Warn(
                            $"[Sending][MODE][SKIP] sendMsg empty. cmdId={sourceAndDestMove.commnadId}, action={sourceAndDestMove.actionName}"
                        );
                        return string.Empty;
                    }

                    EventLogger.Info($"[Sending][MODE][OK] cmdId={sourceAndDestMove.commnadId}, action={sourceAndDestMove.actionName}");
                    return sendMsg;
                }

                // ------------------------------------------------------------
                // 2-2) HOLD 유지 최초 전송 (OPEN_HOLD_*)
                // ------------------------------------------------------------
                var holdCandidate_1 = all.FirstOrDefault(c =>
                    c.actionName == nameof(CommandAction.OPEN_HOLD_SOURCE) ||
                    c.actionName == nameof(CommandAction.OPEN_HOLD_DEST));

                if (holdCandidate_1 != null)
                {
                    sendMsg = BuildSendMsgByActionName(holdCandidate_1.actionName);

                    if (string.IsNullOrWhiteSpace(sendMsg))
                    {
                        EventLogger.Warn(
                            $"[Sending][HOLD][SKIP] sendMsg empty. holdId={holdCandidate_1.commnadId}, action={holdCandidate_1.actionName}"
                        );
                        return string.Empty;
                    }

                    EventLogger.Info($"[Sending][HOLD][OK] holdId={holdCandidate_1.commnadId}, action={holdCandidate_1.actionName}");
                    return sendMsg;
                }

                // ------------------------------------------------------------
                // 2-3) MODECHANGE 우선
                // ------------------------------------------------------------
                var mode = candidates.FirstOrDefault(c =>
                    c.actionName == nameof(CommandAction.AGVMODE) ||
                    c.actionName == nameof(CommandAction.NOTAGVMODE));

                if (mode != null)
                {
                    sendMsg = BuildSendMsgByActionName(mode.actionName);

                    if (string.IsNullOrWhiteSpace(sendMsg))
                    {
                        EventLogger.Warn(
                            $"[Sending][MODE][SKIP] sendMsg empty. cmdId={mode.commnadId}, action={mode.actionName}"
                        );
                        return string.Empty;
                    }

                    EventLogger.Info($"[Sending][MODE][OK] cmdId={mode.commnadId}, action={mode.actionName}");
                    return sendMsg;
                }

                // ------------------------------------------------------------
                // 2-4) 그 외 FIFO 1개
                // ------------------------------------------------------------
                var cmd = candidates.FirstOrDefault();
                if (cmd != null)
                {
                    sendMsg = BuildSendMsgByActionName(cmd.actionName);

                    if (string.IsNullOrWhiteSpace(sendMsg))
                    {
                        EventLogger.Warn(
                            $"[Sending][SKIP] sendMsg empty. cmdId={cmd.commnadId}, action={cmd.actionName}"
                        );
                        return string.Empty;
                    }

                    EventLogger.Info($"[Sending][OK] cmdId={cmd.commnadId}, action={cmd.actionName}");
                    return sendMsg;
                }
            }
            // ------------------------------------------------------------
            // 2-1) CLOSE 최우선
            // ------------------------------------------------------------
            var close = candidates.FirstOrDefault(c => c.actionName == nameof(CommandAction.DOORCLOSE));
            if (close != null)
            {
                // ------------------------------------------------------------
                // [중요] Sending에서는 상태(COMPLETED) 변경하지 않는다.
                // - HOLD 종료는 commmandCompleted()에서 doorClose 완료 이벤트 기준으로 처리한다.
                // - 여기서는 HOLD가 살아있는지 "조회/로그"만 남긴다.
                // ------------------------------------------------------------
                var holdAlive = all.FirstOrDefault(c =>
                    c != null
                    && (c.actionName == nameof(CommandAction.OPEN_HOLD_SOURCE)
                     || c.actionName == nameof(CommandAction.OPEN_HOLD_DEST))
                    && (c.state == nameof(CommandState.PENDING)
                     || c.state == nameof(CommandState.REQUEST)
                     || c.state == nameof(CommandState.EXECUTING))
                );

                if (holdAlive != null)
                {
                    EventLogger.Info(
                        $"[Sending][CLOSE] HOLD alive (will complete on protocol). " +
                        $"holdId={holdAlive.commnadId}, holdState={holdAlive.state}, closeId={close.commnadId}"
                    );
                }

                // (B) CLOSE 전송 메시지 생성
                sendMsg = BuildSendMsgByActionName(close.actionName);

                if (string.IsNullOrWhiteSpace(sendMsg))
                {
                    EventLogger.Warn($"[Sending][CLOSE][SKIP] sendMsg empty. closeId={close.commnadId}");
                    return string.Empty;
                }

                EventLogger.Info($"[Sending][CLOSE][OK] closeId={close.commnadId}");
                return sendMsg;
            }
            else
            {
                // ------------------------------------------------------------
                // 2-2) HOLD 유지 (OPEN_HOLD_*)
                // ------------------------------------------------------------

                var holdCandidate = all.FirstOrDefault(c => c.state == nameof(CommandState.EXECUTING)
                && (c.actionName == nameof(CommandAction.OPEN_HOLD_SOURCE) || c.actionName == nameof(CommandAction.OPEN_HOLD_DEST)));

                if (holdCandidate != null)
                {
                    sendMsg = BuildSendMsgByActionName(holdCandidate.actionName);

                    if (string.IsNullOrWhiteSpace(sendMsg))
                    {
                        EventLogger.Warn(
                            $"[Sending][HOLD][SKIP] sendMsg empty. holdId={holdCandidate.commnadId}, action={holdCandidate.actionName}"
                        );
                        return string.Empty;
                    }

                    EventLogger.Info($"[Sending][HOLD][OK] holdId={holdCandidate.commnadId}, action={holdCandidate.actionName}");
                    return sendMsg;
                }
            }
            // ------------------------------------------------------------
            // 3) 후보 Command가 없을 때 ModeCheck 로직
            // ------------------------------------------------------------
            if (ModeCheck == false)
            {
                sendMsg = BuildSendMsgByActionName(nameof(CommandAction.State));
                ModeCheck = true;
                return sendMsg;
            }

            if (elevator.mode == nameof(Mode.AGVMODE))
            {
                sendMsg = BuildSendMsgByActionName(nameof(CommandAction.AGVMODE));
                ModeCheck = false;
                return sendMsg;
            }

            if (elevator.mode == nameof(Mode.NOTAGVMODE))
            {
                sendMsg = BuildSendMsgByActionName(nameof(CommandAction.NOTAGVMODE));
                ModeCheck = false;
                return sendMsg;
            }

            EventLogger.Warn($"[Sending][DEFAULT] unknown elevator.mode={elevator.mode}. skip.");
            return string.Empty;
        }

        /// <summary>
        /// actionName -> 엘리베이터 송신 프로토콜 문자열 생성 (HOLD 포함 버전)
        ///
        /// [HOLD 정책]
        /// - OPEN_HOLD_SOURCE / OPEN_HOLD_DEST 는 DOOROPEN 프로토콜(Param=05)을 계속 전송하는 용도.
        /// - 즉 "메시지 생성"은 DOOROPEN과 동일.
        /// - 주기 전송은 상위 Timer/Thread가 이미 계속 tick을 돌고 있으니 여기서는 선택만 한다.
        ///
        /// 리턴:
        /// - 성공: "Cmd=20&..." 문자열
        /// - 실패: string.Empty
        /// </summary>
        private string BuildSendMsgByActionName(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                ProtocolLogger.Warn("[BuildSendMsg][SKIP] actionName empty");
                return string.Empty;
            }

            string sendMsg = string.Empty;

            switch (actionName)
            {
                case nameof(CommandAction.State):
                    sendMsg = "Cmd=10&AId=1&DId=1";
                    ProtocolLogger.Info("[BuildSendMsg] State.");
                    break;

                case nameof(CommandAction.DOOROPEN):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=05&Data=00&Dest=00";
                    ProtocolLogger.Info("[BuildSendMsg] DOOROPEN.");
                    break;

                case nameof(CommandAction.PAUSEDOOROPEN):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=05&Data=00&Dest=00";
                    ProtocolLogger.Info("[BuildSendMsg][Pause] DOOROPEN.");
                    break;

                case nameof(CommandAction.OPEN_HOLD_SOURCE):
                case nameof(CommandAction.OPEN_HOLD_DEST):
                    // ------------------------------------------------------------
                    // [HOLD 핵심]
                    // - 프로토콜은 DOOROPEN과 동일
                    // - 차이는 "커맨드 의미"만 다름 (계속 보내는 유지 용도)
                    // ------------------------------------------------------------
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=05&Data=00&Dest=00";
                    ProtocolLogger.Info($"[BuildSendMsg] OPEN_HOLD -> DOOROPEN keep sending. action={actionName}");
                    break;

                case nameof(CommandAction.DOORCLOSE):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=06&Data=00&Dest=00";
                    ProtocolLogger.Info("[BuildSendMsg] DOORCLOSE.");
                    break;

                case nameof(CommandAction.CALL_B1F):
                case nameof(CommandAction.GOTO_B1F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=01&Dest=01";
                    ProtocolLogger.Info("[BuildSendMsg] B1F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_1F):
                case nameof(CommandAction.GOTO_1F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=02&Dest=02";
                    ProtocolLogger.Info("[BuildSendMsg] 1F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_2F):
                case nameof(CommandAction.GOTO_2F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=03&Dest=03";
                    ProtocolLogger.Info("[BuildSendMsg] 2F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_3F):
                case nameof(CommandAction.GOTO_3F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=04&Dest=04";
                    ProtocolLogger.Info("[BuildSendMsg] 3F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_4F):
                case nameof(CommandAction.GOTO_4F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=05&Dest=05";
                    ProtocolLogger.Info("[BuildSendMsg] 4F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_5F):
                case nameof(CommandAction.GOTO_5F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=06&Dest=06";
                    ProtocolLogger.Info("[BuildSendMsg] 5F CALL/GOTO.");
                    break;

                case nameof(CommandAction.CALL_6F):
                case nameof(CommandAction.GOTO_6F):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=07&Dest=07";
                    ProtocolLogger.Info("[BuildSendMsg] 6F CALL/GOTO.");
                    break;

                case nameof(CommandAction.AGVMODE):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=01&Dest=00";
                    ProtocolLogger.Info("[BuildSendMsg] AGVMODE (Data=01).");
                    break;

                case nameof(CommandAction.NOTAGVMODE):
                    sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=00&Dest=00";
                    ProtocolLogger.Info("[BuildSendMsg] NOTAGVMODE (Data=00).");
                    break;

                default:
                    ProtocolLogger.Warn($"[BuildSendMsg][NO_MAP] unknown actionName={actionName}");
                    break;
            }

            return sendMsg;
        }

        /// <summary>
        /// 프로토콜 문자열을 ASCII byte[] 로 변환
        /// (필요 시 디버그 로그로 일부 내용 출력)
        /// </summary>
        private byte[] SendingServerStream(string strBuffer) // Sending Data 변환
        {
            if (strBuffer == null)
            {
                strBuffer = string.Empty;
            }

            // 문자열 → ASCII byte[]
            byte[] sendData = Encoding.ASCII.GetBytes(strBuffer);

            // 로그: 길이와 앞부분만 찍기 (너무 길어지는 것 방지)
            string preview;

            if (strBuffer.Length > 100)
            {
                preview = strBuffer.Substring(0, 100) + "...";
            }
            else
            {
                preview = strBuffer;
            }

            //EventLogger.Debug("[Elevator] 송신 byte[] 생성 완료. ByteLength=" + sendData.Length + ", Preview=\"" + preview + "\"");

            return sendData;
        }
    }
}