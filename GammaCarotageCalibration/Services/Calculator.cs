using GammaCarotageCalibration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Services
{
    public class Calculator
    {
        public double GetCoefC(
            double alfa1, double alfa2, double alfa3,
            double sigma1, double sigma2, double sigma3
        )
        {
            // https://studfile.net/preview/8950493/page:17/
            double numerator = sigma3 - sigma2 - (sigma2 - sigma1) * (alfa3 - alfa2) / (alfa2 - alfa1);
            double denominator = (alfa3 - alfa1) * (alfa3 - alfa2);
            return numerator / denominator;
        }

        public double GetCoefB(
            double alfa1, double alfa2, double alfa3,
            double sigma1, double sigma2, double sigma3
        )
        {
            // https://studfile.net/preview/8950493/page:17/
            double C = GetCoefC(
                alfa1, alfa2, alfa3,
                sigma1, sigma2, sigma3
            );

            return (sigma2 - sigma1) / (alfa2 - alfa1) - (alfa2 + alfa1) * C;
        }

        public double GetCoefA(
            double alfa1, double alfa2, double alfa3,
            double sigma1, double sigma2, double sigma3
        )
        {
            // https://studfile.net/preview/8950493/page:17/
            double B = GetCoefB(
                alfa1,alfa2, alfa3,
                sigma1, sigma2, sigma3
            );
            double C = GetCoefC(
                alfa1, alfa2, alfa3,
                sigma1, sigma2, sigma3
            );

            return sigma3 - alfa3 * B - Math.Pow(alfa3, 2) * C;
        }

        public double CalculateDensity(double A, double B, double C, double alfa)
        {
            return A + B * alfa + C * Math.Pow(alfa, 2);
        }
    }
}
