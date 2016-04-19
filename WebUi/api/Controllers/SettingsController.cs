using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using SharedComponents.Settings;

namespace WebUi.api.Controllers
{
    public class SettingsController :ApiController
    {
        readonly IEnumerable<IniSettings> _allSettings;

        public SettingsController(IEnumerable<IniSettings> allSettings)
        {
            _allSettings = allSettings;
        }

        public IHttpActionResult Get()
        {
            var names = _allSettings.Select(s => s.GetType().Namespace);
            return Ok(names);
        }

        public IHttpActionResult Get(string module)
        {
            var ini = _allSettings.FirstOrDefault(n => n.GetType().Namespace == module);
            if(ini == null) return BadRequest($"Unknown module {module}");
            return Ok(ini.GetAll());
        }
    }
}