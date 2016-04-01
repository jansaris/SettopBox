using Nancy;
using SharedComponents;

namespace WebUi.Nancy.Modules
{
    public class HomeModule : NancyModule
    {
        readonly ModuleInformation _moduleInformation;

        public HomeModule(ModuleInformation moduleInformation)
        {
            _moduleInformation = moduleInformation;
            //Add some default routes
            Get["/"] = _ => Response.AsFile("Content/index.html");
            Get["/welcome"] = _ => "Welcome to SettopBox WebUi";
            Get["/modules"] = GetModules;
        }

        object GetModules(object arg)
        {
            return _moduleInformation.Modules;
        }
    }
}