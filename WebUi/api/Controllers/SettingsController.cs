using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using SharedComponents.Settings;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
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

        [Route("api/settings/{module}")]
        public IHttpActionResult Put(string module, Setting[] settings)
        {
            var ini = _allSettings.FirstOrDefault(n => n.GetType().Namespace == module);
            if (ini == null) return BadRequest($"Unknown module {module}");
            var changedCount = 0;
            foreach (var setting in settings)
            {
                var original = ini.GetValue(setting.Name);
                if (original?.ToString() == setting.Value?.ToString()) continue;

                ini.SetValue(setting.Name, setting.Value?.ToString());
                changedCount++;
            }
            if (changedCount > 0)
            {
                ini.Save();
            }

            return Ok(changedCount);
        }
    }
}