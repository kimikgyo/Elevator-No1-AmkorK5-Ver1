using Common.Dtos.Rests.Command;
using Common.Models;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Elevator_NO1.MQTTs.interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Elevator_NO1.Controllers
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
                responseDto = _mapping.CommandMappings.Get(command);
            }

            return Ok(responseDto);
        }

        // POST api/<ValuesController>
        [HttpPost]
        public ActionResult Post([FromBody] Post_CommandDto add)
        {
            if (add == null)
                return BadRequest("Body is null");

            // ------------------------------------------------------------
            // [중요] 외부 서비스가 부여한 Command GUID를 그대로 써야 한다
            // ------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(add.guid))
                return BadRequest("Command guid is required");

            // 1) 유효성 검사
            string message = ConditionAddCommand(add);
            if (message != null)
                return BadRequest(message);

            // 2) 멱등 처리: 같은 guid가 이미 저장되어 있으면 중복 처리하지 않음
            var exists = _repository.Commands.GetAll().FirstOrDefault(c => c != null && c.commnadId == add.guid);

            if (exists != null)
            {
                logger.Info($"[COMMAND][POST][IDEMPOTENT] already exists. cmdGuid={add.guid}, subType={exists.subType}, state={exists.state}");
                return Ok(new { ok = true, cmdGuid = add.guid, duplicated = true });
            }

            // 3) CreateCommand는 "새 guid 만들기"가 절대 있으면 안 된다.
            //    DTO -> Command 변환만 하고 guid는 add.guid 그대로 복사되어야 한다.
            var command = CreateCommand(add);

            if (command == null)
                return BadRequest("CreateCommand failed");

            // 4) 최종 방어: 매핑 결과가 guid를 바꾸지 않았는지 확인
            if (command.commnadId != add.guid)
                return BadRequest("Command guid mismatch (must preserve original guid)");

            _repository.Commands.Add(command);

            _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.Publish_Command(command));

            return Ok(new { ok = true, cmdGuid = command.commnadId, duplicated = false });
        }

        private Command CreateCommand(Post_CommandDto addRequestDto)
        {
            Command command = null;

            if (addRequestDto == null) return null;
            if (string.IsNullOrWhiteSpace(addRequestDto.guid)) return null;

            // Action 찾기 (네 기존 로직 유지 가능)
            string actionName = null;

            if (addRequestDto.parameters != null)
            {
                foreach (var p in addRequestDto.parameters)
                {
                    if (p == null) continue;
                    if (string.IsNullOrWhiteSpace(p.value)) continue;

                    string v = p.value.Trim().ToUpperInvariant();
                    bool exist = Enum.IsDefined(typeof(CommandAction), v);
                    if (exist == true)
                    {
                        actionName = v;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            // 매핑으로 생성
            command = _mapping.CommandMappings.Post(addRequestDto, actionName);
            if (command == null) return null;

            // ------------------------------------------------------------
            // [핵심] 외부에서 받은 guid를 반드시 유지
            // - 매핑에서 새 guid 만들었더라도 여기서 원복
            // ------------------------------------------------------------
            command.commnadId = addRequestDto.guid;

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
                _mqttQueue.MqttPublishMessage(TopicType.NO1, TopicSubType.command, _mapping.CommandMappings.Publish_Command(command));
            }
        }
    }
}