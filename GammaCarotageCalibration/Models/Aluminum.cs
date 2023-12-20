using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public class Aluminum : IMaterial
    {
        public double Sigma { get; set; }
        public ProbeMetrics ProbeMetrics { get; set; }
        public Materials MaterialType { get; set; }

        public Aluminum(double sigma, double?[] smallProbe, double?[] largeProbe, double totalSeconds)
        {
            Sigma = sigma;
            ProbeMetrics = new ProbeMetrics(smallProbe, largeProbe, totalSeconds);
            MaterialType = Materials.Aluminum;
        }

        public Aluminum(double sigma, double averageSmallProbe, double averageLargeProbe)
        {
            Sigma = sigma;
            ProbeMetrics = new ProbeMetrics(averageSmallProbe, averageLargeProbe);
            MaterialType = Materials.Aluminum;
        }
    }
}
