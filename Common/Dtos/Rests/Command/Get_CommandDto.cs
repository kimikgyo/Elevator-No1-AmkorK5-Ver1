using Common.Models;
using System.Text.Json.Serialization;

namespace Common.Dtos.Rests.Command
{
    public class Get_CommandDto
    {
        [JsonPropertyOrder(1)] public string commnadId { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public int sequence { get; set; }

        [JsonPropertyOrder(7)] public string WorkerId { get; set; }
        [JsonPropertyOrder(8)] public string actionName { get; set; }
        [JsonPropertyOrder(9)] public List<Parameter> parameters { get; set; }

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
               $",sequence = {sequence,-5}" +
               $",WorkerId = {WorkerId,-5}" +
               $",actionName = {actionName,-5}" +
               $",parameters = {parameters,-5}";
        }
    }
}
