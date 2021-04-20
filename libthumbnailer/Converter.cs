using System;

namespace libthumbnailer
{
    public class Converter
    {
        public static string ToKiB(string value)
        {
            if (int.TryParse(value, out int result))
            {
                double temp = result;

                if (temp / 1073741824 > 1)
                {
                    return (temp / 1073741824).ToString("N2") + " Gi";
                }
                else if (temp / 1048576 > 1)
                {
                    return (temp / 1048576).ToString("N2") + " Mi";
                }
                else if (temp / 1024 > 1)
                {
                    return (temp / 1024).ToString("N2") + " Ki";
                }
                else
                {
                    return (temp).ToString();
                }
            }
            else
            {
                return "0 ";
            }
        }

        public static string ToKB(string value)
        {
            if (int.TryParse(value, out int result))
            {
                double temp = result;

                if (temp / 1000000000 > 1)
                {
                    return (temp / 1000000000).ToString("N2") + " G";
                }
                else if (temp / 1000000 > 1)
                {
                    return (temp / 1000000).ToString("N2") + " M";
                }
                else if (temp / 1000 > 1)
                {
                    return (temp / 1000).ToString("N2") + " k";
                }
                else
                {
                    return (temp).ToString();
                }
            }
            else
            {
                return "0 ";
            }
        }

        public static string ToHMS(double value)
        {
            TimeSpan t = TimeSpan.FromSeconds(value);
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }
    }
}
