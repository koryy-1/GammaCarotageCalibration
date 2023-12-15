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
        public double ProbeData { get; set; }
        public double CalculatedDensity { get; set; }
        public double MeasurementError { get; set; }

        public Report(
            double density, double dataProbe, double calculatedDensity, double measurementError
            )
        {
            Density = density;
            ProbeData = dataProbe;
            CalculatedDensity = calculatedDensity;
            MeasurementError = measurementError;
        }
    }
}
