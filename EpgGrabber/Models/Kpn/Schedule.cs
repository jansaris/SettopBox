using log4net;

namespace EpgGrabber.Models.Kpn
{
    class Schedule
    {
        private readonly string[] Columns;

        public string Id => Columns[0];
        public string ProgramId => Columns[1];
        public string StartTime => Columns[2];
        public string Duration => Columns[3];
        public string ChannelId => Columns[13];

        public Schedule(string line, ILog logger)
        {
            Columns = line.Split('|');
            if(Columns.Length < 13) logger.Warn($"Missing data for schedule, expected at least 13 columns ({Columns.Length}): {line}");
        }
    }
}
