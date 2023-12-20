using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public class Magnesium : IMaterial
    {
        public double Sigma { get; set; }
        public ProbeMetrics ProbeMetrics { get; set; }
        public Materials MaterialType { get; set; }

        public Magnesium(double sigma, double?[] smallProbe, double?[] largeProbe, TimeSpan selectedAccumulationTime)
        {
            Sigma = sigma;
            ProbeMetrics = new ProbeMetrics(smallProbe, largeProbe, selectedAccumulationTime);
            MaterialType = Materials.Magnesium;
        }

        public Magnesium(double sigma, double averageSmallProbe, double averageLargeProbe)
        {
            Sigma = sigma;
            ProbeMetrics = new ProbeMetrics(averageSmallProbe, averageLargeProbe);
            MaterialType = Materials.Aluminum;
        }
    }
}
