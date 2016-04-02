using System.Web.Http;

namespace WebUi.api.Controllers
{
    public class HomeController : ApiController
    {
        public IHttpActionResult Get()
        {
            return Ok("Welcome to SettopBox WebUi");
        }
    }
}
