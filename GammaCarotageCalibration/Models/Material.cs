using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public class Material
    {
        public double Sigma { get; set; }
        public Materials MaterialType { get; set; }
        ProbeMetrics ProbeMetrics { get; set; }
    }
}
