using Common.Dtos;
using Common.Models;
using System.Text;

namespace Elevator.Services
{
    public partial class MainService
    {
        private bool MakeRecvData(byte[] data)
        {
            string recvDataLength = string.Empty; //EnCodig Data
            int recvLengthIndex = 8;        //index 0~7까지 8개의 객체가 객체 수량임
            bool returnValue = false;
            string strBuffer = string.Empty;
            try
            {
                //Recv Data 객체 수량을 확인
                //Recv Data 중 index 0~7까지 8개의 객체가 객체 수량임
                recvDataLength += Encoding.ASCII.GetString(data, 0, recvLengthIndex);

                //Recv Data 객체 추출
                //index 7~n 객체 수량까지가 사용할 Data
                strBuffer += Encoding.ASCII.GetString(data, recvLengthIndex, Convert.ToInt32(recvDataLength));

                if (Convert.ToInt32(recvDataLength) == strBuffer.Length)
                {
                    RecvElevator_Control(strBuffer);
                    ReceivedProtocol(strBuffer);
                    returnValue = true;
                }
                else
                {
                    returnValue = false;
                }
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
            }
            return returnValue;
        }

        private void elevatorState(ProtocolDto protocolDto)
        {
            bool basicCondition = (protocolDto.Status == 2 || protocolDto.Status == 9) && protocolDto.Cmd == 11 && protocolDto.Aid == 1 && protocolDto.Dld == 1;
            bool doorOpen = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 2;
            bool doorClose = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 3;
            bool upDriving = basicCondition && protocolDto.Dir == 1 && protocolDto.Door == 3;
            bool downDriving = basicCondition && protocolDto.Dir == 2 && protocolDto.Door == 3;

            switch (protocolDto.Floor)
            {
                case 1:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_B1F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_B1F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_B1F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_B1F));
                    break;

                case 2:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_1F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_1F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_1F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_1F));
                    break;

                case 3:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_2F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_2F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_2F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_2F));
                    break;

                case 4:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_3F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_3F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_3F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_3F));
                    break;

                case 5:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_4F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_4F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_4F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_4F));
                    break;

                case 6:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_5F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_5F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_5F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_5F));
                    break;

                case 7:
                    if (doorOpen) elevatorStateUpdate(nameof(State.DOOROPEN_6F));
                    else if (doorClose) elevatorStateUpdate(nameof(State.DOORCLOSE_6F));
                    else if (upDriving) elevatorStateUpdate(nameof(State.UPDRIVING_6F));
                    else if (downDriving) elevatorStateUpdate(nameof(State.DOWNDRIVING_6F));
                    break;
            }
        }

        private void commmandCompleted(ProtocolDto protocolDto)
        {
            var command = _repository.Commands.GetAll().FirstOrDefault(c => c.state == nameof(CommandState.EXECUTING));
            if (command != null)
            {
                bool basicCondition = (protocolDto.Status == 2 || protocolDto.Status == 9) && protocolDto.Cmd == 11 && protocolDto.Aid == 1 && protocolDto.Dld == 1;
                bool doorOpen = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 2;
                bool doorClose = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 3;
                bool changeState = false;

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
                        changeState = doorClose && protocolDto.Floor == 1;
                        break;

                    case nameof(CommandAction.CALL_1F):
                    case nameof(CommandAction.GOTO_1F):
                        changeState = doorClose && protocolDto.Floor == 2;
                        break;

                    case nameof(CommandAction.CALL_2F):
                    case nameof(CommandAction.GOTO_2F):
                        changeState = doorClose && protocolDto.Floor == 3;
                        break;

                    case nameof(CommandAction.CALL_3F):
                    case nameof(CommandAction.GOTO_3F):
                        changeState = doorClose && protocolDto.Floor == 4;
                        break;

                    case nameof(CommandAction.CALL_4F):
                    case nameof(CommandAction.GOTO_4F):
                        changeState = doorClose && protocolDto.Floor == 5;

                        break;

                    case nameof(CommandAction.CALL_5F):
                    case nameof(CommandAction.GOTO_5F):
                        changeState = doorClose && protocolDto.Floor == 6;
                        break;

                    case nameof(CommandAction.CALL_6F):
                    case nameof(CommandAction.GOTO_6F):
                        changeState = doorClose && protocolDto.Floor == 7;
                        break;
                }
                if (changeState)
                {
                    CommandStateUpdate(nameof(CommandState.COMPLETED));
                }
            }
        }

        private void commandExcuting(ProtocolDto protocolDto)
        {
            var command = _repository.Commands.GetAll().FirstOrDefault(c => c.state == nameof(CommandState.REQUEST));
            if (command != null)
            {
                bool basicCondition = protocolDto.Cmd == 21 && protocolDto.Aid == 1 && protocolDto.Dld == 1 && protocolDto.Dir == 0;
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
                    CommandStateUpdate(nameof(CommandState.EXECUTING));
                    if (command.name == nameof(CommandAction.AGVMODE) || command.name == nameof(CommandAction.NOTAGVMODE))
                    {
                        //기본상태값에 프로토콜이없음.
                        CommandStateUpdate(nameof(CommandState.COMPLETED));
                    }
                }
            }
        }

        private void ReceivedProtocol(string recvMsg)
        {
            //Recv데이터를 ElevatorModel 변수에 넣기
            ProtocolDto protocolDto = GetElevatorModelData(recvMsg);
            if (protocolDto.ErrCode > 0)
            {
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
            }
            else if (protocolDto.Result != null && protocolDto.Result != "" && protocolDto.Result != "ok")
            {
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
            }
            else
            {
                elevatorState(protocolDto);
                commandExcuting(protocolDto);
                commmandCompleted(protocolDto);
            }
        }

        private void RecvElevator_Control(string recvMsg)
        {
            // 1.상시 엘리베이터 상태 요청
            // 2.상시 엘리베이터 상태 응답
            // 2.MiR 운전 제어 요청
            // 3.MiR 운전 제어 응답
            // 4.출발층 Elevator Call요청
            // 5.출발층 Elevator Call요청 응답
            // 6.출발층 으로 Elevator 도착시까지 상태요청
            // 7.출발층 Elevator 상태 응답중 Floor / Door / Dir /Hall_Up및 Hall_Dn 신호로 도착확인
            // 8.출발층 Elevator 도착 후 MiR 진입 신호 전달
            // 9.출발층 Elevator MiR 진입 완료까지 door Open 신호 요청
            // 10.출발층 MiR 진입완료후 목적지층으로 이동 요청 신호
            // 11.출발층 Elevator 목적지층 이동 요청신호 응답
            // 12.출발층 Elevator Door Close 요청 신호
            // 13.출발층 Elevator Door Close 요청 응답
            // 14.목적지층 으로 Elevator 도착시까지 상태요청
            // 15.목적지층 Elevator 상태 응답중 Floor / Door / Dir /Hall_Up및 Hall_Dn 신호로 도착확인
            // 16.목적지층 Elevator MiR 진출 완료까지 Door Open 신호 요청
            // 17.목적지층 MiR 진출 완료후 Door Close요청
            // 18.MiR 운전 제어 해제 신호
            // 19.MiR 운전 제어 해제 신호 응답

            //if (elevators.Status == 2 || elevators.Status == 9)

            //var mirstateElevator = uow.ElevatorState.Find(m => m.ElevatorMissionName != null).FirstOrDefault();
            //if (mirstateElevator != null)
            //{
            //    bool doorOpen = false;
            //    switch (mirstateElevator.ElevatorState)
            //    {
            //        //case "Status":
            //        //    {
            //        //        //엘리베이터 상태가 MiR 제어 신호가 들어올경우
            //        //        var elevatorMode = uow.ElevatorState.GetByElevatorMode();
            //        //        if (elevatorMode.ElevatorMode == ElevatorMode.MiRControlMode.ToString())    //Elevator Mode 변경 버튼으로 사람이 변경한다.
            //        //        {
            //        //            //MiR 상태가 엘리베이터를 부를경우 엘리베이터 상태를 변경한다.
            //        //            mirstateElevator.ElevatorState = ElevatorState.CallStartFloor.ToString();
            //        //            uow.ElevatorState.Update(mirstateElevator);
            //        //        }
            //        //    }
            //        //    break;

            //        //case "MiRControlSignal":
            //        //    //MiR 운전 제어 응답
            //        //    if (elevators.Param == 9 && elevators.Cmd == 21 && elevators.Dest == 0 && elevators.Result == "ok")
            //        //    {
            //        //        mirstateElevator.ElevatorState = ElevatorState.CallStartFloor.ToString();   //[목적지층이 Down일때]
            //        //        uow.ElevatorState.Update(mirstateElevator);
            //        //    }
            //        //    break;

            //        case "CallStartFloor":
            //            //[목적지층이 Up일때] 출발층 Elevator Call요청 응답
            //            if (elevators.Param == 3 && elevators.Cmd == 21 && elevators.Result == "ok")
            //            {
            //                //출발층까지 도착시 까지 상태를 확인한다.
            //                mirstateElevator.ElevatorState = ElevatorState.CallStartFloorStatus.ToString();
            //                uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            break;

            //        case "CallStartFloorStatus":
            //            {
            //                bool StartFloor_protocol = elevators.Status == 2 && elevators.Dld == 1 && elevators.Dir == 0
            //                                          && elevators.car_f == 0 && elevators.car_r == 0 && elevators.Hallup_f == 0 && elevators.Hallup_r == 0
            //                                          && elevators.HallDn_f == 0 && elevators.HallDn_r == 0 && elevators.ErrCode == 0;

            //                //엘리베이터 통신 끈키는 증상발견으로 해당층에 도착후 엘리베이터가 움직이는상태가 아니고 Door닫혀있으면 10번이상확인후 Door Open신호를 던진다 [2023-03-07]
            //                //엘리베이터 업체와 프로토콜이 정해지거나 엘리베이터 수정후 다시 수정해야함 [2023-03-07]

            //                //if(mirstateElevator.elevatorStartFloor == B1F && elevators.Floor == 1) doorOpen = true;

            //                if (StartFloor_protocol)
            //                {
            //                    if (mirstateElevator.StartFloor == "B1F" && elevators.Floor == 1) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "1F" && elevators.Floor == 2) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "2F" && elevators.Floor == 3) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "3F" && elevators.Floor == 4) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "4F" && elevators.Floor == 5) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "5F" && elevators.Floor == 6) doorOpen = true;
            //                    else if (mirstateElevator.StartFloor == "6F" && elevators.Floor == 7) doorOpen = true;

            //                    //DoorOpen
            //                    if (doorOpen)
            //                    {
            //                        mirstateElevator.ElevatorState = ElevatorState.CallStartDoorOpen.ToString();
            //                        uow.ElevatorState.Update(mirstateElevator);
            //                        elevatorOpenRetry = 0;
            //                    }
            //                    else if (elevators.Door == 3)
            //                    {
            //                        if (elevatorOpenRetry >= 100) doorOpen = true;
            //                        else elevatorOpenRetry++;
            //                    }
            //                    else elevatorOpenRetry = 0;

            //                    //else if (elevators.Door == 2 || elevatorOpenRetry > 100) doorOpen = true;

            //                    //else if (elevators.Door == 2 && elevatorOpenRetry > 100) doorOpen = true;

            //                    //else if (elevators.Door == 3) elevatorOpenRetry++;

            //                    //else elevatorOpenRetry = 0;

            //                    /*[2024.06.11]이전 소스
            //                    ////엘리베이터 통신 끈키는 증상발견으로 해당층에 도착후 엘리베이터가 움직이는상태가 아니고 Door닫혀있으면 10번이상확인후 Door Open신호를 던진다 [2023-03-07]
            //                    ////엘리베이터 업체와 프로토콜이 정해지거나 엘리베이터 수정후 다시 수정해야함 [2023-03-07]
            //                    //if (EndFloorStatus_protocol)
            //                    //{
            //                    //    if (elevators.Door == 2 || elevatorOpenRetry > 10)
            //                    //    {
            //                    //        mirstateElevator.ElevatorState = ElevatorState.CallEndDoorOpen.ToString();
            //                    //        uow.ElevatorState.Update(mirstateElevator);
            //                    //        elevatorOpenRetry = 0;
            //                    //    }
            //                    //    else if (elevators.Door == 3) elevatorOpenRetry++;
            //                    //    else elevatorOpenRetry = 0;

            //                    //    //엘리베이터 통신 끈키는 증상발견으로 해당층에 도착후 엘리베이터가 움직이는상태가 아니고 Door닫혀있으면 10번이상확인후 Door Open신호를 던진다 [2023-03-07]
            //                    //    //엘리베이터 업체와 프로토콜이 정해지거나 엘리베이터 수정후 다시 수정해야함 [2023-03-07]

            //                    //}
            //                    //else elevatorOpenRetry = 0;
            //                    */
            //                }
            //                else elevatorOpenRetry = 0;
            //            }
            //            break;

            //        case "CallStartDoorOpen":
            //            //MiR 진입완료 전까지 DoorOpen
            //            if (mirstateElevator.MiRStateElevator == MiRStateElevator.MiRStateElevatorLoaderComplet.ToString())
            //            {
            //                //MiR Elevator 진입했을경우
            //                //목적지층으로 요청
            //                mirstateElevator.ElevatorState = ElevatorState.CallEndFloorSelect.ToString();  //[목적지층이 Down일때]
            //                uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            else
            //            {   //MiR 진입 신호
            //                if (mirstateElevator.MiRStateElevator != MiRStateElevator.MiRStateElevatorLoaderStart.ToString())
            //                {
            //                    mirstateElevator.MiRStateElevator = MiRStateElevator.MiRStateElevatorLoaderStart.ToString();
            //                    uow.ElevatorState.Update(mirstateElevator);
            //                }
            //            }
            //            break;

            //        case "CallEndFloorSelect":
            //            //[목적지층이 Up일때]출발층 Elevator 목적지선택 요청
            //            if (elevators.Param == 3 && elevators.Cmd == 21 && elevators.Result == "ok")
            //            {
            //                //출발층 도어 닫기
            //                mirstateElevator.ElevatorState = ElevatorState.CallStartDoorClose.ToString();
            //                uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            break;

            //        case "CallStartDoorClose":
            //            //출발층 Elevator Door Close 요청 신호 응답
            //            if (elevators.Param == 6 && elevators.Cmd == 21 && elevators.Result == "ok")
            //            {
            //                //목적지층 도착시까지 Elevator 상태값을 요청한다.
            //                mirstateElevator.ElevatorState = ElevatorState.CallEndFloorStatus.ToString();
            //                uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            break;

            //        case "CallEndFloorStatus":

            //            bool EndFloorStatus_protocol = elevators.Status == 2 && elevators.Dld == 1 && elevators.Dir == 0
            //               && elevators.car_f == 0 && elevators.car_r == 0 && elevators.Hallup_f == 0 && elevators.Hallup_r == 0
            //               && elevators.HallDn_f == 0 && elevators.HallDn_r == 0 && elevators.ErrCode == 0;

            //            if (EndFloorStatus_protocol)
            //            {
            //                if (mirstateElevator.EndFloor == "B1F" && elevators.Floor == 1) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "1F" && elevators.Floor == 2) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "2F" && elevators.Floor == 3) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "3F" && elevators.Floor == 4) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "4F" && elevators.Floor == 5) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "5F" && elevators.Floor == 6) doorOpen = true;
            //                else if (mirstateElevator.EndFloor == "6F" && elevators.Floor == 7) doorOpen = true;

            //                //Door Open
            //                if (doorOpen)
            //                {
            //                    mirstateElevator.ElevatorState = ElevatorState.CallEndDoorOpen.ToString();
            //                    uow.ElevatorState.Update(mirstateElevator);
            //                    elevatorOpenRetry = 0;
            //                }
            //                else if (elevators.Door == 3)
            //                {
            //                    if (elevatorOpenRetry >= 100) doorOpen = true;
            //                    else elevatorOpenRetry++;
            //                }
            //                else elevatorOpenRetry = 0;

            //                //else if (elevators.Door == 2 || elevatorOpenRetry > 100) doorOpen = true;
            //                //else if (elevators.Door == 2 && elevatorOpenRetry > 100) doorOpen = true;

            //                //else if (elevators.Door == 3) elevatorOpenRetry++;

            //                //else elevatorOpenRetry = 0;

            //                /*[2024.06.11]이전 소스
            //                ////엘리베이터 통신 끈키는 증상발견으로 해당층에 도착후 엘리베이터가 움직이는상태가 아니고 Door닫혀있으면 10번이상확인후 Door Open신호를 던진다 [2023-03-07]
            //                ////엘리베이터 업체와 프로토콜이 정해지거나 엘리베이터 수정후 다시 수정해야함 [2023-03-07]
            //                //if (EndFloorStatus_protocol)
            //                //{
            //                //    if (elevators.Door == 2 || elevatorOpenRetry > 10)
            //                //    {
            //                //        mirstateElevator.ElevatorState = ElevatorState.CallEndDoorOpen.ToString();
            //                //        uow.ElevatorState.Update(mirstateElevator);
            //                //        elevatorOpenRetry = 0;
            //                //    }
            //                //    else if (elevators.Door == 3) elevatorOpenRetry++;
            //                //    else elevatorOpenRetry = 0;

            //                //    //엘리베이터 통신 끈키는 증상발견으로 해당층에 도착후 엘리베이터가 움직이는상태가 아니고 Door닫혀있으면 10번이상확인후 Door Open신호를 던진다 [2023-03-07]
            //                //    //엘리베이터 업체와 프로토콜이 정해지거나 엘리베이터 수정후 다시 수정해야함 [2023-03-07]

            //                //}
            //                //else elevatorOpenRetry = 0;
            //                */
            //            }
            //            else elevatorOpenRetry = 0;

            //            break;

            //        case "CallEndDoorOpen":
            //            //목적지층 도착시 DoorOpen변경
            //            if (mirstateElevator.MiRStateElevator == MiRStateElevator.MiRStateElevatorUnLoaderComplet.ToString())
            //            {
            //                //MiR Elevator 진출이 완료되었을때 DoorClose 요청
            //                //목적지층 도착했을경우
            //                mirstateElevator.ElevatorState = ElevatorState.CallEndDoorClose.ToString();
            //                uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            else
            //            {
            //                //MiR 진출 신호
            //                if (mirstateElevator.MiRStateElevator != MiRStateElevator.MiRStateElevatorUnLoaderStart.ToString())
            //                {
            //                    mirstateElevator.MiRStateElevator = MiRStateElevator.MiRStateElevatorUnLoaderStart.ToString();
            //                    uow.ElevatorState.Update(mirstateElevator);
            //                }
            //            }
            //            break;

            //        case "CallEndDoorClose":
            //            //목적지층 DoorClose 요청 신호 응답
            //            if (elevators.Param == 6 && elevators.Cmd == 21 && elevators.Result == "ok")
            //            {
            //                //완료
            //                uow.ElevatorState.Remove(mirstateElevator); //MiR 운전 제어 해제 신호

            //                //mirstateElevator.ElevatorState = ElevatorState.MiRUnContorlSignal.ToString();
            //                //uow.ElevatorState.Update(mirstateElevator);
            //            }
            //            break;

            //            //case "MiRUnControlSignal":
            //            //    //MiR 운전 제어 해제 신호 응답
            //            //    if (elevators.Param == 9 && elevators.Cmd == 21 && elevators.Data == 0 && elevators.Result == "ok")
            //            //    {
            //            //        //완료
            //            //        uow.ElevatorState.Remove(mirstateElevator);
            //            //    }
            //            //    break;
            //    }
            //}
            //else
            //{
            //    ElevatorStateModule elevatorStatus = null;
            //    var missionName = uow.Missions.Find(m => m.MissionName.Contains("Elevator") && m.MissionState == "Executing").FirstOrDefault();
            //    if (missionName != null)
            //    {
            //        elevatorStatus = uow.ElevatorState.Find(e => string.IsNullOrEmpty(e.MiRStateElevator) && (e.RobotName == missionName.RobotName || e.RobotName == missionName.JobCreateRobotName)).FirstOrDefault();
            //    }

            //    var elevatorInfoMode = uow.ElevatorInfo.Find(m => m.Location == "Elevator1").FirstOrDefault();
            //    if ((missionName == null || elevatorStatus == null) && elevatorInfoMode != null)
            //    {
            //        //"MiRControlSignal":
            //        //    //MiR 운전 제어 응답
            //        if (elevatorInfoMode.ACSMode == "MiRControlMode" && elevators.Param == 9 && elevators.Cmd == 21 && elevators.Dest == 0 && elevators.Result == "ok")
            //        {
            //            elevatorInfoMode.ElevatorMode = "AGVMode";
            //        }
            //        else
            //        {
            //            elevatorInfoMode.ElevatorMode = "NotAGVMode";
            //        }
            //    }
            //}
        }

        private ProtocolDto GetElevatorModelData(string recvMsg)
        {
            //"&","^"두개의 문자 잘라서 배열로 반환한다
            string[] splitData = recvMsg.Split(new string[] { "&", "^", "\r\n" }, StringSplitOptions.None);

            foreach (var Data in splitData)
            {
                if (Data.Contains("Cmd="))
                {
                    //Data에 해당 문자가 있으면 초기화 후 진행
                    //문자 바꾸기 하여 Cmd=문자를 ""빈문자로 변경
                    elevatorProtocolDto.Cmd = 0;
                    elevatorProtocolDto.Cmd = Convert.ToInt32(Data.Replace("Cmd=", ""));
                }
                else if (Data.Contains("Aid="))
                {
                    elevatorProtocolDto.Aid = 0;
                    elevatorProtocolDto.Aid = Convert.ToInt32(Data.Replace("Aid=", ""));
                }
                else if (Data.Contains("Count="))
                {
                    elevatorProtocolDto.Count = 0;
                    elevatorProtocolDto.Count = Convert.ToInt32(Data.Replace("Count=", ""));
                }
                else if (Data.Contains("DId="))
                {
                    elevatorProtocolDto.Dld = 0;
                    elevatorProtocolDto.Dld = Convert.ToInt32(Data.Replace("DId=", ""));
                }
                else if (Data.Contains("Status="))
                {
                    elevatorProtocolDto.Status = 0;
                    elevatorProtocolDto.Status = Convert.ToInt32(Data.Replace("Status=", ""));
                }
                else if (Data.Contains("Floor="))
                {
                    elevatorProtocolDto.Floor = 0;
                    elevatorProtocolDto.Floor = Convert.ToInt32(Data.Replace("Floor=", ""));
                }
                else if (Data.Contains("Dir="))
                {
                    elevatorProtocolDto.Dir = 0;
                    elevatorProtocolDto.Dir = Convert.ToInt32(Data.Replace("Dir=", ""));
                }
                else if (Data.Contains("Door="))
                {
                    elevatorProtocolDto.Door = 0;
                    elevatorProtocolDto.Door = Convert.ToInt32(Data.Replace("Door=", ""));
                }
                else if (Data.Contains("car_f="))
                {
                    elevatorProtocolDto.car_f = 0;
                    elevatorProtocolDto.car_f = Convert.ToInt32(Data.Replace("car_f=", ""));
                }
                else if (Data.Contains("car_r="))
                {
                    elevatorProtocolDto.car_r = 0;
                    elevatorProtocolDto.car_r = Convert.ToInt32(Data.Replace("car_r=", ""));
                }
                else if (Data.Contains("Hallup_f="))
                {
                    elevatorProtocolDto.Hallup_f = 0;
                    elevatorProtocolDto.Hallup_f = Convert.ToInt32(Data.Replace("Hallup_f=", ""));
                }
                else if (Data.Contains("Hallup_r="))
                {
                    elevatorProtocolDto.Hallup_r = 0;
                    elevatorProtocolDto.Hallup_r = Convert.ToInt32(Data.Replace("Hallup_r=", ""));
                }
                else if (Data.Contains("HallDn_f="))
                {
                    elevatorProtocolDto.HallDn_f = 0;
                    elevatorProtocolDto.HallDn_f = Convert.ToInt32(Data.Replace("HallDn_f=", ""));
                }
                else if (Data.Contains("HallDn_r="))
                {
                    elevatorProtocolDto.HallDn_r = 0;
                    elevatorProtocolDto.HallDn_r = Convert.ToInt32(Data.Replace("HallDn_r=", ""));
                }
                else if (Data.Contains("ErrCode="))
                {
                    elevatorProtocolDto.ErrCode = 0;
                    elevatorProtocolDto.ErrCode = Convert.ToInt32(Data.Replace("ErrCode=", ""));
                }
                else if (Data.Contains("Param="))
                {
                    elevatorProtocolDto.Param = 0;
                    elevatorProtocolDto.Param = Convert.ToInt32(Data.Replace("Param=", ""));
                }
                else if (Data.Contains("Data="))
                {
                    elevatorProtocolDto.Data = 0;
                    elevatorProtocolDto.Data = Convert.ToInt32(Data.Replace("Data=", ""));
                }
                else if (Data.Contains("Dest="))
                {
                    elevatorProtocolDto.Dest = 0;
                    elevatorProtocolDto.Dest = Convert.ToInt32(Data.Replace("Dest=", ""));
                }
                else if (Data.Contains("Result="))
                {
                    elevatorProtocolDto.Result = "";
                    elevatorProtocolDto.Result = Data.Replace("Result=", "");
                }
            }
            return elevatorProtocolDto;
        }
    }
}