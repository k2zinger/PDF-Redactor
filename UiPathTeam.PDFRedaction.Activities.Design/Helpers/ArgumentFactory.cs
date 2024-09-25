using System;
using System.Activities;
using System.Activities.Expressions;
using System.Diagnostics;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;

namespace UiPathTeam.PDFRedaction.Activities.Design.Helpers;

public class ArgumentFactory
{
    private readonly Language _language;

    public ArgumentFactory(Language language) => this._language = language;

    public InArgument<T> CreateLiteralArgument<T>(T value) => new InArgument<T>(new Literal<T>(value));

    public InArgument<T> CreateValueArgument<T>(string expressionText)
    {
        if (this._language == Language.VisualBasic)
            return new InArgument<T>()
            {
                Expression = new VisualBasicValue<T>(expressionText)
            };
        return new InArgument<T>()
        {
            Expression = new CSharpValue<T>(expressionText)
        };
    }

    public Variable<T> CreateVariableWithDefaultValue<T>(string defaultValue, string name)
    {
        if (this._language == Language.VisualBasic)
        {
            Variable<T> withDefaultValue = new();
            withDefaultValue.Name = name;
            withDefaultValue.Default = new VisualBasicValue<T>(defaultValue);
            return withDefaultValue;
        }
        Variable<T> withDefaultValue1 = new Variable<T>();
        withDefaultValue1.Name = name;
        withDefaultValue1.Default = new CSharpValue<T>(defaultValue);
        return withDefaultValue1;
    }

    public InArgument CreateLiteralWithReflection(object value)
    {
        if (value == null)
            return (InArgument)null;
        try
        {
            object instance = Activator.CreateInstance(typeof(Literal<>).MakeGenericType(value.GetType()), value);
            return Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(value.GetType()), instance) as InArgument;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
        return (InArgument)null;
    }

    public InArgument CreateValueArgumentWithReflection(
      string expressionText,
      Type argumentType)
    {
        if (string.IsNullOrWhiteSpace(expressionText))
            return null;
        try
        {
            object instance = Activator.CreateInstance((this._language == Language.VisualBasic ? typeof(VisualBasicValue<>) : typeof(CSharpValue<>)).MakeGenericType(argumentType), (object)expressionText);
            return Activator.CreateInstance(typeof(InArgument<>).MakeGenericType(argumentType), instance) as InArgument;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(ex.ToString());
        }
        return null;
    }

    public OutArgument<T> CreateOutArgumentReference<T>(string name) => this._language != Language.VisualBasic ? (OutArgument<T>)new CSharpReference<T>(name) : (OutArgument<T>)(Activity<Location<T>>)new VisualBasicReference<T>(name);

    public InOutArgument<T> CreateInOutArgumentReference<T>(string name) => this._language != Language.VisualBasic ? (InOutArgument<T>)new CSharpReference<T>(name) : (InOutArgument<T>)(Activity<Location<T>>)new VisualBasicReference<T>(name);

    public Type GetTypeOfReference() => this._language != Language.VisualBasic ? typeof(CSharpReference<>) : typeof(VisualBasicReference<>);

    public Type GetTypeOfValue() => this._language != Language.VisualBasic ? typeof(CSharpValue<>) : typeof(VisualBasicValue<>);
}