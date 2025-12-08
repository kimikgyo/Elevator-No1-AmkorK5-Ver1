using Common.Dtos;
using Common.Models;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Elevator_NO1.MQTTs.interfaces;
using log4net;

namespace Elevator_NO1.Services
{
    public partial class Elevator_No1_Service
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");
        private static readonly ILog ProtocolLogger = LogManager.GetLogger("Protocol");
        private static readonly ILog StatusLogger = LogManager.GetLogger("Status");

        private MainService main = null;
        private ProtocolDto elevatorProtocolDto = null;

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        private readonly IMqttWorker _mqtt;

        private int ConnectedCount = 0;
        private int elevatorOpenRetry = 0;

        //    현재 실행 중인 작업들을 추적하기 위한 리스트
        //    - Stop/재시작 시 Task가 겹치지 않게 관리하고
        //    - 종료 대기(WhenAll) 및 예외 추적에 사용
        private List<Task> _tasks = new();

        //     스케줄러의 실행 여부 플래그
        //    - 무한루프 탈출 조건으로 사용 (while(_running))
        //    - Start/Stop 간 레이스를 줄이려면 bool 대신 volatile 추천
        private bool _running;

        public Elevator_No1_Service(MainService mainService, IUnitOfWorkRepository unitOfWorkRepository, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue, IMqttWorker mqtt)
        {
            main = mainService;
            _repository = unitOfWorkRepository;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
            _mqtt = mqtt;
            elevatorProtocolDto = new ProtocolDto();
        }

        /// <summary>
        /// 스케줄러의 모든 무한루프 작업을 시작합니다.
        /// </summary>
        public void Start()
        {
            // [중복 실행 방지]
            // 이미 실행 중이면 다시 시작하지 않도록 가드.
            // - 중복 Start는 같은 루프가 2개 이상 떠서 상태가 꼬일 수 있음.
            if (_running) return;

            // [실행 플래그 on]
            // - 아래 Task들이 while(_running) 조건을 보고 동작하므로
            //   Start 전에 반드시 true 로 세팅해야 함.
            _running = true;

            // [Task 컨테이너 초기화]
            // - 이전 실행 기록이 남아있지 않도록 매번 새 리스트로 준비.
            _tasks = new List<Task>
             {
                Task.Run(() => ElevatorTCPClient())
            };
        }

        /// <summary>
        /// Stop 요청 후 모든 Task가 종료될 때까지 대기
        /// </summary>
        public async Task StopAsync()
        {
            if (!_running) return;

            _running = false;  // 루프 종료 신호

            // [실제 종료 대기]
            if (_tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(_tasks);  // 모든 Task 종료 대기
                    EventLogger.Info($"[StopAsync] Elevator_No1 Task Stop");
                }
                catch (Exception ex)
                {
                    // Task 내부 예외 로깅
                    EventLogger.Info($"[StopAsync] Elevator_No1 Task Stop Error : {ex.Message}");
                }
            }

            _tasks.Clear();
        }

        private void elevatorModeUpdate(string mode)
        {
            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator != null && elevator.mode != mode)
            {
                elevator.mode = mode;
                elevator.updateAt = DateTime.Now;
                _repository.ElevatorStatus.Update(elevator);
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.Publish_Status(elevator));
                var Resource = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "Resource");
                if (Resource != null)
                {
                    var mapping = _mapping.SettingMappings.Request(elevator);
                    Resource.Api.Patch_Elevators_Async(elevator.id, mapping);
                }
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
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.Publish_Status(elevator));
            }
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
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.Publish_Command(command));
            }
        }
    }
}