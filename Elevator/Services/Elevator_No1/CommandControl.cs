using Common.Dtos;
using Common.Models;
using System.Text.Json;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        private void CreateCommand(string subType, string parameterValue, string state)
        {
            var param = new Parameter
            {
                key = "Action",
                value = parameterValue.ToString()
            };
            var command = new Command
            {
                commnadId = $"ElevatorNo1_{Guid.NewGuid().ToString()}",
                name = parameterValue.ToString(),
                type = "ACTION",
                subType = subType.ToString(),
                state = state.ToString(),
                WorkerId = "",
                actionName = parameterValue.ToString(),
                parameterJson = JsonSerializer.Serialize(param),
                createdAt = DateTime.Now
            };
            _repository.Commands.Add(command);
        }

        private void commandExcuting(ProtocolDto protocolDto)
        {
            // ============================================================
            // [목적]
            // - 엘리베이터로부터 "요청 수신/ACK" 성격의 프로토콜을 받았을 때
            //   해당 Command를 EXECUTING으로 전환한다.
            //
            // [대상 상태]
            // - PENDING / REQUEST 만 대상 (Sending에서 올린 후보군과 동일)
            //
            // [HOLD 정책]
            // - OPEN_HOLD_SOURCE / OPEN_HOLD_DEST 는 DOOROPEN(Param=5) ACK를 받으면 EXECUTING으로 전환
            // - HOLD의 COMPLETED 처리는 여기서 하지 않는다.
            //   (DoorClose 완료 이벤트(commmandCompleted)에서 HOLD를 같이 종료)
            //
            // 디버깅 포인트
            // 1) protocolDto가 어떤 이벤트에서 들어오는지(Cmd/Status/Param/Dest) 로그로 확인
            // 2) command.actionName별로 changeState가 true가 되는 조건이 정확한지
            // ============================================================

            var all = _repository.Commands.GetAll();
            if (all == null) return;

            var commands = all.Where(c => c != null && (c.state == nameof(CommandState.PENDING) || c.state == nameof(CommandState.REQUEST))).ToList();

            if (commands == null || commands.Count == 0)
                return;

            // ------------------------------------------------------------
            // [기본 ACK 조건]
            // - Cmd=21 ACK 이벤트에 대해서만 EXECUTING으로 올린다.
            // - Status 조건이 ACK를 의미한다면 여기에 추가하는 것이 안전하다.
            // ------------------------------------------------------------
            bool basicCondition = protocolDto.Cmd == 21 && protocolDto.AId == 1 && protocolDto.Dld == 1 && protocolDto.Dir == 0;

            // 필요하다면(프로토콜 정의에 맞으면) 아래 Status 제한도 추가 권장
            // basicCondition = basicCondition && (protocolDto.Status == 2 || protocolDto.Status == 9);

            foreach (var command in commands)
            {
                if (command == null) continue;

                bool changeState = false;

                // ------------------------------------------------------------
                // actionName 별 ACK 매칭
                // ------------------------------------------------------------
                switch (command.actionName)
                {
                    case nameof(CommandAction.DOOROPEN):
                    case nameof(CommandAction.OPEN_HOLD_SOURCE):
                    case nameof(CommandAction.OPEN_HOLD_DEST):
                        changeState = basicCondition && protocolDto.Param == 5 && protocolDto.Dest == 0;
                        break;

                    case nameof(CommandAction.DOORCLOSE):
                        changeState = basicCondition && protocolDto.Param == 6 && protocolDto.Dest == 0;
                        break;

                    case nameof(CommandAction.CALL_B1F):
                    case nameof(CommandAction.GOTO_B1F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 1;
                        break;

                    case nameof(CommandAction.CALL_1F):
                    case nameof(CommandAction.GOTO_1F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 2;
                        break;

                    case nameof(CommandAction.CALL_2F):
                    case nameof(CommandAction.GOTO_2F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 3;
                        break;

                    case nameof(CommandAction.CALL_3F):
                    case nameof(CommandAction.GOTO_3F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 4;
                        break;

                    case nameof(CommandAction.CALL_4F):
                    case nameof(CommandAction.GOTO_4F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 5;
                        break;

                    case nameof(CommandAction.CALL_5F):
                    case nameof(CommandAction.GOTO_5F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 6;
                        break;

                    case nameof(CommandAction.CALL_6F):
                    case nameof(CommandAction.GOTO_6F):
                        changeState = basicCondition && protocolDto.Param == 3 && protocolDto.Dest == 7;
                        break;

                    case nameof(CommandAction.AGVMODE):
                    case nameof(CommandAction.AGVMODE_CHANGING_NOTAGVMODE):
                    case nameof(CommandAction.NOTAGVMODE):
                    case nameof(CommandAction.NOTAGVMODE_CHANGING_AGVMODE):
                        changeState = basicCondition && protocolDto.Param == 9 && protocolDto.Dest == 0;
                        break;
                }

                if (!changeState)
                    continue;

                // ------------------------------------------------------------
                // EXECUTING 전환
                // - 디버깅: 어떤 ACK로 어떤 커맨드가 전환되었는지
                // ------------------------------------------------------------
                EventLogger.Info($"[Executing] ACK matched. cmdId={command.commnadId}, action={command.actionName}, prevState={command.state},proto(Cmd={protocolDto.Cmd}," +
                                 $"Param={protocolDto.Param},Dest={protocolDto.Dest},Status={protocolDto.Status})");

                CommandStateUpdate(command.commnadId, nameof(CommandState.EXECUTING));

                // ------------------------------------------------------------
                // MODECHANGE 계열은 즉시 완료 처리(너 정책 유지)
                // ------------------------------------------------------------
                string mode = null;
                switch (command.actionName)
                {
                    case nameof(CommandAction.AGVMODE):
                        mode = nameof(Mode.AGVMODE);
                        break;

                    case nameof(CommandAction.NOTAGVMODE):
                        mode = nameof(Mode.NOTAGVMODE);
                        break;

                    case nameof(CommandAction.AGVMODE_CHANGING_NOTAGVMODE):
                        mode = nameof(Mode.AGVMODE_CHANGING_NOTAGVMODE);
                        break;

                    case nameof(CommandAction.NOTAGVMODE_CHANGING_AGVMODE):
                        mode = nameof(Mode.NOTAGVMODE_CHANGING_AGVMODE);
                        break;
                }

                if (mode != null)
                {
                    elevatorModeUpdate(mode);
                    CommandStateUpdate(command.commnadId, nameof(CommandState.COMPLETED));
                }
            }
        }

        private void commmandCompleted(ProtocolDto protocolDto)
        {
            // ------------------------------------------------------------
            // [목적]
            // - EXECUTING 상태 Command 들 중,
            //   프로토콜(ProtocolDto)로 "완료 조건"이 만족되면 COMPLETED로 바꾼다.
            //
            // [추가 정책 - HOLD 종료]
            // - DoorClose 완료 이벤트가 들어오면
            //   OPEN_HOLD_SOURCE/DEST 가 EXECUTING이면 같이 COMPLETED로 종료한다.
            //
            // 디버깅 포인트:
            // 1) doorClose 이벤트가 들어왔는데 HOLD가 왜 남아있는지
            // 2) HOLD가 EXECUTING 상태인지, 상태 전이가 누락된 건 아닌지
            // ------------------------------------------------------------

            var all = _repository.Commands.GetAll();
            if (all == null) return;

            var commands = all.Where(c => c != null && c.state == nameof(CommandState.EXECUTING)).ToList();
            if (commands == null || commands.Count == 0) return;

            // 기존 로직 유지(doorOpen 자동 생성용)
            var doorOpenCommand = all.FirstOrDefault(c => c != null
                                                 && (c.actionName == nameof(CommandAction.DOOROPEN) || c.actionName == nameof(CommandAction.OPEN_HOLD_SOURCE) || c.actionName == nameof(CommandAction.OPEN_HOLD_DEST)));

            foreach (var command in commands)
            {
                if (command == null) continue;

                bool basicCondition =
                    (protocolDto.Status == 2 || protocolDto.Status == 9)
                    && protocolDto.Cmd == 11
                    && protocolDto.AId == 1
                    && protocolDto.Dld == 1;

                bool doorOpen = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 2;
                bool doorClose = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 3;

                bool changeState = false;
                bool createDoorOpen = false;

                switch (command.actionName)
                {
                    case nameof(CommandAction.DOOROPEN):
                        changeState = doorOpen;
                        break;

                    case nameof(CommandAction.DOORCLOSE):
                        // 다른 로봇과 겹쳤을시에 문이닫혀서 문제가 생김! 
                        //changeState = doorClose;
                        
                        //수정
                        // EXECUTING 중인 HOLD를 찾아 종료
                        var hold = all.FirstOrDefault(c => c != null && c.state == nameof(CommandState.EXECUTING) &&
                                                     (c.actionName == nameof(CommandAction.OPEN_HOLD_SOURCE) || c.actionName == nameof(CommandAction.OPEN_HOLD_DEST)));

                        if (hold != null)
                        {
                            EventLogger.Info($"[Completed][CLOSE] complete HOLD by doorClose done. holdId={hold.commnadId}, holdAction={hold.actionName}");
                            CommandStateUpdate(hold.commnadId, nameof(CommandState.COMPLETED));
                            changeState = true;
                        }
                        else
                        {
                            // 디버깅: CLOSE 완료인데 HOLD가 없으면 정상(이미 끝났거나 애초에 없거나)
                            changeState = true;
                            EventLogger.Info("[Completed][CLOSE] no EXECUTING HOLD found (ok).");
                        }
                        break;

                    case nameof(CommandAction.CALL_B1F):
                    case nameof(CommandAction.GOTO_B1F):
                        if (protocolDto.Floor == 1)
                        {
                            if(doorOpen || doorClose) changeState = true;
                            //if(doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_1F):
                    case nameof(CommandAction.GOTO_1F):
                        if (protocolDto.Floor == 2)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if(doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_2F):
                    case nameof(CommandAction.GOTO_2F):
                        if (protocolDto.Floor == 3)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if(doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_3F):
                    case nameof(CommandAction.GOTO_3F):
                        if (protocolDto.Floor == 4)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if(doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_4F):
                    case nameof(CommandAction.GOTO_4F):
                        if (protocolDto.Floor == 5)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if(doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_5F):
                    case nameof(CommandAction.GOTO_5F):
                        if (protocolDto.Floor == 6)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if (doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_6F):
                    case nameof(CommandAction.GOTO_6F):
                        if (protocolDto.Floor == 7)
                        {
                            if (doorOpen || doorClose) changeState = true;
                            //if (doorOpen) changeState = true;
                            //changeState = true;
                        }
                        break;
                    // HOLD는 완료 이벤트가 없다고 했으니
                    // 여기서 changeState 케이스로 넣지 않는다.
                    // (HOLD는 DoorClose 완료 시 같이 종료)
                    case nameof(CommandAction.OPEN_HOLD_SOURCE):
                    case nameof(CommandAction.OPEN_HOLD_DEST):
                        // do nothing
                        break;
                }
                if (changeState)
                {
                    // ------------------------------------------------------------
                    // 1) 현재 command COMPLETED 처리
                    // ------------------------------------------------------------
                    CommandStateUpdate(command.commnadId, nameof(CommandState.COMPLETED));

                    // ------------------------------------------------------------
                    // 2) [핵심 추가] DoorClose 완료 시 HOLD도 같이 종료
                    // - 이 로직을 여기(commmandCompleted)에 넣으면
                    //   "실제 문이 닫힌 완료 이벤트" 기준으로 HOLD가 종료되므로 정합성이 좋다.
                    // ------------------------------------------------------------
                    if (command.actionName == nameof(CommandAction.DOORCLOSE))
                    {
                    }
                }
            }
        }
    }
}