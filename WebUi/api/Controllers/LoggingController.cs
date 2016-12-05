using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebUi.api.Logging;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/logging")]
    public class LoggingController : ApiController
    {
        readonly InMemoryLogger _logmodule;
        const string All = "all";

        public LoggingController(InMemoryLogger logmodule)
        {
            _logmodule = logmodule;
        }

        public IHttpActionResult Get(string module = All, string level = All)
        {
            var log = module.EqualsIgnoreCase(All) ? 
                _logmodule.GetAll() : 
                _logmodule.GetByModule(module);
            var levelsFilter = _logmodule.GetLevelsFilter(level);
            log = log.Where(l => levelsFilter.Contains(l.Level))
                     .OrderByDescending(l => l.Timestamp);
            return Ok(log);
        }

        [Route("levels")]
        [HttpGet]
        public IHttpActionResult Levels()
        {
            return Ok(_logmodule.GetLevels());
        }
    }
}