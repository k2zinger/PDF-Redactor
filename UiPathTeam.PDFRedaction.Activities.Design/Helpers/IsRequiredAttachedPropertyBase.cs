using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using System.Windows.Media;

namespace UiPathTeam.PDFRedaction.Activities.Design
{
    public class IsRequiredAttachedPropertyBase<T> where T : FrameworkElement
    {
        protected const string AsteriskText = " *";
        protected const string RequiredText = " (Required)";
        private static SolidColorBrush _asteriskBrush;

        protected static Run InitializeAsterisk()
        {
            try
            {
                if (_asteriskBrush == null)
                {
                    if (Application.Current?.TryFindResource("AsteriskColor") is not SolidColorBrush solidColorBrush)
                        solidColorBrush = Brushes.Blue;
                    _asteriskBrush = solidColorBrush;
                }
            }
            catch
            {
                _asteriskBrush = Brushes.Blue;
            }
            Run run = new(" *")
            {
                Foreground = _asteriskBrush,
                FontWeight = FontWeights.DemiBold,
                BaselineAlignment = BaselineAlignment.TextTop
            };
            return run;
        }

        protected static void AddRequiredToAutomationName(DependencyObject element, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            AutomationProperties.SetName(element, text + " (Required)");
        }

        protected static void RemoveRequiredFromAutomationName(DependencyObject element)
        {
            string name = AutomationProperties.GetName(element);
            if (string.IsNullOrEmpty(name))
                return;
            string str = name.Replace(" (Required)", string.Empty);
            AutomationProperties.SetName(element, str);
        }
    }
}
