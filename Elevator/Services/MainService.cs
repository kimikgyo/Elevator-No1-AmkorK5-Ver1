using Common.Dtos;
using Common.Models;
using Data;
using Data.Interfaces;
using Elevator1.Mappings.interfaces;
using Elevator1.MQTTs.interfaces;
using log4net;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Elevator.Services
{
    public partial class MainService
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");
        private static readonly ILog ProtocolLogger = LogManager.GetLogger("Protocol");
        private static readonly ILog StatusLogger = LogManager.GetLogger("Status");

        private int ConnectedCount = 0;
        private int elevatorOpenRetry = 0;
        private Status elevator = null;
        private ProtocolDto elevatorProtocolDto = null;
        private MQTTService mQTT = null;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        public readonly IMqttWorker _mqtt;

        public MainService(IUnitOfWorkRepository unitOfWorkRepository, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue, IMqttWorker mqtt)
        {
            _repository = unitOfWorkRepository;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
            _mqtt = mqtt;
            createClass();
            Start();
        }

        private void createClass()
        {
            this.elevatorProtocolDto = new ProtocolDto();

            mQTT = new MQTTService(_mqtt, _mqttQueue);
        }

        public void Start()
        {
            createStatus();
            Task.Run(() => Loop());
            Task.Run(() => log_DataDelete());
        }

        private async Task log_DataDelete()
        {
            while (true)
            {
                try
                {
                    int deleteAddDay = 180;// 30;
                    DateTime searchDateTime = DateTime.Now.AddDays(-(deleteAddDay));
                    PastLogDelete(searchDateTime);

                    //12시간 대기
                    await Task.Delay(43200000);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }

        private async void Loop()
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
                    LogExceptionMessage(ex);
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
                        LogExceptionMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                elevatorStateUpdate(nameof(State.PROTOCOLERROR));
                LogExceptionMessage(ex);
            }
            return false;
        }

        private void LogExceptionMessage(Exception ex)
        {
            string message = ex.InnerException?.Message ?? ex.Message;
            Debug.WriteLine(message);
            EventLogger.Info(message + "\n[StackTrace]\n" + ex.StackTrace);
        }

        private void elevatorModeUpdate(string mode)
        {
            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator != null && elevator.mode != mode)
            {
                elevator.mode = mode;
                elevator.updateAt = DateTime.Now;
                _repository.ElevatorStatus.Update(elevator);
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.MqttPublishStatus(elevator));
            }
        }

        private void elevatorStateUpdate(string state)
        {
            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator != null && elevator.state != state)
            {
                if (elevator.state != nameof(State.DISCONNECT) && state == nameof(State.CONNECT)) return;
                if (elevator.state != nameof(State.CONNECT) && state == nameof(State.DISCONNECT)) return;

                elevator.state = state;
                elevator.updateAt = DateTime.Now;
                _repository.ElevatorStatus.Update(elevator);
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.MqttPublishStatus(elevator));
            }
        }

        private void createStatus()
        {
            this.elevator = new Status
            {
                id = ConfigData.ElevatorSetting.id,
                mode = ConfigData.ElevatorSetting.mode,
                name = "Elevator",
                state = nameof(State.DISCONNECT),
                createAt = DateTime.Now,
            };
            _repository.ElevatorStatus.Add(elevator);
            _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.MqttPublishStatus(elevator));
        }

        private void CommandStateUpdate(string commandId, string state)
        {
            var command = _repository.Commands.GetById(commandId);
            if (command != null && command.state != state)
            {
                command.state = state;
                if (command.state == nameof(CommandState.COMPLETED) || command.state == nameof(CommandState.CANCELED))
                {
                    command.finishedAt = DateTime.Now;
                    _repository.Commands.Remove(command);
                }
                else
                {
                    command.updatedAt = DateTime.Now;
                    _repository.Commands.Update(command);
                }
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.MqttPublishCommand(command));
            }
        }

        /// <summary>
        /// 생성된 로그 폴더 구조(날짜 폴더 → JobScheduler → 파일)에 맞추어
        /// 오래된 로그 디렉토리를 삭제하는 메소드.
        ///
        /// 로그 생성 구조 예:
        /// \Log\ACS\2025-11-27\JobScheduler\_ApiEvent.log
        /// </summary>
        private void PastLogDelete(DateTime searchDateTime)
        {
            try
            {
                // 1) 로그 루트 경로: \Log\ACS
                // log4net 설정의 <file value="\Log\ACS\" /> 와 동일한 기준 경로
                string logRoot = @"C:\Log\ACS";

                // 루트 폴더가 없다면 삭제할 것도 없으므로 종료
                if (!Directory.Exists(logRoot)) return;

                // 2) 날짜 폴더 목록 가져오기
                // 예: \Log\ACS\2025-11-27, \Log\ACS\2025-11-25 등
                foreach (var dateDir in Directory.GetDirectories(logRoot))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dateDir);

                    // 3) 폴더명이 yyyy-MM-dd 형식인지 확인
                    // 올바른 날짜 폴더만 삭제 검사 대상으로 삼는다.
                    // 날짜 형식이 아니면 로그 폴더가 아니므로 스킵
                    if (!DateTime.TryParseExact(dirInfo.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime folderDate)) continue;

                    // 4) 날짜 비교: searchDateTime 이전 날짜면 삭제 대상
                    if (folderDate < searchDateTime)
                    {
                        // 날짜 폴더 안의 JobScheduler 폴더 경로
                        // 예: \Log\ACS\2025-11-27\JobScheduler
                        string jobSchedulerPath = Path.Combine(dateDir, "Elevator1");

                        // 5) JobScheduler 폴더가 있으면 그 하위 모든 파일/폴더 삭제
                        if (Directory.Exists(jobSchedulerPath))
                        {
                            // true = 하위 파일과 디렉토리 포함 전체 삭제
                            Directory.Delete(jobSchedulerPath, true);
                        }

                        // 6) 날짜 폴더가 비었으면 날짜 폴더도 삭제
                        // 로그 파일만 삭제하면 날짜 폴더가 빈 폴더로 남을 수 있으므로 정리 필요
                        bool isEmpty = Directory.GetFiles(dateDir).Length == 0 && Directory.GetDirectories(dateDir).Length == 0;

                        if (isEmpty)
                        {
                            Directory.Delete(dateDir, true);
                        }

                        // 7) 로그 출력 (삭제되었다는 기록)
                        EventLogger.Info($"deleteSystemLogFile_Time() : deleted {dirInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 8) 예상치 못한 오류 기록
                LogExceptionMessage(ex);
            }
        }
    }
}