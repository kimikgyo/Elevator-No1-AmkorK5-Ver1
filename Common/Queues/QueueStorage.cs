using System.Collections.Concurrent;

namespace Common.Models.Queues
{
    public static class QueueStorage
    {
        #region MQTT

        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishStatus = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishCommand = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttMessagesSubscribe = new ConcurrentQueue<MqttSubscribeMessageDto>();

        public static void MqttEnqueuePublishStatus(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishStatus.Enqueue(item);
        }

        public static bool MqttTryDequeuePublisStatus(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishStatus.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishCommand(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishCommand.Enqueue(item);
        }

        public static bool MqttTryDequeuePublisCommand(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishCommand.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribe(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttMessagesSubscribe.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribe(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttMessagesSubscribe.TryDequeue(out item);
        }

        #endregion MQTT
    }
}