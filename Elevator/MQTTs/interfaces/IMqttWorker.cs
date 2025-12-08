namespace Elevator_NO1.MQTTs.interfaces
{
    public interface IMqttWorker
    {
        Task PublishAsync(string topic, string payload);

        Task StartAsync(CancellationToken cancellationToken);

        void Dispose();
    }
}