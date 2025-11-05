using Common.Models;

namespace Data
{
    public static class ConfigData
    {
        public static List<MqttTopicSubscribe> SubscribeTopics { get; set; }
        public static List<MqttTopicPublish> PublishTopics { get; set; }
        public static MQTTSetting MQTTSetting { get; set; }
        public static ElevatorSetting ElevatorSetting { get; set; }
        public static void Load(IConfiguration configuration)
        {
            ConfigData.MQTTSetting = configuration.GetSection("MQTTSetting").Get<MQTTSetting>();
            ConfigData.SubscribeTopics = configuration.GetSection("MqttTopicSubscribe").Get<List<MqttTopicSubscribe>>();
            ConfigData.PublishTopics = configuration.GetSection("MqttTopicPublish").Get<List<MqttTopicPublish>>();
            ConfigData.ElevatorSetting = configuration.GetSection("ElevatorSetting").Get<ElevatorSetting>();
        }
    }
    public class ElevatorSetting
    {
        public string ip { get; set; }
        public string id { get; set; }
        public string port { get; set; }
        public string timeout { get; set; }
    }
    public class MQTTSetting
    {
        public string id { get; set; }
        public string host { get; set; }
        public string port { get; set; }
    }

    public class MqttTopicPublish
    {
        public string type { get; set; }
        public string subType { get; set; }
        public string topic { get; set; }
    }
    public class MqttTopicSubscribe
    {
        public string topic { get; set; }
    }
}