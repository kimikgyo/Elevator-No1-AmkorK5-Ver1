using Elevator_NO1.MQTTs.interfaces;

namespace Elevator_NO1.Services
{
    public class MQTTService
    {
        public readonly IMqttWorker _mqttWorker;
        public readonly IUnitofWorkMqttQueue _mqttQueue;

        public MQTTService(IMqttWorker mqttWorker, IUnitofWorkMqttQueue mqttQueue)
        {
            _mqttWorker = mqttWorker;
            _mqttQueue = mqttQueue;
            var task = _mqttWorker.StartAsync(CancellationToken.None);
        }

        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    _mqttQueue.HandleReceivedMqttMessage();
                    Thread.Sleep(100);
                }
            });
        }
    }
}