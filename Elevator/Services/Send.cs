using Common.Models;
using System.Net.Sockets;
using System.Text;

namespace Elevator.Services
{
    public partial class MainService
    {
        private byte[] MakeSendingData()
        {
            string sendMsg = "";

            string fmt = "00000000";
            string sendMsgLength = string.Empty;
            string sendData = string.Empty;

            //elevator 상태값으로 하여 보낼 데이터 선택하기
            sendMsg = SendingElevator_Control();
            sendMsgLength = sendMsg.Length.ToString(fmt);
            sendData = sendMsgLength + sendMsg;
            return SendingServerStream(sendData);
        }

        private bool test = false;

        private string SendingElevator_Control()
        {
            string SendMsg = "";
            var command = _repository.Commands.GetAll().FirstOrDefault(c => (c.state == nameof(CommandState.PENDING)) || (c.state == nameof(CommandState.REQUEST)));
            if (command != null)
            {
                CommandStateUpdate(nameof(CommandState.REQUEST));
                switch (command.actionName)
                {
                    case nameof(CommandAction.DOOROPEN):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=05&Data=00&Dest=00";
                        break;

                    case nameof(CommandAction.DOORCLOSE):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=06&Data=00&Dest=00";
                        break;

                    case nameof(CommandAction.CALL_B1F):
                    case nameof(CommandAction.GOTO_B1F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=01&Dest=01";
                        break;

                    case nameof(CommandAction.CALL_1F):
                    case nameof(CommandAction.GOTO_1F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=02&Dest=02";
                        break;

                    case nameof(CommandAction.CALL_2F):
                    case nameof(CommandAction.GOTO_2F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=03&Dest=03";
                        break;

                    case nameof(CommandAction.CALL_3F):
                    case nameof(CommandAction.GOTO_3F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=04&Dest=04";
                        break;

                    case nameof(CommandAction.CALL_4F):
                    case nameof(CommandAction.GOTO_4F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=05&Dest=05";
                        break;

                    case nameof(CommandAction.CALL_5F):
                    case nameof(CommandAction.GOTO_5F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=06&Dest=06";
                        break;

                    case nameof(CommandAction.CALL_6F):
                    case nameof(CommandAction.GOTO_6F):
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=03&Data=07&Dest=07";
                        break;

                    case nameof(CommandAction.AGVMODE):
                        //Param = 09 AGV운전 명령 , Data = 1이면 제어 , Dest = 00으로 고정
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=01&Dest=00";
                        break;

                    case nameof(CommandAction.NOTAGVMODE):
                        //Param = 09 AGV운전 명령 , Data = 1이면 제어 , Dest = 00으로 고정
                        SendMsg = "Cmd=20&AId=1&DId=1&Param=09&Data=00&Dest=00";
                        break;
                }
            }
            else
            {
                //Status
                SendMsg = "Cmd=10&AId=1&DId=1";
            }

            return SendMsg;
        }

        private byte[] SendingServerStream(string strBuffer)//Sending Data 변환
        {
            byte[] tempBuff = Encoding.ASCII.GetBytes(strBuffer);
            byte[] sendData = null;
            using (var ms = new MemoryStream())
            {
                ms.Write(tempBuff, 0, tempBuff.Length);
                sendData = ms.ToArray();
            }
            return sendData;
        }
    }
}