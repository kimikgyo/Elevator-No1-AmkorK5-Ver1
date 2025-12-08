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
            };
            _repository.Commands.Add(command);
        }

        private void commandExcuting(ProtocolDto protocolDto)
        {
            var commands = _repository.Commands.GetAll().Where(c => c.state == nameof(CommandState.PENDING)).ToList();
            foreach (var command in commands)
            {
                bool basicCondition = protocolDto.Cmd == 21 && protocolDto.AId == 1 && protocolDto.Dld == 1 && protocolDto.Dir == 0;
                bool changeState = false;

                switch (command.actionName)
                {
                    case nameof(CommandAction.DOOROPEN):
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
                    case nameof(CommandAction.NOTAGVMODE):
                        //Param = 09 AGV운전 명령 , Data = 1이면 제어 , Dest = 00으로 고정
                        changeState = basicCondition && protocolDto.Param == 9 && protocolDto.Dest == 0;
                        break;
                }
                if (changeState)
                {
                    CommandStateUpdate(command.commnadId, nameof(CommandState.EXECUTING));
                    if (command.actionName == nameof(CommandAction.AGVMODE))
                    {
                        elevatorModeUpdate(nameof(Mode.AGVMODE));
                        CommandStateUpdate(command.commnadId, nameof(CommandState.COMPLETED));
                    }
                    else if (command.actionName == nameof(CommandAction.NOTAGVMODE))
                    {
                        elevatorModeUpdate(nameof(Mode.NOTAGVMODE));
                        //기본상태값에 프로토콜이없음.
                        CommandStateUpdate(command.commnadId, nameof(CommandState.COMPLETED));
                    }
                }
            }
        }

        private void commmandCompleted(ProtocolDto protocolDto)
        {
            var commands = _repository.Commands.GetAll().Where(c => c.state == nameof(CommandState.EXECUTING)).ToList();
            var doorOpenCommand = _repository.Commands.GetAll().FirstOrDefault(c => c.actionName == nameof(CommandAction.DOOROPEN));
            foreach (var command in commands)
            {
                bool basicCondition = (protocolDto.Status == 2 || protocolDto.Status == 9) && protocolDto.Cmd == 11 && protocolDto.AId == 1 && protocolDto.Dld == 1;
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
                        changeState = doorClose;
                        break;

                    case nameof(CommandAction.CALL_B1F):
                    case nameof(CommandAction.GOTO_B1F):
                        if (protocolDto.Floor == 1)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;

                    case nameof(CommandAction.CALL_1F):
                    case nameof(CommandAction.GOTO_1F):
                        if (protocolDto.Floor == 2)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }
                        break;

                    case nameof(CommandAction.CALL_2F):
                    case nameof(CommandAction.GOTO_2F):
                        if (protocolDto.Floor == 3)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;

                    case nameof(CommandAction.CALL_3F):
                    case nameof(CommandAction.GOTO_3F):
                        if (protocolDto.Floor == 4)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;

                    case nameof(CommandAction.CALL_4F):
                    case nameof(CommandAction.GOTO_4F):
                        if (protocolDto.Floor == 5)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;

                    case nameof(CommandAction.CALL_5F):
                    case nameof(CommandAction.GOTO_5F):
                        if (protocolDto.Floor == 6)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;

                    case nameof(CommandAction.CALL_6F):
                    case nameof(CommandAction.GOTO_6F):
                        if (protocolDto.Floor == 7)
                        {
                            createDoorOpen = doorClose && doorOpenCommand == null;
                            if (doorOpen) changeState = true;
                        }

                        break;
                }
                if (createDoorOpen)
                {
                    CreateCommand(nameof(SubType.DOOROPEN), nameof(CommandAction.DOOROPEN), nameof(CommandState.PENDING));
                }
                else if (changeState)
                {
                    CommandStateUpdate(command.commnadId, nameof(CommandState.COMPLETED));
                }
            }
        }
    }
}