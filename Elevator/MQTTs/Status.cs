using Common.Models;
using Common.Models.Queues;

namespace Elevator_NO1.MQTTs
{
    public partial class MqttProcess
    {
        public void Status()
        {
            while (QueueStorage.MqttTryDequeuePublisStatus(out MqttPublishMessageDto cmd))
            {
                try
                {
                    _mqttWorker.PublishAsync(cmd.Topic, cmd.Payload).Wait();
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}