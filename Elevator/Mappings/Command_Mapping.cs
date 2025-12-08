using Common.Dtos;
using Common.Dtos.Mqtts.Command;
using Common.Dtos.Rests;
using Common.Dtos.Rests.Command;
using Common.Models;
using System.Text.Json;

namespace Elevator_NO1.Mappings
{
    public class Command_Mapping
    {
        public Get_CommandDto Get(Command model)
        {
            var Get = new Get_CommandDto()
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

            return Get;
        }

        public Command Post(Post_CommandDto aPIAddRequestDto, string actionName)
        {
            var Post = new Command
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

            return Post;
        }

        public Publish_CommandDto Publish_Command(Command model)
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