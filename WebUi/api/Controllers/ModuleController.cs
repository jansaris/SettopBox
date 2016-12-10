using System.Linq;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Cors;
using log4net;
using SharedComponents.Module;
using WebUi.api.Models;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/module")]
    public class ModuleController : ApiController
    {
        readonly ModuleCommunication _info;
        readonly ILog _logger;

        public ModuleController(ModuleCommunication info, ILog logger)
        {
            _info = info;
            _logger = logger;
        }

        public IHttpActionResult Get()
        {
            return Ok(_info.Modules.Select(Map));
        }

        [Route("names")]
        [HttpGet]
        public IHttpActionResult Names()
        {
            return Ok(_info.Modules);
        }

        [Route("start/{name}")]
        [HttpPost]
        public IHttpActionResult Start(string name)
        {
            _logger.Debug($"Start module {name}");
            _info.Start(name);
            Thread.Sleep(100);
            return Ok(Map(name));
        }

        [HttpPost]
        [Route("stop/{name}")]
        public IHttpActionResult Stop(string name)
        {
            _logger.Debug($"Stop module {name}");
            _info.Stop(name);
            Thread.Sleep(100);
            return Ok(Map(name));
        }

        public IHttpActionResult Get(string name)
        {
            return Ok(Map(name));
        }

        Module Map(string name)
        {
            return new Module
            {
                Name = name,
                Enabled = _info.Enabled(name),
                Status = _info.Status(name),
                Info = _info.Info(name)
            };
        }
    }
}