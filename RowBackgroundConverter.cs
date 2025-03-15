using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System;

namespace BeaconScan
{
    public class RowBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ListViewItem item && parameter is ListView listView)
            {
                // Obtener el índice del elemento dentro del ListView
                int index = listView.IndexFromContainer(item);

                // Alternar colores según el índice (par o impar)
                return (index % 2 == 0)
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            }

            // Color predeterminado si falla algo
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
