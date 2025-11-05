using Common.Models;
using Common.Models.Queues;

namespace Elevator1.MQTTs
{
    public partial class MqttProcess
    {
        public void Command()
        {
            while (QueueStorage.MqttTryDequeuePublisCommand(out MqttPublishMessageDto cmd))
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
