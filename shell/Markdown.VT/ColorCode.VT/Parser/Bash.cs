﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ColorCode.Common;

namespace ColorCode.VT;

public class Bash : ILanguage
{
    public string Id => "bash";
    public string Name => "bash";
    public string CssClassName => "bash";
    public string FirstLinePattern => null;

    internal const string BashCommentScope = "Bash Comment";

    public IList<LanguageRule> Rules =>
        new List<LanguageRule>
        {
            new LanguageRule(
                @"(#.*?)\r?$",
                new Dictionary<int, string>
                {
                    {1, BashCommentScope}
                }),

            // match the first word of a line in a multi-line string as the command name.
            new LanguageRule(
                @"(?m)^\s*(\w+)",
                new Dictionary<int, string>
                {
                    {1, ScopeName.PowerShellCommand}
                }),

            // match options like '-word'
            new LanguageRule(
                @"\s(-\w+)",
                new Dictionary<int, string>
                {
                    {1, ScopeName.PowerShellParameter}
                }),

            // match options like '--word', '--word-word', and '--word-word-word', but not '--word-' or '--word-word-'.
            // Also match potential value for the option that is specified in the form of '--word=value'.
            new LanguageRule(
                @"\s(--(?:\w+-)*\w+)(?:=(?:\w+-)*\w+)?",
                new Dictionary<int, string>
                    {
                        {1, ScopeName.PowerShellParameter}
                    }
                ),

            // match variable like '$word', '$digit', '$word_word' and etc.
            new LanguageRule(
                @"\$\w+",
                new Dictionary<int, string>
                    {
                        {0, ScopeName.PowerShellVariable}
                    }
                ),
        };

    public bool HasAlias(string lang)
    {
        switch (lang.ToLower())
        {
            case "sh":
                return true;

            default:
                return false;
        }
    }
}
