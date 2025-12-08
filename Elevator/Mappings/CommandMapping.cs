using Common.Dtos;
using Common.Dtos.Mqtts.Command;
using Common.Dtos.Rests;
using Common.Dtos.Rests.Command;
using Common.Models;
using System.Text.Json;

namespace Elevator_NO1.Mappings
{
    public class CommandMapping
    {
        public Get_CommandDto Response(Command model)
        {
            var response = new Get_CommandDto()
            {
                commnadId = model.commnadId,
                name = model.name,
                type = model.type,
                subType = model.subType,
                state = model.state,
                WorkerId = model.WorkerId,
                actionName = model.actionName,
                parameters = JsonSerializer.Deserialize<List<Parameter>>(model.parameterJson),
            };

            return response;
        }

        public Command APIAddRequest(Post_CommandDto aPIAddRequestDto, string actionName)
        {
            var add = new Command
            {
                commnadId = aPIAddRequestDto.guid,
                name = aPIAddRequestDto.name,
                type = aPIAddRequestDto.type,
                subType = aPIAddRequestDto.subType,
                state = nameof(CommandState.PENDING),
                WorkerId = aPIAddRequestDto.assignedWorkerId,
                actionName = actionName,
                parameterJson = JsonSerializer.Serialize(aPIAddRequestDto.parameters),

                createdAt = DateTime.Now,
            };

            return add;
        }

        public Publish_CommandDto MqttPublishCommand(Command model)
        {
            var publish = new Publish_CommandDto()
            {
                commnadId = model.commnadId,
                name = model.name,
                type = model.type,
                subType = model.subType,
                state = model.state,
                WorkerId = model.WorkerId,
                actionName = model.actionName,
                parameterJson = model.parameterJson,
            };
            return publish;
        }
    }
}