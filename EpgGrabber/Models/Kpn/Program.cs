using log4net;

namespace EpgGrabber.Models.Kpn
{
    public class Program
    {
        private readonly string[] Columns;

        public string Id => Columns[0];
        public string ProgramName => Columns[2];
        public string Episode => Columns[13];
        public string Genre1 => Columns[15];
        public string Genre2 => Columns[16];
        public string Description => Columns[24];
        public string EpisoneNumber => Columns[56];

        public Program(string line, ILog logger)
        {
            Columns = line.Split('|');
            if (Columns.Length < 56) logger.Warn($"Missing data for Program, expected at least 56 columns ({Columns.Length}): {line}");
        }
    }
}