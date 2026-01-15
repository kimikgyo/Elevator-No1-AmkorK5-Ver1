namespace Common.Models
{
    public enum TopicType
    {
        NO1,
        elevator,
    }

    public enum TopicSubType
    {
        camera,
        status,
        command
    }

    public class MqttPublishMessageDto
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MqttSubscribeMessageDto
    {
        public string id { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string topic { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    //사용안함
    //public class MqttMessage
    //{
    //    public string Topic { get; set; }
    //    public string Payload { get; set; }
    //    public DateTime Timestamp { get; set; }
    //}
}