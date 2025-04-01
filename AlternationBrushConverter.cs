using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace BeaconScan
{
    public partial class AlternationBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isEven = (bool)value;
            // Devuelve LightGray para filas pares y White para filas impares (ajusta los colores según lo necesites)
            return isEven ? new SolidColorBrush(Colors.DeepSkyBlue) : new SolidColorBrush(Colors.DarkBlue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}