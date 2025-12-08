using Common.Models;

namespace Elevator_NO1.MQTTs.interfaces
{
    public interface IUnitofWorkMqttQueue
    {
        void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value);

        void HandleReceivedMqttMessage();
    }
}