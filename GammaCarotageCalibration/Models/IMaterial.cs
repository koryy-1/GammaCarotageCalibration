using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public interface IMaterial
    {
        public double Sigma { get; set; }
        ProbeMetrics ProbeMetrics { get; set; }
    }
}
