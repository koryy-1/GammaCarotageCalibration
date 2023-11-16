using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace Looch.LasParser
{
    public class LasParser
    {
        public LasParser()
        {
            Clear();
        }

        public delegate int FCallBack(double n);
        public event FCallBack? CallBack;

        private static int CountLengthForPrintDouble(int countInt, int countFract)
        {
            if (countFract == 0)
                return countInt;
            return countInt + countFract + 1;
        }

        //Расчет оптимального вывода чисел
        private void CalcOptimalFormat(double?[] data, int maxCountFract, ref int countInt, ref int countFract)
        {
            countInt = 1;
            countFract = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double value = data[i] ?? Null;
                //Определяем длину дробной части
                if (countFract != maxCountFract)
                {
                    double tmp = Math.Pow(10.0, maxCountFract);
                    long cell = (long)(Math.Abs(value - (long)value) * tmp + 0.5);
                    int count = maxCountFract;
                    while (cell % 10 == 0 && count > countFract)
                    {
                        count--;
                        cell /= 10;
                    }
                    if (count > countFract)
                    {
                        countFract = count;
                    }
                }
                //Определяем длину целой части
                long tmp_cell = (long)(value + 0.5);
                tmp_cell /= 10;// ноль тоже печатать надо!!!
                int tmp_count = 1;
                while (tmp_cell != 0)
                {
                    tmp_cell /= 10;
                    tmp_count++;
                }
                if (tmp_count >= countInt)
                {
                    countInt = tmp_count;
                    if (value < 0)
                    {
                        countInt++;
                    }
                }
            }
        }
        public string FileInfo { get; set; }
        public bool IsWrap { get; set; }
        public List<string> Wmnem { get; set; }
        public List<string> Wunit { get; set; }
        public List<string> Wvalue { get; set; }
        public List<string> Wdesc { get; set; }

        public bool IsWMnem(string mnem)
        {
            foreach (string cur_mnem in Wmnem)
            {
                if (mnem.Equals(cur_mnem, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void WAdd(string mnem, string unit, string value, string desc)
        {
            Wmnem.Add(mnem);
            Wunit.Add(unit);
            Wvalue.Add(value);
            Wdesc.Add(desc);
        }

        public void WSet(string mnem, string unit, string value, string desc)
        {
            for (int i = 0; i < Wmnem.Count; i++)
            {
                if (mnem.Equals(Wmnem[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    Wmnem[i] = mnem;
                    Wunit[i] = unit;
                    Wvalue[i] = value;
                    Wdesc[i] = desc;
                    return;
                }

            }
            WAdd(mnem, unit, value, desc);
        }

        public string WGetParam(string mnem)
        {
            for (int i = 0; i < Wmnem.Count; i++)
            {
                if (mnem.Equals(Wmnem[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    return Wvalue[i];
                }
            }
            throw new Exception("Нет такой мнемоники");
        }

        public List<string> Cmnem { get; set; }
        public List<string> Cunit { get; set; }
        public List<string> Cvalue { get; set; }
        public List<string> Cdesc { get; set; }

        public void CAdd(string mnem, string unit, string value, string desc, double[] data)
        {
            var cdata = new double?[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                var cur_val = data[i];
                if (double.IsNaN(cur_val))
                {
                    cdata[i] = null;
                }
                else
                {
                    cdata[i] = cur_val;
                }
            }
            CAdd(mnem, unit, value, desc, cdata);
        }
        public void CAdd(string mnem, string unit, string value, string desc, double?[] data)
        {
            CAddHeader(mnem, unit, value, desc);
            Data[mnem] = data;
        }
        private void CAddHeader(string mnem, string unit, string value, string desc)
        {
            Cmnem.Add(mnem);
            Cunit.Add(unit);
            Cvalue.Add(value);
            Cdesc.Add(desc);
        }
        public List<string> Pmnem { get; set; }
        public List<string> Punit { get; set; }
        public List<string> Pvalue { get; set; }
        public List<string> Pdesc { get; set; }
        public void PAdd(string mnem, string unit, string value, string desc)
        {
            Pmnem.Add(mnem);
            Punit.Add(unit);
            Pvalue.Add(value);
            Pdesc.Add(desc);
        }

        public void PSet(string mnem, string unit, string value, string desc)
        {
            for (int i = 0; i < Pmnem.Count; i++)
            {
                if (mnem.Equals(Pmnem[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    Pmnem[i] = mnem;
                    Punit[i] = unit;
                    Pvalue[i] = value;
                    Pdesc[i] = desc;
                    return;
                }

            }
            PAdd(mnem, unit, value, desc);
        }

        public string Other { get; set; }

        public Dictionary<string, double?[]> Data { get; set; }

        public void CutNullStartAndFinish()
        {
            double?[] Grid = Data[Cmnem[0]];
            int idx_start = 0, idx_finish = Grid.Length - 1;
            List<double?[]> data_for_analyze = new List<double?[]>();
            for (int i = 1; i < Cmnem.Count; i++)
            {
                data_for_analyze.Add(Data[Cmnem[i]]);
            }
            //Пододвигаем начало
            for (idx_start = 0; idx_start < Grid.Length; idx_start++)
            {
                bool f = false;
                foreach (var cur in data_for_analyze)
                {
                    if (cur[idx_start] != null)
                    {
                        f = true;
                        break;
                    }
                }
                if (f)
                {
                    break;
                }
            }
            //Пододвигаем конец
            for (idx_finish = Grid.Length - 1; idx_finish > idx_start; idx_finish--)
            {
                bool f = false;
                foreach (var cur in data_for_analyze)
                {
                    if (cur[idx_finish] != null)
                    {
                        f = true;
                        break;
                    }
                }
                if (f)
                {
                    break;
                }
            }
            if (idx_start == idx_finish)
            {
                foreach (var cur in Cmnem)
                {
                    Data[cur] = Array.Empty<double?>();
                }
                return;
            }
            foreach (var cur in Cmnem)
            {
                Data[cur] = SliceArray(Data[cur], idx_start, idx_finish - idx_start + 1);
            }
        }
        private static T[] SliceArray<T>(T[] array, int startIndex, int length)
        {
            T[] result = new T[length];
            Array.Copy(array, startIndex, result, 0, length);
            return result;
        }

        public double Null { get; set; }

        public void WriteFile(string namefile, string codepage)
        {
            GC.Collect();
            //IsWrap = false;
            if (Cmnem.Count == 0)
                throw new Exception("Нет первой кривой");
            double?[] Grid = Data[Cmnem[0]];
            using (var stream = new FileStream(namefile, FileMode.Create, FileAccess.Write))
            using (var OutStream = new StreamWriter(stream, Encoding.GetEncoding(codepage)))
            {
                //Максимальное количество знаков после запятой
                const int MaxCountFract = 5;
                if (FileInfo.Length > 0)
                {
                    OutStream.WriteLine("# " + FileInfo);
                }
                //! Секция Version
                OutStream.WriteLine("~VERSION INFORMATION");
                OutStream.WriteLine("VERS     .                               2.0:CWLS LAS VERSION");
                if (IsWrap)
                {
                    OutStream.WriteLine("WRAP     .                               YES: Multiline per depth step");
                }
                else
                {
                    OutStream.WriteLine("WRAP     .                                NO: One line per depth step");
                }
                //! Секция Well
                OutStream.WriteLine("~WELL INFORMATION");
                OutStream.WriteLine("#    MNEM.UNIT                     DATA TYPE:INFORMATION");
                OutStream.WriteLine("#========.==================================:=========================================");

                if (Wmnem != null)
                {
                    for (int i = 0; i < Wmnem.Count; i++)
                    {
                        string str_util = Wunit != null ? Wunit[i] : "";
                        string str_value = Wvalue != null ? Wvalue[i] : "";
                        string str_desc = Wdesc != null ? Wdesc[i] : "";
                        OutStream.WriteLine("{0,-9}.{1,-10} {2,23}:{3}", Wmnem[i], str_util, str_value, str_desc);
                    }
                }
                OutStream.Flush();
                //!Секция кривых
                OutStream.WriteLine("~CURVE INFORMATION");
                OutStream.WriteLine("#    MNEM.UNIT                      API CODE:CURVE DESCRIPTION");
                OutStream.WriteLine("#========.==================================:=========================================");
                for (int i = 0; i < Cmnem.Count; i++)
                {
                    string str_util = Cunit != null ? Cunit[i] : "";
                    string str_value = Cvalue != null ? Cvalue[i] : "";
                    string str_desc = Cdesc != null ? Cdesc[i] : "";
                    OutStream.WriteLine("{0,-9}.{1,-8} {2,25}:{3}", Cmnem[i], str_util, str_value, str_desc);
                }
                OutStream.Flush();
                //!Секция  параметров
                if (Pmnem != null)
                {
                    OutStream.WriteLine("~PARAMETER INFORMATION BLOCK");
                    OutStream.WriteLine("#    MNEM.UNIT                      API CODE:CURVE DESCRIPTION");
                    OutStream.WriteLine("#========.==================================:=========================================");
                    for (int i = 0; i < Pmnem.Count; i++)
                    {
                        string str_util = Punit != null ? Punit[i] : "";
                        string str_value = Pvalue != null ? Pvalue[i] : "";
                        string str_desc = Pdesc != null ? Pdesc[i] : "";
                        OutStream.WriteLine("{0,-9}.{1,-8} {2,25}:{3}", Pmnem[i], str_util, str_value, str_desc);
                    }
                }
                OutStream.Flush();
                OutStream.WriteLine("~OTHER INFORMATION");
                OutStream.WriteLine(Other);
                OutStream.Flush();
                //Определяем размеры каждой из кривых
                Dictionary<string, int> countInt = new Dictionary<string, int>();
                Dictionary<string, int> countFract = new Dictionary<string, int>();
                int counter_progress = 0;
                foreach (KeyValuePair<string, double?[]> kvp in Data)
                {
                    int tmp_countInt = 0, tmp_countFract = 0;
                    this.CalcOptimalFormat(kvp.Value, MaxCountFract, ref tmp_countInt, ref tmp_countFract);
                    countInt[kvp.Key] = tmp_countInt;
                    countFract[kvp.Key] = tmp_countFract;
                    counter_progress++;
                    SendCallback((0.05 * counter_progress) / Data.Count);
                }
                //Временные массивы для ускорения процесса
                int[] total_length = new int[Cmnem.Count];
                int[] fract_length = new int[Cmnem.Count];
                double?[][] array_data = new double?[Cmnem.Count][];

                OutStream.Write("~A");

                //Секция начала данных
                for (int i = 0; i < Cmnem.Count; i++)
                {
                    int length = CountLengthForPrintDouble(countInt[Cmnem[i]], countFract[Cmnem[i]]);
                    int max_length = length;
                    if (i == 0)
                    {
                        max_length -= 3;
                    }
                    int start_idx = Math.Clamp(Cmnem[i].Length - max_length, 0, Cmnem[i].Length);
                    string title = Cmnem[i].Substring(start_idx);
                    if (i == 0)
                    {
                        title = "";
                    }
                    if (!IsWrap)
                    {
                        OutStream.Write(string.Format("{0," + (max_length + 1) + "}", title));
                    }
                    total_length[i] = length;
                    fract_length[i] = countFract[Cmnem[i]];
                    array_data[i] = Data[Cmnem[i]];
                }
                OutStream.WriteLine();

                OutStream.Flush();

                //Печать данных
                //Нужно применить форматирование
                for (int i = 0; i < Grid.Length; i++)
                {
                    int length_string = 0;
                    for (int j = 0; j < array_data.Length; j++)
                    {
                        //int length=CountLengthForPrintDouble(countInt[Cmnem[j]],countFract[Cmnem[j]]);
                        double val = array_data[j][i] ?? Null;
                        if (j != 0)
                        {
                            if (IsWrap && length_string + total_length[j] >= 80)
                            {
                                length_string = 0;
                                OutStream.WriteLine();
                            }
                            else
                            {
                                OutStream.Write(" ");
                                length_string++;
                            }
                        }
                        if (double.IsInfinity(val) || double.IsNaN(val))
                        {
                            val = Null;
                        }
                        string tmp_str = string.Format(NumberFormatInfo.InvariantInfo, "{0," + total_length[j] + ":F" + fract_length[j] + "}", val);
                        OutStream.Write(tmp_str);
                        length_string += total_length[j];

                        if (IsWrap)
                        {
                            if (j == 0)
                            {
                                length_string = 0;
                                OutStream.WriteLine();
                            }
                        }
                    }
                    if (i % 100 == 0)
                    {
                        SendCallback(0.05 + 0.95 * (i / ((double)Grid.Length)));
                    }
                    OutStream.WriteLine();
                }
                OutStream.Flush();
                OutStream.Close();
            }
            GC.Collect();
        }

        private void Clear()
        {
            FileInfo = "";
            Null = -999.25;
            Wmnem = new List<string>();
            Wunit = new List<string>();
            Wvalue = new List<string>();
            Wdesc = new List<string>();
            Cmnem = new List<string>();
            Cunit = new List<string>();
            Cvalue = new List<string>();
            Cdesc = new List<string>();
            Pmnem = new List<string>();
            Punit = new List<string>();
            Pvalue = new List<string>();
            Pdesc = new List<string>();
            Other = "";
            Data = new Dictionary<string, double?[]>();
            IsWrap = false;
        }

        //Функция проверяет что секция началась
        private static char? StartSection(string? s)
        {
            if (s != null && s.Length >= 2)
            {
                if (s[0] != '~')
                {
                    return null;
                }
                return char.ToUpper(s[1]);
            }
            return null;
        }

        private static bool TryParseLineHeader(
            string? s,
            [NotNullWhen(true)] out string? mnem,
            [NotNullWhen(true)] out string? unit,
            [NotNullWhen(true)] out string? value,
            [NotNullWhen(true)] out string? desc)
        {
            mnem = default;
            unit = default;
            value = default;
            desc = default;
            if (!string.IsNullOrEmpty(s))
            {
                if (s[0] == '#')
                {
                    return false;
                }
                int idx_point = 0;
                while (s[idx_point] != '.')
                {
                    idx_point++;
                    if (idx_point >= s.Length)
                    {
                        throw new Exception("Строка не может быть разобрана" + " (" + s + ")");
                    }
                }
                int idx_space = idx_point;

                while (!(char.IsSeparator(s[idx_space]) || char.IsWhiteSpace(s[idx_space])))
                {
                    idx_space++;
                    if (idx_space >= s.Length)
                    {
                        throw new Exception("Строка не может быть разобрана" + " (" + s + ")");
                    }
                }

                int idx_double_point = idx_space;
                while (s[idx_double_point] != ':')
                {
                    idx_double_point++;
                    if (idx_double_point >= s.Length)
                    {
                        throw new Exception("Строка не может быть разобрана" + " (" + s + ")");
                    }
                }
                mnem = s.Substring(0, idx_point).Trim();
                unit = s.Substring(idx_point + 1, (idx_space - idx_point) - 1).Trim();
                value = s.Substring(idx_space + 1, (idx_double_point - idx_space) - 1).Trim();
                desc = s.Substring(idx_double_point + 1).Trim();
                //Дальнейший разбор
                return true;
            }
            return false;
        }

        double?[]? ParseLineData(string? s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                if (s[0] == '#')
                    return null;
                string[] tmp = s.Split(new char[] { ' ', '\r', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                double?[] res = new double?[tmp.Length];
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!double.TryParse(tmp[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        val = Null;
                        /*throw new Exception("Не преобразуется в double "+tmp[i]);*/
                    }
                    if (Math.Abs(val - Null) < 0.00001)
                    {
                        res[i] = null;
                    }
                    else
                    {
                        res[i] = val;
                    }
                }
                return res;
            }
            return null;
        }

        public string GetHeaderValue(string sec, string mnem)
        {
            switch (sec)
            {
                case "W":
                    for (int i = 0; i < Wmnem.Count; i++)
                    {
                        if (Wmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Wvalue[i];
                        }
                    }
                    break;
                case "C":
                    for (int i = 0; i < Cmnem.Count; i++)
                    {
                        if (Cmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Cvalue[i];
                        }
                    }
                    break;
                case "P":
                    for (int i = 0; i < Pmnem.Count; i++)
                    {
                        if (Pmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Pvalue[i];
                        }
                    }
                    break;
            }
            throw new Exception("Не найдена мнемоника " + mnem + " в секции " + sec);
        }

        public string GetHeaderUnit(string sec, string mnem)
        {
            switch (sec)
            {
                case "W":
                    for (int i = 0; i < Wmnem.Count; i++)
                    {
                        if (Wmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Wunit[i];
                        }
                    }
                    break;
                case "C":
                    for (int i = 0; i < Cmnem.Count; i++)
                    {
                        if (Cmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Cunit[i];
                        }
                    }
                    break;
                case "P":
                    for (int i = 0; i < Pmnem.Count; i++)
                    {
                        if (Pmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Punit[i];
                        }
                    }
                    break;
            }
            throw new Exception("Не найдена мнемоника " + mnem + " в секции " + sec);
        }

        public string GetHeaderDesc(string sec, string mnem)
        {
            switch (sec)
            {
                case "W":
                    for (int i = 0; i < Wmnem.Count; i++)
                    {
                        if (Wmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Wdesc[i];
                        }
                    }
                    break;
                case "C":
                    for (int i = 0; i < Cmnem.Count; i++)
                    {
                        if (Cmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Cdesc[i];
                        }
                    }
                    break;
                case "P":
                    for (int i = 0; i < Pmnem.Count; i++)
                    {
                        if (Pmnem[i].Equals(mnem, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Pdesc[i];
                        }
                    }
                    break;
            }
            throw new Exception("Не найдена мнемоника " + mnem + " в секции " + sec);
        }

        private void SendCallback(double procent)
        {
            CallBack?.Invoke(procent);
        }

        public void ReadFile(string namefile, string codepage)
        {
            long line_count = 0;
            GC.Collect();
            StringBuilder sb = new StringBuilder();
            using var stream = new FileStream(namefile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var InStream = new StreamReader(stream, Encoding.GetEncoding(codepage));

            Clear();
            char cur = 'I';
            while (cur != 'A')
            {
                string? s = InStream.ReadLine();
                line_count++;
                if (InStream.EndOfStream)
                {
                    throw new Exception(BuildErrorMessage("Неожиданное окончание файла", line_count));
                }
                char? section = StartSection(s);
                if (section != null)
                {
                    cur = section.Value;
                    continue;
                }
                if (cur != 'O')
                {
                    if (TryParseLineHeader(s, out var mnem, out var unit, out var value, out var desc))
                    {
                        switch (cur)
                        {
                            case 'V':
                                if (mnem.Equals("VERS", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (!value.Equals("2.0"))
                                    {
                                        throw new Exception(BuildErrorMessage("Поддерживается только формат LAS 2.0", line_count));
                                    }
                                }
                                if (mnem.Equals("WRAP", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (value.Equals("YES", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        IsWrap = true;
                                    }
                                    else
                                    {
                                        if (value.Equals("NO", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            IsWrap = false;
                                        }
                                        else
                                        {
                                            throw new Exception(BuildErrorMessage("В данный момент не поддерживается режим WRAP отличныйй от YES и NO", line_count));
                                        }
                                    }


                                }
                                break;
                            case 'W':
                                WAdd(mnem, unit, value, desc);
                                break;
                            case 'C':
                                CAddHeader(mnem, unit, value, desc);
                                break;
                            case 'P':
                                PAdd(mnem, unit, value, desc);
                                break;
                        }
                    }
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('\n');
                    }
                    sb.Append(s);

                    //if (!Other.Equals(""))
                    //{
                    //    Other += "\n";
                    //}
                    //Other += s;
                }
            }

            Other = sb.ToString();

            //Здесь проверка параметров входных 
            string null_value = GetHeaderValue("W", "NULL");
            if (!double.TryParse(null_value, NumberStyles.Float, CultureInfo.InvariantCulture, out double tmp_null))
            {
                throw new Exception("Неверный формат значения NULL " + null_value);
            }
            Null = tmp_null;

            List<double?>[] data = new List<double?>[Cmnem.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new List<double?>();
            }
            int count_update = 0;
            const int size_block = 100000;
            int length_string = 0;

            while (!InStream.EndOfStream)
            {
                string? s = InStream.ReadLine();
                line_count++;
                double?[]? vals = null;
                try
                {
                    vals = ParseLineData(s);
                }
                catch (Exception e)
                {
                    throw new Exception(BuildErrorMessage(e.Message, line_count));
                }
                if (vals != null)
                {
                    if (length_string + vals.Length <= Cmnem.Count)
                    {
                        for (int i = 0; i < vals.Length; i++)
                        {
                            data[length_string + i].Add(vals[i]);
                        }
                    }
                    else
                    {
                        throw new Exception(BuildErrorMessage("Неверное количесво элементов в строке", line_count));
                        //Ошибка разбора LAS
                    }
                    length_string += vals.Length;
                    if (length_string == Cmnem.Count)
                    {
                        length_string = 0;
                    }
                }
                if ((count_update + 1) * size_block < stream.Position)
                {
                    count_update++;
                    SendCallback(stream.Position / (double)stream.Length);
                }
            }

            for (int i = 0; i < Cmnem.Count; i++)
            {
                Data[Cmnem[i]] = data[i].ToArray();
                data[i] = null!;
                GC.Collect();
            }


            GC.Collect();
        }

        private static string BuildErrorMessage(string message, long num_line)
        {
            return message + " (" + num_line.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
