using Common.Models;
using Common.Models.Queues;
using Data;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using log4net;

namespace Elevator_NO1.MQTTs.interfaces
{
    public class UnitofWorkMqttQueue : IUnitofWorkMqttQueue
    {
        private static readonly ILog MqttServiceLogger = LogManager.GetLogger("MQTT");

        private readonly MqttProcess _mqttProcess;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;

        public UnitofWorkMqttQueue(IMqttWorker mqttWorker, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _mqttProcess = new MqttProcess(this, mqttWorker, repository, mapping);
        }

        public void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value)
        {
            lock (this)
            {
                var getByPublish = ConfigData.PublishTopics.FirstOrDefault(t => t.type == $"{topicType}"
                                                                            && t.subType == $"{topicSubType}");
                if (getByPublish == null)
                {
                    MqttServiceLogger.Info($"{nameof(MqttPublishMessage)} = ConfigTopic Flie " +
                                           $" ,type = {topicType} ,subType = {topicSubType}");
                    return;
                }
                else
                {
                    string payload = value.ToJson();

                    switch (getByPublish.subType)
                    {
                        case nameof(TopicSubType.status):
                            QueueStorage.MqttEnqueuePublishStatus(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Status();
                            break;

                        case nameof(TopicSubType.command):
                            QueueStorage.MqttEnqueuePublishCommand(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Command();
                            break;
                    }
                }
            }
        }

        public void MqttSubscribe(MqttSubscribeMessageDto subscribe)
        {
            switch (subscribe.type)
            {
            }
        }

        public void HandleReceivedMqttMessage()
        {
            _mqttProcess.HandleReceivedMqttMessage();
        }
    }
}