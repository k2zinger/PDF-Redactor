using System.Text;
using Microsoft.CSharp.Activities;

namespace UiPathTeam.PDFRedaction.Activities.Design.Helpers;

internal class DefaultCSharpExpressionParser : ICSharpExpressionParser
{
    public bool TryGetStringLiteral(CSharpValue<string> value, out string text)
    {
        text = (string)null;
        string str = value?.ExpressionText?.Trim();
        if (string.IsNullOrEmpty(str))
            return true;
        if (str[0] != '"' || str[str.Length - 1] != '"')
            return false;
        bool flag = false;
        StringBuilder stringBuilder = new StringBuilder();
        int index;
        for (index = 1; index < str.Length; ++index)
        {
            char ch = str[index];
            if (ch == '\\' && !flag)
                flag = true;
            else if (ch != '"' || flag)
            {
                flag = false;
                stringBuilder.Append(ch);
            }
            else
                break;
        }
        if (index < str.Length - 1)
            return false;
        text = stringBuilder.ToString();
        return true;
    }
}