using Common.Models;
using Common.Models.Queues;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Elevator_NO1.MQTTs.interfaces;
using log4net;
using System.Diagnostics;

namespace Elevator_NO1.MQTTs
{
    public partial class MqttProcess
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");
        private readonly IMqttWorker _mqttWorker;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly UnitofWorkMqttQueue _mqttQueue;

        public MqttProcess(UnitofWorkMqttQueue mqttQueue, IMqttWorker mqttWorker, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _mqttQueue = mqttQueue;
            _mqttWorker = mqttWorker;
            _repository = repository;
            _mapping = mapping;
        }

        public void HandleReceivedMqttMessage()
        {
            while (QueueStorage.MqttTryDequeueSubscribe(out MqttSubscribeMessageDto message))
            {
                try
                {
                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", message.topic, message.Payload, message.Timestamp));

                    if (string.IsNullOrWhiteSpace(message.topic)) return;
                    if (string.IsNullOrWhiteSpace(message.Payload)) return;     // 페이로드 null check
                    if (!message.Payload.IsValidJson()) return;                 // 페이로드 json check
                    string[] topic = message.topic.Split('/');

                    message.type = topic[1];
                    message.id = topic[2];
                    message.subType = topic[3];

                    _mqttQueue.MqttSubscribe(message);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }

        public void elevatorStateUpdate(string state)
        {

            var elevator = _repository.ElevatorStatus.GetAll().FirstOrDefault();
            if (elevator != null && elevator.state != state)
            {
                if (elevator.state != nameof(State.DISCONNECT) && state == nameof(State.CONNECT)) return;
                if (elevator.state != nameof(State.CONNECT) && state == nameof(State.DISCONNECT)) return;
                if (elevator.state == nameof(State.PAUSE)&& state != nameof(State.RESUME)) return;
                elevator.state = state;
                elevator.updateAt = DateTime.Now;
                _repository.ElevatorStatus.Update(elevator);
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.status, _mapping.StatusMappings.Publish_Status(elevator));
            }
        }
        public void LogExceptionMessage(Exception ex)
        {
            //string message = ex.InnerException?.Message ?? ex.Message;
            //string message = ex.ToString();
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            EventLogger.Info(message);
        }
    }
}