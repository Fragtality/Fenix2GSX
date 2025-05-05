using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Fenix2GSX.UI
{
    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan span)
                return $"{(int)span.TotalMinutes}";
            else
                return $"0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str) && double.TryParse(str, new RealInvariantFormat(str), out double span))
                return TimeSpan.FromMinutes((int)span);
            else
                return TimeSpan.Zero;
        }
    }
}
