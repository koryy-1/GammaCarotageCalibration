using LiveChartsCore.Geo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public class ProbeMetrics
    {
        public double AverageSmallProbe { get; set; }
        public double AverageLargeProbe { get; set; }
        public double Alfa { get; set; }
        public int CountOfSamples { get; set; }

        public ProbeMetrics(double?[] smallProbe, double?[] largeProbe, TimeSpan selectedAccumulationTime)
        {
            CountOfSamples = Convert.ToInt32(selectedAccumulationTime.TotalSeconds / 4);
            AverageSmallProbe = smallProbe.Take(CountOfSamples).Average().Value;
            AverageLargeProbe = largeProbe.Take(CountOfSamples).Average().Value;
            Alfa = Math.Round(AverageLargeProbe / AverageSmallProbe, 6);
        }

        public ProbeMetrics(double averageSmallProbe, double averageLargeProbe)
        {
            AverageSmallProbe = averageSmallProbe;
            AverageLargeProbe = averageLargeProbe;
            Alfa = Math.Round(AverageLargeProbe / AverageSmallProbe, 6);
        }

        //public double GetAverageSmallProbe(double totalSeconds)
        //{
        //    CountOfSamples = Convert.ToInt32(totalSeconds / 4);
        //    return AverageSmallProbe.Take(CountOfSamples).Average().Value;
        //}

        //public double GetAverageLargeProbe(double totalSeconds)
        //{
        //    CountOfSamples = Convert.ToInt32(totalSeconds / 4);
        //    return AverageLargeProbe.Take(CountOfSamples).Average().Value;
        //}
    }
}
