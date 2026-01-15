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

        /// <summary>
        /// 현재 Command / ElevatorStatus / ModeCheck 값을 기준으로
        /// 엘리베이터로 보낼 프로토콜 문자열을 생성한다.
        /// </summary>
        private string SendingElevator_Control()
        {
            string sendMsg = string.Empty;

            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();

            if (elevator == null)
            {
                ProtocolLogger.Warn("[Elevator][Sending] ElevatorStatus 가 null 입니다. 모드 전환 명령을 전송하지 않습니다.");
                return string.Empty;
            }

            if (elevator.state == nameof(State.PAUSE))
            {
                switch (elevator.mode)
                {
                    case nameof(Mode.AGVMODE):
                        sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=01&Dest=00";
                        ModeCheck = false;
                        ProtocolLogger.Info("[Elevator][Sending][PAUSE] AGV Mode Change Messgae (Param=09, Data=01).");
                        break;

                    case nameof(Mode.NOTAGVMODE):
                        sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=00&Dest=00";
                        ModeCheck = false;
                        ProtocolLogger.Info("[Elevator][Sending][PAUSE] NOT AGV Mode Change Messgae (Param=09, Data=00).");
                        break;

                    default:
                        ProtocolLogger.Warn($"[Elevator] 알 수 없는 ElevatorStatus.mode 값: {elevator.mode}. 모드 전환 명령 생략.");
                        break;
                }
            }
            else
            {

                // ------------------------------------------------------------
                // [1] 1순위: Commands 에 PENDING 또는 REQUEST 상태의 명령이 있는 경우
                // ------------------------------------------------------------
                var command = _repository.Commands.GetAll().FirstOrDefault(c => c.state == nameof(CommandState.PENDING) || c.state == nameof(CommandState.REQUEST));

                if (command != null)
                {
                    EventLogger.Info($"[Elevator][Sending] Command 감지. Id={command.commnadId}, Action={command.actionName}, State={command.state}");

                    switch (command.actionName)
                    {
                        case nameof(CommandAction.DOOROPEN):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=05&Data=00&Dest=00";
                            ProtocolLogger.Info("[Elevator][Sending] DOOROPEN Messgae.");
                            break;

                        case nameof(CommandAction.DOORCLOSE):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=06&Data=00&Dest=00";
                            ProtocolLogger.Info("[Elevator][Sending] DOORCLOSE Messgae.");
                            break;

                        case nameof(CommandAction.CALL_B1F):
                        case nameof(CommandAction.GOTO_B1F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=01&Dest=01";
                            ProtocolLogger.Info("[Elevator][Sending] B1F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_1F):
                        case nameof(CommandAction.GOTO_1F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=02&Dest=02";
                            ProtocolLogger.Info("[Elevator][Sending] 1F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_2F):
                        case nameof(CommandAction.GOTO_2F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=03&Dest=03";
                            ProtocolLogger.Info("[Elevator][Sending] 2F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_3F):
                        case nameof(CommandAction.GOTO_3F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=04&Dest=04";
                            ProtocolLogger.Info("[Elevator][Sending] 3F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_4F):
                        case nameof(CommandAction.GOTO_4F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=05&Dest=05";
                            ProtocolLogger.Info("[Elevator][Sending] 4F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_5F):
                        case nameof(CommandAction.GOTO_5F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=06&Dest=06";
                            ProtocolLogger.Info("[Elevator][Sending] 5F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.CALL_6F):
                        case nameof(CommandAction.GOTO_6F):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=07&Dest=07";
                            ProtocolLogger.Info("[Elevator][Sending] 6F Call/Goto Messgae.");
                            break;

                        case nameof(CommandAction.AGVMODE):
                            // Param = 09 AGV운전 명령 , Data = 1이면 제어 , Dest = 00으로 고정
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=01&Dest=00";
                            ProtocolLogger.Info("[Elevator][Sending] AGV Mode Change Messgae (Data=01).");
                            break;

                        case nameof(CommandAction.NOTAGVMODE):
                            // Param = 09 AGV운전 명령 , Data = 0이면 비제어 , Dest = 00으로 고정
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=00&Dest=00";
                            ProtocolLogger.Info("[Elevator][Sending] NOT AGV Mode Change Messgae (Data=00).");
                            break;

                        default:
                            // 정의되지 않은 Action 이 들어온 경우
                            ProtocolLogger.Warn($"[Elevator][Sending] 정의되지 않은 CommandAction: {command.actionName}. 송신 명령 없음.");
                            break;
                    }

                    return sendMsg;
                }

                // ------------------------------------------------------------
                // [2] 2순위: Command 가 없을 때는 상태/모드 기반 처리
                //      - ModeCheck == false : 아직 모드 조회 안 함 → 상태 조회(Cmd=10) 요청
                //      - ModeCheck == true  : 이전에 상태 요청을 보냈고, 이번엔 DB 상태값 보고 모드 변경
                // ------------------------------------------------------------
                if (!ModeCheck)
                {
                    // 아직 모드 확인을 안한 상태 → 모드 조회 명령 전송
                    sendMsg = "Cmd=10&AId=1&DId=1";
                    ModeCheck = true;

                    ProtocolLogger.Info("[Elevator][Sending] default Status Messgae.");
                }
                else
                {
                    // ModeCheck == true 인 상태 → ElevatorStatus 테이블 기준으로 모드 전환

                    //ProtocolLogger.Info($"[Elevator] ElevatorStatus 확인. mode={status.mode}");

                    switch (elevator.mode)
                    {
                        case nameof(Mode.AGVMODE):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=01&Dest=00";
                            ModeCheck = false;
                            ProtocolLogger.Info("[Elevator][Sending] AGV Mode Change Messgae (Param=09, Data=01).");
                            break;

                        case nameof(Mode.NOTAGVMODE):
                            sendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=00&Dest=00";
                            ModeCheck = false;
                            ProtocolLogger.Info("[Elevator][Sending] NOT AGV Mode Change Messgae (Param=09, Data=00).");
                            break;

                        default:
                            ProtocolLogger.Warn($"[Elevator] 알 수 없는 ElevatorStatus.mode 값: {elevator.mode}. 모드 전환 명령 생략.");
                            break;
                    }
                }
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