using Microsoft.CSharp.Activities;

namespace UiPathTeam.PDFRedaction.Activities.Design.Helpers;

public interface ICSharpExpressionParser
{
    bool TryGetStringLiteral(CSharpValue<string> value, out string text);
}
