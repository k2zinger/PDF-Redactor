using System;
using System.Activities.Presentation.Model;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UiPathTeam.PDFRedaction.Activities.Design.Converters
{
    public class ActivityIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return (object)null;
                Type itemType = (value as ModelItem).ItemType;
                string name = itemType.Name;
                if (itemType.IsGenericType)
                    name = name.Split('`')[0];
                string key = name + "Icon";
                if (!(new ResourceDictionary()
                {
                    Source = new Uri(parameter as string)
                }[(object)key] is DrawingBrush resource))
                    resource = Application.Current.Resources[(object)key] as DrawingBrush;
                if (resource == null)
                    resource = Application.Current.Resources[(object)"GenericLeafActivityIcon"] as DrawingBrush;
                return (object)resource.Drawing;
            }
            catch
            {
                return (object)null;
            }
        }

        public object ConvertBack(
          object value,
          Type targetType,
          object parameter,
          CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}