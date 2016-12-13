using System;
using System.Diagnostics;
using log4net;
using WebUi.api.Models;

namespace WebUi.api
{
    public class PerformanceMeter
    {
        private readonly ILog _logger;
        private PerformanceCounter _processCpu;
        private PerformanceCounter _monoCpu;
        private PerformanceCounter _totalCpu;
        private readonly int _cores;

        public PerformanceMeter(ILog logger)
        {
            _logger = logger;
            var name = Process.GetCurrentProcess().ProcessName;
            _totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _processCpu = new PerformanceCounter("Process", "% Processor Time", name);
            _monoCpu = new PerformanceCounter("Process", "% Processor Time", "mono");
            _cores = Environment.ProcessorCount;
            logger.Info($"Generated performance counters for _Total, mono and {name}");
        }

        public PerformanceModel Next()
        {
            return new PerformanceModel
            {
                Total = GetCounterValue(ref _totalCpu),
                Process = GetCounterValue(ref _processCpu),
                Mono = GetCounterValue(ref _monoCpu),
                Cores = _cores
            };
        }

        private float GetCounterValue(ref PerformanceCounter counter)
        {
            if (counter == null) return 0;
            try
            {
                return counter.NextValue();
            }
            catch (Exception)
            {
                _logger.Info($"Failed to retrieve counter '{counter.InstanceName}', Disable counter");
                counter = null;
            }
            return 0;
        }
    }
}
