using Common.Models;
using Data;
using System.Net.Sockets;
using System.Text;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
       
        public async void ElevatorTCPClient()
        {
            while (true)
            {
                try
                {
                    //await Task.Delay(500); // <=========================== 노드간 통신 간격
                    await Task.Delay(500); // <=========================== 노드간 통신 간격

                    bool recv_good = false;

                    recv_good = await SendRecvAsync();

                    if (recv_good)                         //정상적인 데이터를 읽어왔는지 확인
                    {
                        ConnectedCount = 0;
                        //컨넥트 이벤트
                        elevatorStateUpdate(nameof(State.CONNECT));
                    }
                    else
                    {
                        if (ConnectedCount == 10)
                        {
                            elevatorStateUpdate(nameof(State.DISCONNECT));
                            ConnectedCount = 0;
                        }
                        else ConnectedCount++;
                    }
                    await Task.Delay(1); // <=========================== 루프 통신 딜레이
                }
                catch (Exception ex)
                {
                    elevatorStateUpdate(nameof(State.DISCONNECT));
                    main.LogExceptionMessage(ex);
                }
            }
        }


        private async Task<bool> SendRecvAsync()
        {
            //ILog("ip = " + PlcIpAddress);
            byte[] sendData = MakeSendingData();

            try
            {
                using (var client = new TcpClient())
                {
                    var cancelTask = Task.Delay(int.Parse(ConfigData.ElevatorSetting.timeout)); // <=========================== 연결타임아웃 시간
                    //시뮬레이터 Test
                    var connectTask = client.ConnectAsync(ConfigData.ElevatorSetting.ip, int.Parse(ConfigData.ElevatorSetting.port));

                    var completedTask = await Task.WhenAny(connectTask, cancelTask);

                    if (completedTask == cancelTask)
                    {
                        EventLogger.Info("Elevator SendRecvAsync() : connection time out");
                        client.Close();  // ★★★ 연결 강제 종료
                        return false;
                    }

                    try
                    {
                        await connectTask; // ★ 여기서 Task를 완료시켜줘야함 (예외를 무시하기 위해)

                        using (var stream = client.GetStream())
                        {
                            String response = String.Empty;
                            byte[] recvBuff = new Byte[1024];
                            int recvLength = 0;

                            stream.ReadTimeout = 1000; // <=========================== 수신타임아웃 시간
                                                       // send message
                            stream.Write(sendData, 0, sendData.Length);

                            string message = Encoding.ASCII.GetString(sendData);

                            ProtocolLogger.Info($"Elevator Sent: {message}");

                            sendData = null;

                            //
                            // recv response
                            while ((recvLength = stream.Read(recvBuff, 0, recvBuff.Length)) != 0)
                            {
                                response += Encoding.ASCII.GetString(recvBuff, 0, recvLength);

                                if (response.IndexOf("\r\n") != -1) // ETX 수신시 루프 탈출
                                    break;
                            }
                            ProtocolLogger.Info($"Elevator Recv: {response}");
                            if (response.Length > 0)
                                return MakeRecvData(recvBuff);
                            else
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        elevatorStateUpdate(nameof(State.PROTOCOLERROR));
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
                main.LogExceptionMessage(ex);
            }
            return false;
        }
    }
}
