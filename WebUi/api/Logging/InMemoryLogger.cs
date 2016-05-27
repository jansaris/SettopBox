using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Appender;
using log4net.Core;
using WebUi.api.Models;

namespace WebUi.api.Logging
{
    public class InMemoryLogger : AppenderSkeleton
    {
        readonly LimitedList<LogModel> _logList = new LimitedList<LogModel>(500);

        public InMemoryLogger()
        {
            var root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
            root.AddAppender(this);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var logName = loggingEvent.LoggerName.Split('.');
            var module = logName.FirstOrDefault();
            var component = logName.Length > 1 ? string.Join(".", logName.Skip(1)) : module;
            var model = new LogModel
            {
                Module = module,
                Component = component,
                Message = loggingEvent.RenderedMessage,
                Timestamp = loggingEvent.TimeStamp,
                Level = loggingEvent.Level.DisplayName
            };
            _logList.Add(model);
        }

        public IEnumerable<LogModel> GetByModule(string name)
        {
            return _logList.Where(m => m.Module == name).ToList().AsReadOnly();
        }

        public IEnumerable<LogModel> GetAll()
        {
            return _logList.AsReadOnly();
        }

        public IEnumerable<string> GetLevels()
        {
            yield return Level.Debug.DisplayName;
            yield return Level.Info.DisplayName;
            yield return Level.Warn.DisplayName;
            yield return Level.Error.DisplayName;
            yield return Level.Fatal.DisplayName;
        }

        /// <summary>
        /// Returns the list of levels to filter on
        /// The higher the given level, the less in the filter
        /// ERROR returns: Error and Fatal
        /// If no match, all the levels are given
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public IList<string> GetLevelsFilter(string level)
        {
            var levels = GetLevels().ToList();
            while (levels.Count > 0)
            {
                if (levels[0].EqualsIgnoreCase(level)) break;
                levels.RemoveAt(0);
            }
            return (levels.Any() ? levels : GetLevels()).ToList();
        }
    }
}