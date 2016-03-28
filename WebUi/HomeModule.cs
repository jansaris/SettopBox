namespace WebUi
{
    public class HomeModule : Nancy.NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ => "Hello world";
        }
    }
}