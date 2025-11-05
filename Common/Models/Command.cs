using System.Text.Json.Serialization;

namespace Common.Models
{
    public enum Type
    {
        ACTION
    }

    public enum SubType
    {
        SOURCEFLOOR,
        DOOROPEN,
        DOORCLOSE,
        DESTINATIONFLOOR,
    }

    public enum Mode
    {
        AGVMODE,
        NOTAGVMODE
    }

    public enum CommandAction
    {
        None = 0,
        AGVMODE,
        NOTAGVMODE,
        DOOROPEN,
        DOORCLOSE,
        CALL_B1F,
        CALL_1F,
        CALL_2F,
        CALL_3F,
        CALL_4F,
        CALL_5F,
        CALL_6F,
        GOTO_B1F,
        GOTO_1F,
        GOTO_2F,
        GOTO_3F,
        GOTO_4F,
        GOTO_5F,
        GOTO_6F,
    }

    public enum CommandState
    {
        PENDING,
        REQUEST,
        REQUESTCOMPLETED,
        EXECUTING,
        CANCELED,
        FAILED,
        COMPLETED
    }

    public class Command
    {
        [JsonPropertyOrder(1)] public string commnadId { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public string WorkerId { get; set; }
        [JsonPropertyOrder(7)] public string actionName { get; set; }
        [JsonPropertyOrder(8)] public string parametersjson { get; set; }
        //[JsonPropertyOrder(9)] public string preReportsjson { get; set; }
        //[JsonPropertyOrder(10)] public string postReportsjson { get; set; }
        [JsonPropertyOrder(11)] public DateTime createdAt { get; set; }
        [JsonPropertyOrder(12)] public DateTime? updatedAt { get; set; }
        [JsonPropertyOrder(13)] public DateTime? finishedAt { get; set; }

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
               $",parametersjson = {parametersjson,-5}" +
               //$",preReportsjson = {preReportsjson,-5}" +
               //$",postReportsjson = {postReportsjson,-5}" +
               $",createdAt = {createdAt,-5}" +
               $",updatedAt = {updatedAt,-5}" +
               $",finishedAt = {finishedAt,-5}";
        }
    }
}