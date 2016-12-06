using System;
using System.Diagnostics;
using WebUi.api.Models;

namespace WebUi.api
{
    public class PerformanceMeter
    {
        private readonly PerformanceCounter _processCpu;
        private readonly PerformanceCounter _totalCpu;
        private readonly int _cores;

        public PerformanceMeter()
        {
            _totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _processCpu = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            _cores = Environment.ProcessorCount;
        }

        public PerformanceModel Next()
        {
            return new PerformanceModel
            {
                Total = _totalCpu.NextValue(),
                Process = _processCpu.NextValue(),
                Cores = _cores
            };
        }
    }
}
