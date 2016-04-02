using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebUi.api
{
    public class HomeController : ApiController
    {
        public IHttpActionResult Get()
        {
            return Ok("Welcome to SettopBox WebUi");
        }
    }
}
