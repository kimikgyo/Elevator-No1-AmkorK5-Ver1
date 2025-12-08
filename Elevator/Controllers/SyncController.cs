using Elevator_NO1.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Elevator_NO1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class syncController : ControllerBase
    {
        private readonly MainService _main;

        public syncController(MainService main)
        {
            _main = main;
        }

        // GET: api/<SyncController>
        [HttpGet]
        public async Task<IActionResult> RunSync()
        {
            await _main.ReloadAndRestartAsync();
            return Ok(new { message = "Data reload scheduler restart complete" });
        }

        //// GET api/<ValuesController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<ValuesController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

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
