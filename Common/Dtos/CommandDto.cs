using System.Text.Json.Serialization;

namespace Common.Dtos
{
    public class APIAddRequestDtoCommand
    {
        [JsonPropertyOrder(1)] public string guid { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string service { get; set; }
        [JsonPropertyOrder(4)] public string type { get; set; }
        [JsonPropertyOrder(5)] public string subType { get; set; }
        [JsonPropertyOrder(6)] public int sequence { get; set; }
        [JsonPropertyOrder(7)] public string state { get; set; }
        [JsonPropertyOrder(8)] public string? assignedWorkerId { get; set; }
        [JsonPropertyOrder(12)] public List<Parameter> parameters { get; set; }
        //[JsonPropertyOrder(13)] public List<PreReport> preReports { get; set; }
        //[JsonPropertyOrder(14)] public List<postReport> postReports { get; set; }

        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
                var items = parameters
                    .Select(p => $"{{ key={p.key}, value={p.value} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                parametersStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                parametersStr = "{}";
            }

            //if (preReports != null && preReports.Count > 0)
            //{
            //    // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
            //    var items = preReports
            //        .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

            //    // 여러 개 항목을 ", " 로 이어붙임
            //    preReportsStr = string.Join(", ", items);
            //}
            //else
            //{
            //    preReportsStr = "{}";
            //}

            //if (postReports != null && postReports.Count > 0)
            //{
            //    // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
            //    var items = postReports
            //        .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

            //    // 여러 개 항목을 ", " 로 이어붙임
            //    postReportsStr = string.Join(", ", items);
            //}
            //else
            //{
            //    postReportsStr = "{}";
            //}
            return

          $"guid = {guid,-5}" +
          $",name = {name,-5}" +
          $",service = {service,-5}" +
          $",type = {type,-5}" +
          $",subType = {subType,-5}" +
          $",sequence = {sequence,-5}" +
          $",state = {state,-5}" +
          $",assignedWorkerId = {assignedWorkerId,-5}" +
          $",parameters = {parametersStr,-5}";
            //$",preReports = {preReports,-5}" +
            //$",postReports = {postReports,-5}";
        }
    }

    public class MqttPublishDtoCommand
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

    public class ResponseDtoCommand
    {
        [JsonPropertyOrder(1)] public string commnadId { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public string WorkerId { get; set; }
        [JsonPropertyOrder(7)] public string actionName { get; set; }
        [JsonPropertyOrder(8)] public List<Parameter> parameters { get; set; }

        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
                var items = parameters
                    .Select(p => $"{{ key={p.key}, value={p.value} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                parametersStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                parametersStr = "{}";
            }
            return
               $"commnadId = {commnadId,-5}" +
               $",name = {name,-5}" +
               $",type = {type,-5}" +
               $",subType = {subType,-5}" +
               $",state = {state,-5}" +
               $",WorkerId = {WorkerId,-5}" +
               $",actionName = {actionName,-5}" +
               $",parameters = {parameters,-5}";
        }
    }

    public class Parameter
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    //public class PreReport
    //{
    //}

    //public class postReport
    //{
    //}
}