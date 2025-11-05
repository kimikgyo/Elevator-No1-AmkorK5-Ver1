using Common.Models;

namespace Elevator1.MQTTs.interfaces
{
    public interface IUnitofWorkMqttQueue
    {
        void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value);

        void HandleReceivedMqttMessage();
    }
}