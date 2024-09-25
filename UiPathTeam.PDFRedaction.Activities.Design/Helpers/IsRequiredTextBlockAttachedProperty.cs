using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace UiPathTeam.PDFRedaction.Activities.Design
{
    public class IsRequiredTextBlockAttachedProperty : IsRequiredAttachedPropertyBase<TextBlock>
    {
        public static readonly DependencyProperty IsRequiredProperty = DependencyProperty.RegisterAttached("IsRequired", typeof(bool), typeof(IsRequiredTextBlockAttachedProperty), new PropertyMetadata(false, OnIsRequiredChanged));
        public static bool GetIsRequired(UIElement element) => (bool)element?.GetValue(IsRequiredProperty);

        public static void SetIsRequired(UIElement element, bool value) => element?.SetValue(IsRequiredProperty, value);

        private static void OnIsRequiredChanged(
          DependencyObject sender,
          DependencyPropertyChangedEventArgs e)
        {
            if (sender is not TextBlock textBlock)
                return;
            if ((bool)e.NewValue)
            {
                AddRequiredToAutomationName(sender, textBlock.Text);
                Run run = InitializeAsterisk();
                textBlock.Inlines.Add(run);
            }
            else
            {
                if (textBlock.Inlines.LastInline is Run lastInline && lastInline.Text == " *")
                    textBlock.Inlines.Remove(lastInline);
                RemoveRequiredFromAutomationName(sender);
            }
        }
    }
}
