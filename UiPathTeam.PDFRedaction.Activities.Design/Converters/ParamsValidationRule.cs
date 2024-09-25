using System.Windows.Controls;

namespace UiPathTeam.PDFRedaction.Activities.Design.Converters
{
    public class ParamsValidationRule : ValidationRule
    {
        public override System.Windows.Controls.ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is System.Windows.Data.BindingGroup val)
            {
                var items = val.Items;

                if (string.IsNullOrEmpty(items[0].ToString()))
                {
                    new ValidationResult(false, "aaaaaaaaa");
                }
            }
            return System.Windows.Controls.ValidationResult.ValidResult;
        }
    }
}
