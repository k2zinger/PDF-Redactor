using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace UiPathTeam.PDFRedaction.Activities;

public class RedactionProcessorBase
{
    public static string CreateFormula(string formula, string[] keywords, string[] formulaAuto, bool silent)
    {
        var regexPatterns = new List<string>();
        keywords ??= Array.Empty<string>();

        var trimmedKeywords = keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToArray();
        if (trimmedKeywords.Length > 0)
        {
            var keywordPattern = string.Join("|", trimmedKeywords);
            regexPatterns.Add($"(?i)({keywordPattern})");
        }

        var autoPatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ssn", @"\d{3}-\d{2}-\d{4}" },
            { "ein", @"\d{2}-\d{7}" },
            { "dates", @"((\d{1,2}[/-]\d{1,2}[/-]\d{4})|(\d{4}[/-]\d{1,2}[/-]\d{1,2}))" },
            { "currency", @"\$\s?-?0*(?:\d+(?!,)(?:\.\d{1,2})?|(?:\d{1,3}(?:,\d{3})*(?:\.\d{1,2})?))" },
            { "email", @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" },
            { "phone", @"(\([0-9]{3}\)|[0-9]{3}-)\s{0,1}[0-9]{3}-[0-9]{4}" }
        };

        if (formulaAuto != null)
        {
            foreach (var auto in formulaAuto)
            {
                if (autoPatterns.TryGetValue(auto, out var pattern))
                {
                    regexPatterns.Add($"({pattern})");
                }
            }
        }

        if (!string.IsNullOrEmpty(formula))
        {
            regexPatterns.Add($"({formula.Trim()})");
        }

        if (!silent)
        {
            Console.WriteLine("Keywords: " + string.Join(" | ", trimmedKeywords));
            if (formulaAuto != null)
            {
                Console.WriteLine("Common: " + string.Join(" | ", formulaAuto));
            }
            if (!string.IsNullOrEmpty(formula))
            {
                Console.WriteLine("Custom: " + formula);
            }
        }

        return string.Join("|", regexPatterns);
    }
}
