using Common.Models;
using Data.Interfaces;
using Elevator_NO1.Mappings.interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Elevator_NO1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;

        public StatusController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mapping = mapping;
        }
        // GET: api/<StatusController>
        [HttpGet]
        public ActionResult<Status> Get()
        {
            var tstatus = _repository.ElevatorStatus.GetAll();
            var status = _repository.ElevatorStatus.GetAll().FirstOrDefault(r => r.id == "NO1");
            return status;
        }

        // GET api/<StatusController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<StatusController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<StatusController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<StatusController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
