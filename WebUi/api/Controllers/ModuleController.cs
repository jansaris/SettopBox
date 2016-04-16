using System.Linq;
using System.Web.Http;
using SharedComponents.Module;
using WebUi.api.Models;

namespace WebUi.api.Controllers
{
    [RoutePrefix("api/module")]
    public class ModuleController : ApiController
    {
        readonly ModuleCommunication _info;

        public ModuleController(ModuleCommunication info)
        {
            _info = info;
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