using System.Activities.Presentation.Metadata;
using System.ComponentModel;

namespace UiPathTeam.PDFRedaction.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();


            builder.AddCustomAttributes(typeof(PDFRedaction), new DesignerAttribute(typeof(PDFRedactionDesigner)));
            builder.AddCustomAttributes(typeof(PDFRedaction), new BrowsableAttribute(true));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
