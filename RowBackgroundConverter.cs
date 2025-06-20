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
                // Get the index of the item within the ListView
                int index = listView.IndexFromContainer(item);

                // Alternate colors based on index (even or odd)
                return (index % 2 == 0)
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            }

            // Default color in case something fails
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
