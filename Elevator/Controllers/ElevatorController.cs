using Common.Dtos;
using Common.Dtos.Rests;
using Common.Dtos.Rests.Command;
using Common.Models;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Elevator_NO1.MQTTs.interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Elevator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class commandController : ControllerBase
    {
        //// GET api/<ValuesController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}
        private static readonly ILog logger = LogManager.GetLogger("CommandController"); //Function 실행관련 Log

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public commandController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
        }

        //// GET: api/<ValuesController>
        [HttpGet]
        public ActionResult Get()
        {
            Get_CommandDto responseDto = null;
            var command = _repository.Commands.GetAll().FirstOrDefault();
            if (command != null)
            {
                responseDto = _mapping.CommandMappings.Response(command);
            }

            return Ok(responseDto);
        }

        // POST api/<ValuesController>
        [HttpPost]
        public ActionResult Post([FromBody] Post_CommandDto add)
        {
            logger.Info($"AddRequest = {add}");
            string message = ConditionAddCommand(add);
            if (message == null)
            {
                var command = CreateCommand(add);
                if (command != null)
                {
                    logger.Info($"{this.ControllerLogPath()} Response = " +
                                   $"Code = {Ok(message).StatusCode}" +
                                   $",massage = {Ok(message).Value}" +
                                   $",Date = {add}"
                                   );
                    _repository.Commands.Add(command);
                    _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.MqttPublishCommand(command));
                    return Ok(add);
                }
                else
                {
                    logger.Info($"{this.ControllerLogPath()} Response = " +
                          $"Code = {NotFound(message).StatusCode}" +
                          $",massage = {NotFound(message).Value}" +
                          $",Date = {add}"
                          );
                    return NotFound();
                }
            }
            else
            {
                logger.Info($"{this.ControllerLogPath()} Response = " +
                                 $"Code = {NotFound(message).StatusCode}" +
                                 $",massage = {NotFound(message).Value}" +
                                 $",Date = {add}"
                                 );
                return BadRequest(message);
            }
        }

        private Command CreateCommand(Post_CommandDto addRequestDto)
        {
            Command command = null;
            string subtype = addRequestDto.subType.ToUpper();
            var parameter = addRequestDto.parameters.FirstOrDefault();
            if (parameter != null)
            {
                bool existSubTypes = Enum.IsDefined(typeof(CommandAction), parameter.value);
                if (!existSubTypes) return command;
                string actionName = parameter.value;

                if (actionName != null)
                {
                    command = _mapping.CommandMappings.APIAddRequest(addRequestDto, actionName);
                }
            }

            return command;
        }

        private string ConditionAddCommand(Post_CommandDto addRequestDto)
        {
            string massage = null;
            var commandnew = _repository.Commands.GetAll().FirstOrDefault(r => r.commnadId == addRequestDto.guid);
            if (commandnew != null) massage = "Check commnadId";
            if (IsInvalid(addRequestDto.guid)) massage = "Check commnadId";
            //var runcommand = _repository.Commands.GetAll().FirstOrDefault();
            //if (runcommand != null) massage = "There is a command in progress";

            return massage;
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING"
                || value.ToUpper() == "";
        }

        //// PUT api/<ValuesController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var command = _repository.Commands.GetById(id);
            if (command != null)
            {
                CommandStateUpdate(command.commnadId, nameof(CommandState.CANCELED));
                _repository.Commands.Remove(command);
                return Ok();
            }
            else
            {
                return NotFound("DeleteComplete");
            }
        }

        private void CommandStateUpdate(string commandId, string state)
        {
            var command = _repository.Commands.GetById(commandId);
            if (command != null && command.state != state)
            {
                command.state = state;
                if (command.state == nameof(CommandState.COMPLETED) || command.state == nameof(CommandState.CANCELED))
                {
                    command.finishedAt = DateTime.Now;
                    _repository.Commands.Remove(command);
                }
                else
                {
                    command.updatedAt = DateTime.Now;
                    _repository.Commands.Update(command);
                }
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.MqttPublishCommand(command));
            }
        }
    }
}