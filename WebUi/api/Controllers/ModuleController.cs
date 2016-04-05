using System.Linq;
using System.Web.Http;
using SharedComponents;
using SharedComponents.Module;
using WebUi.api.Models;

namespace WebUi.api.Controllers
{
    public class ModuleController : ApiController
    {
        readonly ModuleInformation _info;

        public ModuleController(ModuleInformation info)
        {
            _info = info;
        }

        public IHttpActionResult Get()
        {
            return Ok(_info.Modules.Select(Map));
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