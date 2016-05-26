using log4net;
using SimpleInjector;

namespace SharedComponents.DependencyInjection
{
    public static class ContainerExtensions
    {
        public static void RegisterLog4Net(this Container container)
        {
            container.RegisterConditional(
                typeof (ILog),
                c => typeof (TypedLogger<>).MakeGenericType(c.Consumer.ImplementationType),
                Lifestyle.Singleton,
                c => true);
        }
    }
}