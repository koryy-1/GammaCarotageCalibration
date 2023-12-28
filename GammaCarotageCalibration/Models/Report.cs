using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Models
{
    public class Report
    {
        public double Density { get; set; }
        public string ProbeData { get; set; }
        public string CalculatedDensity { get; set; }
        public string MeasurementError { get; set; }

        public Report(
            double density, string dataProbe, string calculatedDensity, string measurementError
            )
        {
            Density = density;
            ProbeData = dataProbe;
            CalculatedDensity = calculatedDensity;
            MeasurementError = measurementError;
        }
    }
}
