using System;
using System.Activities;

namespace UiPathTeam.PDFRedaction.Activities.Design.Helpers;

public static class ArgumentFactoryHelper
{
    private static ArgumentFactory _argumentFactory;

    public static Language ProjectLanguage { get; private set; } = Language.VisualBasic;

    public static ICSharpExpressionParser CSharpExpressionParser { get; private set; } = new DefaultCSharpExpressionParser();

    public static void SetProjectLanguage(int language)
    {
        if ((Language)language == ProjectLanguage)
            return;
        ProjectLanguage = language == 0 ? Language.VisualBasic : Language.CSharp;
        _argumentFactory = null;
    }

    public static void SetCSharpExpressionParser(ICSharpExpressionParser csharpExpressionParser) => CSharpExpressionParser = csharpExpressionParser ?? throw new ArgumentNullException(nameof(csharpExpressionParser));

    public static InArgument<T> CreateLiteralArgument<T>(T value) => GetFactory().CreateLiteralArgument(value);

    public static InArgument<T> CreateValueArgument<T>(string expressionText) => GetFactory().CreateValueArgument<T>(expressionText);

    public static Variable<T> CreateVariableWithDefaultValue<T>(
      string defaultValue,
      string name)
    {
        return GetFactory().CreateVariableWithDefaultValue<T>(defaultValue, name);
    }

    public static OutArgument<T> CreateOutArgumentReference<T>(string name) => GetFactory().CreateOutArgumentReference<T>(name);

    public static InOutArgument<T> CreateInOutArgumentReference<T>(string name) => GetFactory().CreateInOutArgumentReference<T>(name);

    public static InArgument CreateLiteralWithReflection(object value) => GetFactory().CreateLiteralWithReflection(value);

    public static InArgument CreateValueArgumentWithReflection(
      string expressionText,
      Type argumentType)
    {
        return GetFactory().CreateValueArgumentWithReflection(expressionText, argumentType);
    }

    public static Type GetTypeOfReference() => GetFactory().GetTypeOfReference();

    public static Type GetTypeOfValue() => GetFactory().GetTypeOfValue();

    private static ArgumentFactory GetFactory() => _argumentFactory ?? (_argumentFactory = new ArgumentFactory(ProjectLanguage));
}