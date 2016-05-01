using log4net;

namespace EpgGrabber
{
    public class Grabber
    {
        readonly ILog _logger;

        public Grabber(ILog logger)
        {
            _logger = logger;
        }

        public void Download()
        {
            _logger.Info("Start grabbing EPG");
        }
    }
}