using Common.Dtos;
using Common.Models;
using Data;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Elevator_NO1.MQTTs.interfaces;
using Elevator_NO1.Services.Data;
using log4net;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Elevator_NO1.Services
{
    public class MainService
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        private MainService main = null;
        private Status elevator = null;
        private MQTTService mQTT = null;
        private Elevator_No1_Service elevator_No1 = null;
        private Response_Data response_Data = null;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        public readonly IMqttWorker _mqtt;

        public MainService(IUnitOfWorkRepository unitOfWorkRepository, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue, IMqttWorker mqtt)
        {
            main = this;
            _repository = unitOfWorkRepository;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
            _mqtt = mqtt;
            createClass();
            stratAsync();
        }

        private void createClass()
        {
            elevator_No1 = new Elevator_No1_Service(main, _repository, _mapping, _mqttQueue, _mqtt);
            mQTT = new MQTTService(_mqtt, _mqttQueue);
            response_Data = new Response_Data(EventLogger, _repository, _mapping);
        }

        private void Start()
        {
            Task.Run(() => log_DataDelete());
        }

        private async Task stratAsync()
        {
            Start();
            bool response_DataCompleted = await response_Data.StartAsyc();
            if (response_DataCompleted)
            {
                var setting = _repository.Settings.GetAll().FirstOrDefault(e => e.id == "NO1");
                if (setting != null)
                {
                    createStatus(setting.id, setting.mode);
                    mQTT.Start();
                    elevator_No1.Start();
                }
            }
        }

        /// <summary>
        /// 스케줄러를 멈춘 뒤, 데이터 리로드 → 다시 시작
        /// </summary>
        public async Task ReloadAndRestartAsync()
        {
            // 1. 스케줄러 정지 (Task 종료될 때까지 대기)
            await elevator_No1.StopAsync();
            // StopAsync 내부에서 while 루프 빠져나오고 Task.WhenAll() 대기하도록 구현
            bool Response_Data_Complete = await response_Data.StartAsyc();
            if (Response_Data_Complete)
            {
                //// 3. MQTT 다시 시작 (필요시)
                //_mqtt.Start();

                // 4. 스케줄러 다시 시작
                elevator_No1.Start();
            }
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

        public void LogExceptionMessage(Exception ex)
        {
            string message = ex.InnerException?.Message ?? ex.Message;
            Debug.WriteLine(message);
            EventLogger.Info(message + "\n[StackTrace]\n" + ex.StackTrace);
        }

        private void createStatus(string id, string mode)
        {
            this.elevator = new Status
            {
                id = id,
                mode = mode,
                name = "Elevator",
                state = nameof(State.DISCONNECT),
                createAt = DateTime.Now,
            };
            _repository.ElevatorStatus.Add(elevator);
            _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.Publish_Status(elevator));
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
                        string jobSchedulerPath = Path.Combine(dateDir, "Elevator_NO1");

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