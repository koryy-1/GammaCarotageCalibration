using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Services
{
    public static class DataProcessor
    {
        public static List<double?> SmoothDataWithCount(List<double?> data, int windowSize, int count)
        {
            var smoothedData = data;
            for (int i = 0; i < count; i++)
            {
                smoothedData = SmoothData(data, windowSize);
            }
            return smoothedData;
        }

        public static List<double?> SmoothData(List<double?> data, int windowSize)
        {
            if (data == null || data.Count == 0)
            {
                return new List<double?>();
            }

            List<double?> smoothedData = new List<double?>(data.Count - windowSize + 1);

            for (int i = 0; i <= data.Count - windowSize; i++)
            {
                double? sum = 0;
                int validCount = 0; // Счетчик "валидных" значений в окне.

                for (int j = 0; j < windowSize; j++)
                {
                    double? value = data[i + j];

                    if (value.HasValue)
                    {
                        sum += value;
                        validCount++;
                    }
                }

                if (validCount > 0)
                {
                    smoothedData.Add(sum / validCount); // Используем среднее только для валидных значений в окне.
                }
                else
                {
                    smoothedData.Add(null);
                }
            }

            return smoothedData;
        }

        public static List<double?> DivideArrays(List<double?> array1, List<double?> array2)
        {
            if (array1.Count != array2.Count)
            {
                throw new ArgumentException("Массивы должны иметь одинаковую длину.");
            }

            List<double?> result = new List<double?>();

            for (int i = 0; i < array1.Count; i++)
            {
                double? value1 = array1[i];
                double? value2 = array2[i];

                if (value1.HasValue && value2.HasValue && value2.Value != 0)
                {
                    result.Add(value1 / value2);
                }
                else
                {
                    result.Add(null); // Если одно из значений null или делитель равен 0, результат также будет null.
                }
            }

            return result;
        }
    }
}
