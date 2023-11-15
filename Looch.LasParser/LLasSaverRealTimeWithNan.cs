using System.Globalization;
using System.Text;

namespace Looch.LasParser
{

    public class LLasSaverRealTimeWithNan : IDisposable
    {
        private readonly StreamWriter outStream;
        private bool IsDepth { get; set; }
        public LLasSaverRealTimeWithNan(Stream stream, string codepage, bool isDepth = false)
        {
            Null = -999.25;
            FileInfo = null;
            OtherMessage = null;
            outStream = new StreamWriter(stream, Encoding.GetEncoding(codepage));
            IsDepth = isDepth;
        }

        public string? FileInfo { get; set; }
        public string? OtherMessage { get; set; }

        private double Null { get; set; }
        private string[] Mnem { get; set; }
        private string FormatTime { get; set; }
        private string FormatDepth { get; set; }
        private string[] Format { get; set; }
        private long PositionStart { get; set; }
        private long PositionStop { get; set; }
        private long PositionDate { get; set; }

        private long WriteRecord(string mnem, string unit, string value, string description)
        {
            outStream.Write(" " + string.Format("{0,8}", mnem) + ".");
            outStream.Write(string.Format("{0,-8}", unit) + string.Format("{0,25}", value));
            outStream.Flush();
            long pos_value = outStream.BaseStream.Position;
            outStream.WriteLine(":" + description);
            outStream.Flush();
            return pos_value;
        }

        /// <summary>
        /// Записывает заголовок первый тик, шаг в милисекундах, параметры аналогичные LAS.
        /// </summary>
        /// <param name="mnem_well">Мнемоники скважин.</param>
        /// <param name="value_well">Значения скважин.</param>
        /// <param name="desc_well">Описания скважин.</param>
        /// <param name="mnem_curve">Мнемоники кривых.</param>
        /// <param name="unit_curve">Единицы кривых.</param>
        /// <param name="desc_curve">Описания кривых.</param>
        /// <param name="format_curve">Форматы кривых.</param>
        /// <param name="mnem_param">Мнемоники параметров каротажа.</param>
        /// <param name="unit_param">Единицы параметров каротажа.</param>
        /// <param name="value_param">Значения параметров каротажа.</param>
        /// <param name="desc_param">Описания параметров каротажа.</param>
        /// <param name="other">Другая информация, которую нужно записать в конце заголовка.</param>
        public void WriteHeader(
            string[] mnem_well, string[] value_well, string[] desc_well,
            string[] mnem_curve, string[] unit_curve, string[] desc_curve, string[] format_curve,
            string[] mnem_param, string[] unit_param, string[] value_param, string[] desc_param,
            string other)
        {
            Mnem = mnem_curve;
            Format = format_curve;
            FormatTime = "{0,11:0}";

            FormatDepth = "{0,11:0.00}";
            //! Секция Version
            if (FileInfo != null)
                outStream.WriteLine("# " + FileInfo);
            outStream.WriteLine("~VERSION INFORMATION");
            WriteRecord("VERS", "", "2.0", "Версия формата LAS");
            WriteRecord("WRAP", "", "NO", "Режим одной строчки на временной отчет");
            //! Секция Well
            outStream.WriteLine("~WELL INFORMATION");

            if (!IsDepth)
            {
                PositionStart = WriteRecord("STRT", "ms", "", "Время первой записи в файле");
                PositionStop = WriteRecord("STOP", "ms", "", "Время последней записи в файле");
                WriteRecord("STEP", "ms", "0", "Шаг времени, 0 - переменный шаг");
            }
            else
            {
                PositionStart = WriteRecord("STRT", "m", "", "Начальная глубина");
                PositionStop = WriteRecord("STOP", "m", "", "Конечная глубина");
                WriteRecord("STEP", "ms", "0", "Шаг времени, 0 - переменный шаг");

            }
            WriteRecord("NULL", "", Null.ToString(NumberFormatInfo.InvariantInfo), "Метка, нет значения");
            PositionDate = WriteRecord("DATE", "", "", "Дата каротажа");

            if (mnem_well != null)
            {
                for (int i = 0; i < mnem_well.Length; i++)
                {
                    string str_mnem = mnem_well[i];
                    string str_util = "";
                    string str_value = value_well != null ? value_well[i] : "";
                    string str_desc = desc_well != null ? desc_well[i] : "";
                    WriteRecord(str_mnem, str_util, str_value, str_desc);
                }
            }
            //!Секция кривых
            outStream.WriteLine("~CURVE INFORMATION");
            if (!IsDepth)
            {
                WriteRecord("TIME", "ms", FormatTime, "Время, количество миллисекунд от начало суток");
            }
            else
            {
                WriteRecord("DEPT", "m", FormatDepth, "Глубина");
            }
            for (int i = 0; i < mnem_curve.Length; i++)
            {
                string str_mnem = mnem_curve[i];
                string str_util = unit_curve != null ? unit_curve[i] : "";
                //string str_value = Format[i];
                string str_value = "";
                string str_desc = desc_curve != null ? desc_curve[i] : "";
                WriteRecord(str_mnem, str_util, str_value, str_desc);
            }
            //!Секция  параметров
            if (mnem_param != null)
            {
                outStream.WriteLine("~PARAMETER INFORMATION BLOCK");
                for (int i = 0; i < mnem_param.Length; i++)
                {
                    string str_mnem = mnem_param[i];
                    string str_util = unit_param != null ? unit_param[i] : "";
                    string str_value = value_param != null ? value_param[i] : "";
                    string str_desc = desc_param != null ? desc_param[i] : "";
                    WriteRecord(str_mnem, str_util, str_value, str_desc);
                }
            }
            outStream.WriteLine("~OTHER INFORMATION");
            outStream.WriteLine(other);
            outStream.Flush();
            //Секция начала данных
            outStream.WriteLine();
            outStream.Write("~A     TIME");
            for (int i = 0; i < mnem_curve.Length; i++)
            {
                int l = string.Format(Format[i], 0.0).Length;
                var str = string.Format(Format[i], mnem_curve[i]);
                if (str.Length > l)
                {
                    str = str.Substring(str.Length - l);
                }
                outStream.Write(" " + str);
            }
            outStream.WriteLine();
            outStream.Flush();
            FNeedSetDateAndStart = true;
            outStream.Flush();
        }
        /// <summary>
        /// Нужно ли установить дату.
        /// </summary>
        private bool FNeedSetDateAndStart { get; set; }
        private DateTime InitDate { get; set; }
        private DateTime LastDate { get; set; }

        private double InitDepth { get; set; }
        private double LastDepth { get; set; }


        //Записать данные оканчивающиеся на 
        private void WriteDataByPosition(long idx_position, string data)
        {
            long cur_pos = outStream.BaseStream.Position;
            outStream.BaseStream.Seek(idx_position - data.Length, SeekOrigin.Begin);
            outStream.Write(data);
            outStream.Flush();
            outStream.BaseStream.Seek(cur_pos, SeekOrigin.Begin);
            outStream.Flush();
        }

        private int DateToInt(DateTime time)
        {
            return (int)((time - InitDate).TotalMilliseconds + 0.5);
        }

        /// <summary>
        /// Добавляет данные в конец файла.
        /// </summary>
        /// <param name="time">Значения времени.</param>
        /// <param name="data">Данные.</param>
        public void AddData(List<DateTime> time, Dictionary<string, List<double>> data)
        {
            if (time.Count == 0)
                return;
            if (FNeedSetDateAndStart)
            {
                DateTime start = time.First();
                InitDate = start.Date;
                LastDate = time.Last();

                FNeedSetDateAndStart = false;
                WriteDataByPosition(PositionDate, InitDate.ToString("dd/MM/yyyy"));
                WriteDataByPosition(PositionStart, string.Format(NumberFormatInfo.InvariantInfo, FormatTime, DateToInt(start)));
            }
            for (int i = 0; i < time.Count; i++)
            {
                outStream.Write(string.Format(NumberFormatInfo.InvariantInfo, FormatTime, DateToInt(time[i])));
                for (int j = 0; j < Mnem.Length; j++)
                {
                    outStream.Write(" ");
                    double value = Null;
                    if (data.ContainsKey(Mnem[j]))
                    {
                        double val = data[Mnem[j]][i];
                        if (!double.IsNaN(val) && !double.IsInfinity(val))
                            value = val;
                    }
                    var str = string.Format(NumberFormatInfo.InvariantInfo, Format[j], value);
                    outStream.Write(str);
                }
                outStream.WriteLine();
            }
            LastDate = time.Last();
            outStream.Flush();
            //Cохранение данных
        }

        /// <summary>
        /// Добавляет данные в конец файла.
        /// </summary>
        /// <param name="depth">Значения глубины.</param>
        /// <param name="data">Данные.</param>
        public void AddData(List<double> depth, Dictionary<string, List<double>> data)
        {
            if (depth.Count == 0)
                return;
            if (FNeedSetDateAndStart)
            {
                DateTime start = DateTime.Now;
                InitDate = start;
                InitDepth = depth.First();

                LastDepth = depth.Last();
                FNeedSetDateAndStart = false;
                WriteDataByPosition(PositionDate, InitDate.ToString("dd/MM/yyyy"));
                if (!IsDepth)
                {
                    WriteDataByPosition(PositionStart, string.Format(NumberFormatInfo.InvariantInfo, FormatTime, DateToInt(start)));
                }
                else
                {
                    WriteDataByPosition(PositionStart, string.Format(NumberFormatInfo.InvariantInfo, FormatDepth, InitDepth));
                }
            }
            for (int i = 0; i < depth.Count; i++)
            {
                outStream.Write(string.Format(NumberFormatInfo.InvariantInfo, FormatDepth, depth[i]));
                for (int j = 0; j < Mnem.Length; j++)
                {
                    outStream.Write(" ");
                    double value = Null;
                    if (data.ContainsKey(Mnem[j]))
                    {
                        double val = data[Mnem[j]][i];
                        if (!double.IsNaN(val) && !double.IsInfinity(val))
                            value = val;
                    }
                    outStream.Write(string.Format(NumberFormatInfo.InvariantInfo, Format[j], value));
                }
                outStream.WriteLine();
            }
            LastDepth = depth.Last();
            outStream.Flush();
            //Cохранение данных
        }
        /// <summary>
        /// Завершает запись данных с закрытием потока.
        /// </summary>
        public void Close()
        {
            if (!FNeedSetDateAndStart)
            {
                if (!IsDepth)
                {
                    WriteDataByPosition(PositionStop, string.Format(NumberFormatInfo.InvariantInfo, FormatTime, DateToInt(LastDate)));
                }
                else
                {
                    WriteDataByPosition(PositionStop, string.Format(NumberFormatInfo.InvariantInfo, FormatDepth, LastDepth));
                }
            }
            outStream.Close();
        }

        private bool disposed = false;
        void IDisposable.Dispose()
        {
            if (!disposed)
            {
                Close();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
