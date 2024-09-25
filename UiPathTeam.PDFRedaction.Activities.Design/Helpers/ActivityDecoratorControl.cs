using System.Windows;
using System.Windows.Controls;

namespace UiPathTeam.PDFRedaction.Activities.Design
{
    public class ActivityDecoratorControl : ContentControl
    {
        static ActivityDecoratorControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ActivityDecoratorControl), new FrameworkPropertyMetadata((object)typeof(ActivityDecoratorControl)));
    }
}
