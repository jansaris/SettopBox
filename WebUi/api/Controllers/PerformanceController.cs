using System.Web.Http;
using System.Web.Http.Cors;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class PerformanceController : ApiController
    {
        private readonly PerformanceMeter _meter;

        public PerformanceController(PerformanceMeter meter)
        {
            _meter = meter;
        }

        public IHttpActionResult Get()
        {
            return Ok(_meter.Next());
        }
    }
}