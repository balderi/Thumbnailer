using System;

namespace libthumbnailer2
{
    /// <summary>
    /// Contains various methods for performing conversions.
    /// </summary>
    public class Converter
    {
        /// <summary>
        /// Converts a byte value to KiB, MiB, or GiB.
        /// </summary>
        /// <param name="value">Byte value as <see cref="string"/>.</param>
        /// <returns>Converted value, or 0 on parse error.</returns>
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

        /// <summary>
        /// Converts a byte value to KiB, MiB, or GiB.
        /// </summary>
        /// <param name="value">Byte value as <see cref="long"/>.</param>
        /// <returns>Converted value.</returns>
        public static string ToKiB(long value)
        {
            if (value / 1073741824 > 1)
            {
                return (value / 1073741824).ToString("N2") + " Gi";
            }
            else if (value / 1048576 > 1)
            {
                return (value / 1048576).ToString("N2") + " Mi";
            }
            else if (value / 1024 > 1)
            {
                return (value / 1024).ToString("N2") + " Ki";
            }
            else
            {
                return (value).ToString();
            }
        }

        /// <summary>
        /// Converts a byte value to KB, MB, or GB.
        /// </summary>
        /// <param name="value">Byte value as <see cref="string"/>.</param>
        /// <returns>Converted value, or 0 on parse error.</returns>
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

        /// <summary>
        /// Converts a byte value to KB, MB, or GB.
        /// </summary>
        /// <param name="value">Byte value as <see cref="long"/>.</param>
        /// <returns>Converted value.</returns>
        public static string ToKB(long value)
        {
            if (value / 1000000000 > 1)
            {
                return (value / 1000000000).ToString("N2") + " G";
            }
            else if (value / 1000000 > 1)
            {
                return (value / 1000000).ToString("N2") + " M";
            }
            else if (value / 1000 > 1)
            {
                return (value / 1000).ToString("N2") + " k";
            }
            else
            {
                return (value).ToString();
            }
        }

        /// <summary>
        /// Converts seconds into <c>HH:MM:SS</c> format.
        /// </summary>
        /// <param name="value">Seconds as <see cref="double"/>.</param>
        /// <returns>A <see cref="string"/> in <c>HH:MM:SS</c> format.</returns>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ToHMS(double value)
        {
            TimeSpan t = TimeSpan.FromSeconds(value);
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }
    }
}
