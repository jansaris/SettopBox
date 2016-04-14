using System.Web.Http;
using WebUi.api.Logging;

namespace WebUi.api.Controllers
{
    public class LoggingController : ApiController
    {
        readonly InMemoryLogger _logmodule;

        public LoggingController(InMemoryLogger logmodule)
        {
            _logmodule = logmodule;
        }

        public IHttpActionResult Get()
        {
            return Ok(_logmodule.GetAll());
        }

        public IHttpActionResult Get(string name)
        {
            switch (name)
            {
                case "Levels":
                    return Ok(_logmodule.GetLevels());
                default:
                    return Ok(_logmodule.GetByModule(name));
            }
        }

        public IHttpActionResult Levels()
        {
            return Ok(_logmodule.GetLevels());
        }
    }
}