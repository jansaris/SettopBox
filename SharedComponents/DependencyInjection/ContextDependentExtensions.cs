using System;
using System.Linq.Expressions;
using System.Reflection;
using log4net;
using SimpleInjector;

namespace SharedComponents.DependencyInjection
{
    /// <summary>
    /// https://simpleinjector.codeplex.com/wikipage?title=ContextDependentExtensions
    /// </summary>
    public static class ContextDependentExtensions
    {
        public static void RegisterLog4Net(this Container container)
        {
            container.RegisterWithContext(dc =>
            {
                var type = dc.ImplementationType ?? MethodBase.GetCurrentMethod().DeclaringType;
                return LogManager.GetLogger(type);
            });
        }

        static void RegisterWithContext<TService>(this Container container, Func<DependencyContext, TService> contextBasedFactory)
            where TService : class
        {
            if (contextBasedFactory == null)
            {
                throw new ArgumentNullException(nameof(contextBasedFactory));
            }

            Func<TService> rootFactory = () => contextBasedFactory(DependencyContext.Root);

            container.Register(rootFactory, Lifestyle.Singleton);

            // Allow the Func<DependencyContext, TService> to be 
            // injected into parent types.
            container.ExpressionBuilding += (sender, e) =>
            {
                if (e.RegisteredServiceType == typeof(TService)) return;

                var rewriter = new DependencyContextRewriter
                {
                    ServiceType = e.RegisteredServiceType,
                    ContextBasedFactory = contextBasedFactory,
                    RootFactory = rootFactory,
                    Expression = e.Expression
                };

                e.Expression = rewriter.Visit(e.Expression);
            };
        }

        private sealed class DependencyContextRewriter : ExpressionVisitor
        {
            internal Type ServiceType { get; set; }

            internal object ContextBasedFactory { get; set; }

            internal object RootFactory { get; set; }

            internal Expression Expression { get; set; }

            internal Type ImplementationType
            {
                get
                {
                    var expression = Expression as NewExpression;

                    return expression != null ?
                        expression.Constructor.DeclaringType :
                        ServiceType;
                }
            }

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                if (!IsRootedContextBasedFactory(node))
                {
                    return base.VisitInvocation(node);
                }

                return Expression.Invoke(
                    Expression.Constant(ContextBasedFactory),
                    Expression.Constant(new DependencyContext(ServiceType, ImplementationType)));
            }

            private bool IsRootedContextBasedFactory(InvocationExpression node)
            {
                var expression = node.Expression as ConstantExpression;
                return expression != null && ReferenceEquals(expression.Value, RootFactory);
            }
        }
    }
}