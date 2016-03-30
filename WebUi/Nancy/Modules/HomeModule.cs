using Nancy;

namespace WebUi.Nancy.Modules
{
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            //Add some default routes
            Get["/"] = _ => Response.AsFile("Content/index.html");
            Get["/welcome"] = _ => "Welcome to SettopBox WebUi";
        }
    }
}