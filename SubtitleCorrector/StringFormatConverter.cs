namespace SiretT.Converters {
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Windows.Data;

    /// <summary>
    /// Aplica un StringFormat en <paramref name="parameter"/> al objeto especififcado.
    /// </summary>
    public sealed class StringFormatConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string format = parameter.ToString();
            Match match;
            if ((match = Regex.Match(format, "[0-9]+:[0-9][PD]", RegexOptions.IgnoreCase)).Success) {
                int indx = match.Value.IndexOf(":");
                int arg = System.Convert.ToInt32(match.Value.Substring(0, indx));
                int decimals = match.Value.Substring(indx + 1, 1)[0] - '0';
                char opcion = match.Value.Substring(indx + 2, 1)[0];
                if (opcion == 'P' || opcion == 'p') {
                    if (value is double || value is int || value is float) {
                        return String.Format("{" + arg + "} %", Math.Round(System.Convert.ToDouble(value) * 100d, decimals));
                    }
                } else if (opcion == 'D' || opcion == 'd') {
                    if (value is double || value is int || value is float) {
                        return String.Format("{" + arg + "}", Math.Round(System.Convert.ToDouble(value), decimals));
                    }
                }
            }
            return String.Format(format, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }

    public class TimeSpanToMillisecondsConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is TimeSpan) {
                return ((TimeSpan)value).TotalMilliseconds;
            }
            return 0d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double || value is int)
                return TimeSpan.FromMilliseconds((double)value);
            return TimeSpan.Zero;
        }
    }

}
