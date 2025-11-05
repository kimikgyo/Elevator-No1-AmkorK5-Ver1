using Common.Dtos;
using Common.Models;
using System.ComponentModel.Design;
using System.Data;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Xml.Linq;

namespace Elevator1.Mappings
{
    public class CommandMapping
    {
        public ResponseDtoCommand Response(Command model)
        {
            var response = new ResponseDtoCommand()
            {
                commnadId = model.commnadId,
                name = model.name,
                type = model.type,
                subType = model.subType,
                state = model.state,
                WorkerId = model.WorkerId,
                actionName = model.actionName,
                parameters = JsonSerializer.Deserialize<List<Common.Dtos.Parameter>>(model.parametersjson),
            };

            return response;
        }

        public Command APIAddRequest(APIAddRequestDtoCommand aPIAddRequestDto, string actionName)
        {
            var add = new Command
            {
                commnadId = aPIAddRequestDto.commnadId,
                name = aPIAddRequestDto.name,
                type = aPIAddRequestDto.type,
                subType = aPIAddRequestDto.subType,
                state = nameof(CommandState.PENDING),
                WorkerId = aPIAddRequestDto.assignedWorkerId,
                actionName = actionName,
                parametersjson = JsonSerializer.Serialize(aPIAddRequestDto.parameters),
                //preReportsjson = aPIAddRequestDto.preReportsjson
                //postReportsjson = aPIAddRequestDto.postReportsjson
                createdAt = aPIAddRequestDto.createdAt,
                updatedAt = aPIAddRequestDto.updatedAt,
                finishedAt = aPIAddRequestDto.finishedAt,
            };

            return add;
        }

        public MqttPublishDtoCommand MqttPublishCommand(Command model)
        {
            var publish = new MqttPublishDtoCommand()
            {
                commnadId = model.commnadId,
                name = model.name,
                type = model.type,
                subType = model.subType,
                state = model.state,
                WorkerId = model.WorkerId,
                actionName = model.actionName,
                parameters = JsonSerializer.Deserialize<List<Common.Dtos.Parameter>>(model.parametersjson),
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt
            };
            return publish;
        }
    }
}