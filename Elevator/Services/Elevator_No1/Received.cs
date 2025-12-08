using Common.Dtos;
using Common.Models;
using System.Text;
using System.Text.Json;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
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
                main.LogExceptionMessage(ex);
            }
            return returnValue;
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

        private void elevatorState(ProtocolDto protocolDto)
        {
            bool basicCondition = (protocolDto.Status == 2 || protocolDto.Status == 9) && protocolDto.Cmd == 11 && protocolDto.AId == 1 && protocolDto.Dld == 1;
            bool doorOpen = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 2;
            bool doorClose = basicCondition && protocolDto.Dir == 0 && protocolDto.Door == 3;
            bool upDriving = basicCondition && protocolDto.Dir == 1 && protocolDto.Door == 3;
            bool downDriving = basicCondition && protocolDto.Dir == 2 && protocolDto.Door == 3;

            //기본 상태확인
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

        private void protocol_Init()
        {
            elevatorProtocolDto.Cmd = 0;
            elevatorProtocolDto.AId = 0;
            elevatorProtocolDto.Count = 0;
            elevatorProtocolDto.Dld = 0;
            elevatorProtocolDto.Status = 0;
            elevatorProtocolDto.Floor = 0;
            elevatorProtocolDto.Dir = 0;
            elevatorProtocolDto.Door = 0;
            elevatorProtocolDto.car_f = 0;
            elevatorProtocolDto.car_r = 0;
            elevatorProtocolDto.Hallup_f = 0;
            elevatorProtocolDto.Hallup_r = 0;
            elevatorProtocolDto.HallDn_f = 0;
            elevatorProtocolDto.HallDn_r = 0;
            elevatorProtocolDto.ErrCode = 0;
            elevatorProtocolDto.Param = 0;
            elevatorProtocolDto.Data = 0;
            elevatorProtocolDto.Dest = 0;
            elevatorProtocolDto.Result = "";
        }

        private ProtocolDto GetElevatorModelData(string recvMsg)
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

            //"&","^"두개의 문자 잘라서 배열로 반환한다
            string[] splitData = recvMsg.Split(new string[] { "&", "^", "\r\n" }, StringSplitOptions.None);
            protocol_Init();
            foreach (var Data in splitData)
            {
                if (Data.Contains("Cmd="))
                {
                    //Data에 해당 문자가 있으면 초기화 후 진행
                    //문자 바꾸기 하여 Cmd=문자를 ""빈문자로 변경
                    elevatorProtocolDto.Cmd = Convert.ToInt32(Data.Replace("Cmd=", ""));
                }
                else if (Data.Contains("AId=") || Data.Contains("Aid="))
                {
                    if (Data.Contains("AId=")) elevatorProtocolDto.AId = Convert.ToInt32(Data.Replace("AId=", ""));
                    else if (Data.Contains("Aid=")) elevatorProtocolDto.AId = Convert.ToInt32(Data.Replace("Aid=", ""));
                }
                else if (Data.Contains("Count="))
                {
                    elevatorProtocolDto.Count = Convert.ToInt32(Data.Replace("Count=", ""));
                }
                else if (Data.Contains("DId="))
                {
                    elevatorProtocolDto.Dld = Convert.ToInt32(Data.Replace("DId=", ""));
                }
                else if (Data.Contains("Status="))
                {
                    elevatorProtocolDto.Status = Convert.ToInt32(Data.Replace("Status=", ""));
                }
                else if (Data.Contains("Floor="))
                {
                    elevatorProtocolDto.Floor = Convert.ToInt32(Data.Replace("Floor=", ""));
                }
                else if (Data.Contains("Dir="))
                {
                    elevatorProtocolDto.Dir = Convert.ToInt32(Data.Replace("Dir=", ""));
                }
                else if (Data.Contains("Door="))
                {
                    elevatorProtocolDto.Door = Convert.ToInt32(Data.Replace("Door=", ""));
                }
                else if (Data.Contains("car_f="))
                {
                    elevatorProtocolDto.car_f = Convert.ToInt32(Data.Replace("car_f=", ""));
                }
                else if (Data.Contains("car_r="))
                {
                    elevatorProtocolDto.car_r = Convert.ToInt32(Data.Replace("car_r=", ""));
                }
                else if (Data.Contains("Hallup_f="))
                {
                    elevatorProtocolDto.Hallup_f = Convert.ToInt32(Data.Replace("Hallup_f=", ""));
                }
                else if (Data.Contains("Hallup_r="))
                {
                    elevatorProtocolDto.Hallup_r = Convert.ToInt32(Data.Replace("Hallup_r=", ""));
                }
                else if (Data.Contains("HallDn_f="))
                {
                    elevatorProtocolDto.HallDn_f = Convert.ToInt32(Data.Replace("HallDn_f=", ""));
                }
                else if (Data.Contains("HallDn_r="))
                {
                    elevatorProtocolDto.HallDn_r = Convert.ToInt32(Data.Replace("HallDn_r=", ""));
                }
                else if (Data.Contains("ErrCode="))
                {
                    elevatorProtocolDto.ErrCode = Convert.ToInt32(Data.Replace("ErrCode=", ""));
                }
                else if (Data.Contains("Param="))
                {
                    elevatorProtocolDto.Param = Convert.ToInt32(Data.Replace("Param=", ""));
                }
                else if (Data.Contains("Data="))
                {
                    elevatorProtocolDto.Data = Convert.ToInt32(Data.Replace("Data=", ""));
                }
                else if (Data.Contains("Dest="))
                {
                    elevatorProtocolDto.Dest = Convert.ToInt32(Data.Replace("Dest=", ""));
                }
                else if (Data.Contains("Result="))
                {
                    elevatorProtocolDto.Result = Data.Replace("Result=", "");
                }
            }
            return elevatorProtocolDto;
        }
    }
}