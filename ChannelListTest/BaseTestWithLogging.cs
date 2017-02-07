using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using NUnit.Framework;

namespace ChannelListTest
{
    public abstract class BaseTestWithLogging
    {
        protected ILog Logger = LogManager.GetLogger("TestLogger");

        [SetUp]
        public void SetUpLogging()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date %level - %message%newline"
            };
            patternLayout.ActivateOptions();

            var appender = new ConsoleAppender {Layout = patternLayout};
            appender.ActivateOptions();
            hierarchy.Root.AddAppender(appender);

            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
        }
    }
}
