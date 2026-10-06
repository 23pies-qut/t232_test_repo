using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Models;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly SqlConnectionFactory _db;

        public HealthController(SqlConnectionFactory db)
        {
            _db = db;
        }

        // GET api/Health - whether the database is reachable and the tables and stored procedures are installed
        [HttpGet]
        public async Task<ActionResult<HealthReport>> Get() => Ok(await _db.CheckAsync());
    }
}
