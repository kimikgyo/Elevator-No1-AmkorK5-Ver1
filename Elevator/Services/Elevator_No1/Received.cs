using Common.Dtos;
using Common.Models;
using System.Text;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        private bool MakeRecvData(byte[] data)
        {
            string recvDataLength = string.Empty; // 헤더 길이 문자열
            int headerLength = 8;                 // index 0~7까지 8개의 문자가 길이
            bool returnValue = false;
            string strBuffer = string.Empty;

            if (data == null)
            {
                EventLogger.Warn($"[Elevator][MakeRecvData] : data 가 null 입니다.");
                return false;
            }

            if (data.Length < headerLength)
            {
                EventLogger.Warn($"[Elevator][MakeRecvData] : 데이터 길이가 너무 짧습니다. data.Length=" + data.Length);
                return false;
            }

            try
            {
                // 1) 헤더(길이) 부분 추출 (0~7)
                recvDataLength = Encoding.ASCII.GetString(data, 0, headerLength);

                int bodyMinLength = Convert.ToInt32(recvDataLength);   // 최소 보장 길이(기본 프레임 길이)
                int availableLength = data.Length - headerLength;      // 실제로 들어온 본문 길이

                // 2) 실제 길이가 최소 길이보다 짧으면 → 패킷이 덜 온 것
                if (availableLength < bodyMinLength)
                {
                    EventLogger.Warn($"[Elevator][MakeRecvData] : 본문 길이가 부족합니다. Header={bodyMinLength}, Available={availableLength}");

                    return false;
                }

                // 3) ★ 중요: 기본 프레임 길이(bodyMinLength)까지만이 아니라
                //    실제로 들어온 전체 본문(availableLength)을 모두 읽는다.
                //    예) Cmd=...까지만이 아니라 &Result=...&ErrMsg=... 도 같이 포함
                strBuffer = Encoding.ASCII.GetString(data, headerLength, availableLength);

                ProtocolLogger.Info($"[Elevator][MakeRecvData] : HeaderMinLength={bodyMinLength}, ActualBodyLength={strBuffer.Length}, Body={strBuffer}");

                // 4) 최소 길이는 만족해야 한다.
                //    (정상 응답은 보통 bodyMinLength == strBuffer.Length겠지만,
                //     에러 응답처럼 추가 필드가 붙은 경우는 bodyMinLength < strBuffer.Length 도 허용)
                if (strBuffer.Length >= bodyMinLength)
                {
                    ReceivedProtocol(strBuffer);
                    returnValue = true;
                }
                else
                {
                    EventLogger.Warn($"[Elevator][MakeRecvData] : 본문 문자열 길이가 헤더의 최소 길이보다 짧습니다. HeaderMin={bodyMinLength}, Actual={strBuffer.Length}");
                    returnValue = false;
                }
            }
            catch (Exception ex)
            {
                main.LogExceptionMessage(ex);
                returnValue = false;
            }

            return returnValue;
        }

        private void ReceivedProtocol(string recvMsg)
        {
            if (string.IsNullOrEmpty(recvMsg))
            {
                EventLogger.Warn($"[Elevator][ReceivedProtocol] : recvMsg 가 비어 있습니다.");
                return;
            }

            //ProtocolLogger.Info($"[Elevator][{nameof(ReceivedProtocol)}] : 수신 메시지 = {recvMsg}");

            ProtocolDto protocolDto = GetElevatorModelData(recvMsg);

            if (protocolDto.ErrCode > 0)
            {
                EventLogger.Warn($"[Elevator][ReceivedProtocol] : ErrCode={protocolDto.ErrCode}");
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
            }
            else if (protocolDto.Result != null && protocolDto.Result != string.Empty && protocolDto.Result != "ok")
            {
                EventLogger.Warn($"[Elevator][ReceivedProtocol] : Result={protocolDto.Result}");
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
            }
            else
            {
                elevatorState(protocolDto);
                EnsureDoorCloseOnOpenTimeoutByState();
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
            //ProtocolLogger.Info("GetElevatorModelData() : 파싱 시작. Raw=\"" + recvMsg + "\"");

            string[] splitData = recvMsg.Split(new string[] { "&", "^", "\r\n" }, StringSplitOptions.None);

            protocol_Init();

            foreach (string Data in splitData)
            {
                if (string.IsNullOrEmpty(Data))
                {
                    continue;
                }

                try
                {
                    if (Data.Contains("Cmd="))
                    {
                        elevatorProtocolDto.Cmd = Convert.ToInt32(Data.Replace("Cmd=", ""));
                    }
                    else if (Data.Contains("AId=") || Data.Contains("Aid="))
                    {
                        if (Data.Contains("AId="))
                            elevatorProtocolDto.AId = Convert.ToInt32(Data.Replace("AId=", ""));
                        else if (Data.Contains("Aid="))
                            elevatorProtocolDto.AId = Convert.ToInt32(Data.Replace("Aid=", ""));
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
                catch (Exception ex)
                {
                    // 이 토큰만 잘못된 경우: 전체를 죽이지 않고 로그만 남기고 다음 토큰으로 진행
                    EventLogger.Warn($"[Elevator][GetElevatorModelData] : 파싱 실패. Token={Data}");
                    main.LogExceptionMessage(ex);
                }
            }

            //ProtocolLogger.Info(
            //    "GetElevatorModelData() : 파싱 완료. " +"Cmd=" + elevatorProtocolDto.Cmd +
            //    ", AId=" + elevatorProtocolDto.AId +
            //    ", DId=" + elevatorProtocolDto.Dld +
            //    ", Floor=" + elevatorProtocolDto.Floor +
            //    ", Door=" + elevatorProtocolDto.Door +
            //    ", ErrCode=" + elevatorProtocolDto.ErrCode +
            //    ", Result=" + elevatorProtocolDto.Result
            //);

            return elevatorProtocolDto;
        }
    }
}