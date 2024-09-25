using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Markup;
using UiPathTeam.PDFRedaction.Activities.Design.Helpers;

namespace UiPathTeam.PDFRedaction.Activities.Design;

public partial class PDFRedactionDesigner : ActivityDesigner, IComponentConnector
{
    public PDFRedactionDesigner()
    {
        this.InitializeComponent();
        this.OCREngineContainer.AllowedItemType = typeof(Activity<IEnumerable<KeyValuePair<Rectangle, string>>>);
        this.Loaded += new RoutedEventHandler(this.PDFRedactionDesigner_Loaded);
    }

    private void PDFRedactionDesigner_Loaded(object sender, RoutedEventArgs e)
    {
        ((ModelMemberCollection<ModelProperty, DependencyProperty>)this.ModelItem.Properties)["OCREngine"].Value.PropertyChanged += new PropertyChangedEventHandler(this.OCREngineChanged);
    }

    private void OCREngineChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!(e.PropertyName == "Handler") || ((ModelMemberCollection<ModelProperty, DependencyProperty>)((ModelMemberCollection<ModelProperty, DependencyProperty>)this.ModelItem.Properties)["OCREngine"].Value.Properties)["Handler"].ComputedValue == null)
            return;
        ((ModelMemberCollection<ModelProperty, DependencyProperty>)((ModelMemberCollection<ModelProperty, DependencyProperty>)((ModelMemberCollection<ModelProperty, DependencyProperty>)this.ModelItem.Properties)["OCREngine"].Value.Properties)["Handler"].Value.Properties)["Image"].SetValue(ArgumentFactoryHelper.CreateValueArgument<Image>("Image"));
    }
}