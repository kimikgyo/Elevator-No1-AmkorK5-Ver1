using Common.Dtos;
using Common.Models;
using Data.Interfaces;
using Elevator1.Mappings.interfaces;
using Elevator1.MQTTs.interfaces;
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
            ResponseDtoCommand responseDto = null;
            var command = _repository.Commands.GetAll().FirstOrDefault();
            if (command != null)
            {
                 responseDto = _mapping.CommandMappings.Response(command);
            }

            return Ok(responseDto);
        }
        // POST api/<ValuesController>
        [HttpPost]
        public ActionResult Post([FromBody] APIAddRequestDtoCommand add)
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
                    _mqttQueue.MqttPublishMessage(TopicType.No_1, TopicSubType.command, _mapping.CommandMappings.MqttPublishCommand(command));
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

        private Command CreateCommand(APIAddRequestDtoCommand addRequestDto)
        {
            Command command = null;
            string subtype = addRequestDto.subType.ToUpper();
            string actionName = null;
            switch (subtype)
            {
                case nameof(SubType.DOOROPEN):
                    actionName = nameof(CommandAction.DOOROPEN);
                    break;

                case nameof(SubType.DOORCLOSE):
                    actionName = nameof(CommandAction.DOORCLOSE);
                    break;

                case nameof(SubType.SOURCEFLOOR):
                    var sourceparameter1 = addRequestDto.parameters.FirstOrDefault();
                    if (sourceparameter1 != null)
                    {
                        actionName = CreateActionFloor(sourceparameter1.key, sourceparameter1.value);
                    }
                    break;

                case nameof(SubType.DESTINATIONFLOOR):
                    var destparameter = addRequestDto.parameters.FirstOrDefault();
                    if (destparameter != null)
                    {
                        actionName = CreateActionFloor(destparameter.key, destparameter.value);
                    }
                    break;
            }
            if (actionName != null)
            {
                command = _mapping.CommandMappings.APIAddRequest(addRequestDto, actionName);
            }

            return command;
        }

        private string CreateActionFloor(string key, string value)
        {
            string Name = $"{key}_{value}";
            bool existSubTypes = Enum.IsDefined(typeof(CommandAction), Name);
            if (existSubTypes)
            {
                return Name;
            }
            else
            {
                return null;
            }
        }

        private string ConditionAddCommand(APIAddRequestDtoCommand addRequestDto)
        {
            string massage = null;

            if (IsInvalid(addRequestDto.commnadId)) massage = "Check commnadId";
            var runcommand = _repository.Commands.GetAll().FirstOrDefault();
            if(runcommand != null) massage = "There is a command in progress";

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

        //// DELETE api/<ValuesController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}