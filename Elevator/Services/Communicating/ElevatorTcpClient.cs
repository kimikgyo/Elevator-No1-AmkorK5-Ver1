using Common.Models;
using System.Net.Sockets;
using System.Text;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        // 1) async void → async Task 로 변경하는 것을 강력 추천
        public async Task ElevatorTCPClient()
        {
            EventLogger.Info("[ElevatorTCPClient Task] Start");  // 루프 시작 로그
            try
            {
                while (_running)
                {
                    try
                    {
                        // 1) 설정 매번 읽기 (원하지 않으면 바깥으로 빼도 됨)
                        var setting = _repository.Settings.GetAll().FirstOrDefault(r => r.id == "NO1");

                        if (setting == null)
                        {
                            // 설정 없음 → DISCONNECT 상태 유지 + 일정 시간 후 재시도
                            elevatorStateUpdate(nameof(State.DISCONNECT));
                            ConnectedCount = 0;

                            await Task.Delay(1000); // 설정이 없으면 1초마다 재시도
                            continue;
                        }

                        // 2) 포트/타임아웃 파싱 방어
                        if (!int.TryParse(setting.port, out int port) || !int.TryParse(setting.timeout, out int timeout))
                        {
                            main.LogExceptionMessage(new Exception($"[ElevatorTCPClient] 설정 값 오류: port={setting.port}, timeout={setting.timeout}"));

                            elevatorStateUpdate(nameof(State.DISCONNECT));
                            ConnectedCount = 0;

                            await Task.Delay(1000);
                            continue;
                        }

                        string ip = setting.ip;

                        // 3) 노드 간 통신 간격
                        await Task.Delay(1000);

                        bool recv_good = await SendRecvAsync(ip, port, timeout);

                        if (recv_good)
                        {
                            // 정상 통신 → 카운터 초기화 + CONNECT 이벤트
                            if (ConnectedCount != 0)
                            {
                                // 끊겼다가 복구된 상황이면 로그 찍어도 좋음
                                EventLogger.Info("[ElevatorTCPClient] 통신 복구");
                            }

                            ConnectedCount = 0;
                            elevatorStateUpdate(nameof(State.CONNECT));
                        }
                        else
                        {
                            // 실패 누적 카운트
                            ConnectedCount++;

                            if (ConnectedCount >= 10)
                            {
                                elevatorStateUpdate(nameof(State.DISCONNECT));
                                ConnectedCount = 0;
                            }
                        }

                        // 4) 별도 루프 딜레이는 필요 없다면 제거 가능
                        // await Task.Delay(1);
                    }
                    catch (Exception ex)
                    {
                        // 통신 중 예외 발생 시 DISCONNECT 전환 + 로그
                        elevatorStateUpdate(nameof(State.DISCONNECT));
                        main.LogExceptionMessage(ex);

                        // 예외가 계속 터질 때 과도한 루프를 막기 위해 약간 쉬어가는 것도 좋음
                        await Task.Delay(500);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[ElevatorTCPClient Task] Stop");  // 루프 정지 로그
            }
        }

        /// <summary>
        /// 엘리베이터 서버와 통신 (송신 + 수신)
        /// 1) 송신 데이터 생성
        /// 2) TCP 연결 (타임아웃 포함)
        /// 3) 데이터 송신
        /// 4) 응답 수신 (\r\n 까지)
        /// 5) 프로토콜 파싱 (MakeRecvData)
        /// </summary>
        private async Task<bool> SendRecvAsync(string ip, int port, int timeout)
        {
            // 0) 송신 데이터 생성
            byte[] sendData = MakeSendingData();

            if (sendData == null || sendData.Length == 0)
            {
                EventLogger.Warn($"[Elevator][SendRecvAsync] MakeSendingData() 결과가 비어 있습니다.");
                return false;
            }

            // 디버그용 로그 (송신 데이터 길이 확인)
            //EventLogger.Info($"[Elevator][{nameof(SendRecvAsync)}] Start. IP={ip}, Port={port}, Timeout={timeout}ms, SendLength= {sendData.Length}");
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // 1) 연결 타임아웃 Task
                    Task cancelTask = Task.Delay(timeout); // <=========================== 연결 타임아웃 시간

                    // 2) 서버 연결 시도
                    Task connectTask = client.ConnectAsync(ip, port);

                    Task completedTask = await Task.WhenAny(connectTask, cancelTask);

                    // 2-1) 타임아웃 먼저 완료된 경우
                    if (completedTask == cancelTask)
                    {
                        EventLogger.Warn($"[Elevator][SendRecvAsync] : connection time out");

                        try
                        {
                            client.Close();  // 연결 강제 종료
                        }
                        catch (Exception closeEx)
                        {
                            main.LogExceptionMessage(closeEx);
                        }

                        return false;
                    }

                    // 2-2) 연결 Task 완료 (실제 예외가 있었다면 여기서 throw)
                    try
                    {
                        await connectTask; // 여기서 Task를 완전히 완료시켜야 예외를 잡을 수 있음

                        //EventLogger.Info($"[Elevator][{nameof(SendRecvAsync)}] : Server Connect Compleate.");

                        using (NetworkStream stream = client.GetStream())
                        {
                            string response = string.Empty;
                            byte[] recvBuff = new byte[1024];
                            int recvLength = 0;

                            // 3) 수신 타임아웃 설정
                            stream.ReadTimeout = 1000; // <=========================== 수신타임아웃 시간(ms)

                            // 4) 송신 처리
                            stream.Write(sendData, 0, sendData.Length);

                            string message = Encoding.ASCII.GetString(sendData);
                            //ProtocolLogger.Info("Elevator Sent: " + message);

                            // 더 이상 사용하지 않을 예정이면 null 처리
                            sendData = null;

                            // 5) 응답 수신 루프 (\r\n 까지 읽기)
                            try
                            {
                                while (true)
                                {
                                    recvLength = stream.Read(recvBuff, 0, recvBuff.Length);

                                    // 서버에서 연결을 끊은 경우
                                    if (recvLength == 0)
                                    {
                                        EventLogger.Warn($"[Elevator][SendRecvAsync] : 서버에서 연결을 종료했습니다. (recvLength=0)");
                                        break;
                                    }

                                    string chunk = Encoding.ASCII.GetString(recvBuff, 0, recvLength);
                                    response += chunk;

                                    // ETX(\r\n) 수신 시 루프 탈출
                                    if (response.IndexOf("\r\n", StringComparison.Ordinal) != -1)
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // ReadTimeout 등 수신 중 오류
                                //EventLogger.Warn("Elevator SendRecvAsync() : 수신 중 예외 발생 (TimeOut 또는 네트워크 오류 가능).");
                                main.LogExceptionMessage(ex);
                            }

                            //ProtocolLogger.Info($"[Elevator][SendRecvAsync][Received], Recv: {response}");

                            // 6) 응답이 있다면 프로토콜 파싱
                            if (response.Length > 0)
                            {
                                // ★ 중요: 마지막 recvBuff 조각이 아니라 "전체 response 문자열"로 파싱해야 함
                                byte[] recvDataBytes = Encoding.ASCII.GetBytes(response);
                                return MakeRecvData(recvDataBytes);
                            }
                            else
                            {
                                EventLogger.Warn($"[Elevator][SendRecvAsync] : 수신된 응답 문자열이 비어 있습니다.");
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 연결 또는 송수신 과정에서 발생한 예외
                        elevatorStateUpdate(nameof(State.PROTOCOLERROR));
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                // TcpClient 생성 등 최상위 예외
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
                main.LogExceptionMessage(ex);
            }

            return false;
        }
    }
}