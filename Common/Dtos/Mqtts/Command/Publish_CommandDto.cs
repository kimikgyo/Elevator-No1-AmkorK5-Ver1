using System.Text.Json.Serialization;

namespace Common.Dtos.Mqtts.Command
{
    public class Publish_CommandDto
    {
        [JsonPropertyOrder(1)] public string commnadId { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public string WorkerId { get; set; }
        [JsonPropertyOrder(7)] public string actionName { get; set; }
        [JsonPropertyOrder(8)] public string parameterJson { get; set; }

        public override string ToString()
        {
            return
               $"commnadId = {commnadId,-5}" +
               $",name = {name,-5}" +
               $",type = {type,-5}" +
               $",subType = {subType,-5}" +
               $",state = {state,-5}" +
               $",WorkerId = {WorkerId,-5}" +
               $",actionName = {actionName,-5}" +
               $",parameterJson = {parameterJson,-5}";
        }
    }
}