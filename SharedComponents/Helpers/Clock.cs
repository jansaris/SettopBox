using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace SharedComponents.Helpers
{
    public class Clock
    {
        readonly ILog _logger;

        public Clock(ILog logger)
        {
            _logger = logger;
        }


        public async Task WaitForTimestamp(DateTime time, CancellationTokenSource cancelSource, string caller = null)
        {
            _logger.Debug($"WaitForTimestamp: {caller} - {time:yyyy-mm-dd hh:MM:ss}");
            if (cancelSource.IsCancellationRequested) return;
            if (time < DateTime.Now) return;
            _logger.Info($"WaitForTimestamp: {caller} - {time:yyyy-mm-dd hh:MM:ss}");
            var counter = 0;
            while (time > DateTime.Now && !cancelSource.IsCancellationRequested)
            {
                if (counter > 60)
                {
                    counter = 0;
                    _logger.Debug($"WaitForTimestamp: Still waiting for  {caller} - {time:yyyy-mm-dd hh:MM:ss}");
                }
                //Wait for 1 second, and re-evaluate the Cancellation Token
                await Task.Delay(1000);
                counter++;
            }
        }
    }
}