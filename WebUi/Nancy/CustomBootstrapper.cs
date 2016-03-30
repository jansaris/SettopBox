using Nancy;
using Nancy.Conventions;

namespace WebUi.Nancy
{
    public class CustomBoostrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("", @"Content")
            );
            base.ConfigureConventions(nancyConventions);
        }
    }
}