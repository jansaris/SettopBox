using System.Web.Http;
using System.Web.Http.Cors;

namespace WebUi.api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class HomeController : ApiController
    {
        public IHttpActionResult Get()
        {
            return Ok("Welcome to SettopBox WebUi");
        }
    }
}
