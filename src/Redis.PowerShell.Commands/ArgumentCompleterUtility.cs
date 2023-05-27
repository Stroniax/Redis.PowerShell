using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using PSValueWildcard;

namespace Redis.PowerShell
{
    internal static class ArgumentCompleterUtility
    {
        public static SessionState? SessionState { get; set; }
        public static RedisSessionCollection? Redis { get; set; }

        public static IEnumerable<RedisSession> GetRedisSessions(IDictionary fakeBoundParameters)
        {
            if (Redis is null)
            {
                return Array.Empty<RedisSession>();
            }
            if (fakeBoundParameters["Session"] is RedisSession session)
            {
                return new[] { session };
            }
            return Array.Empty<RedisSession>();
        }

        public static bool IsMatch(
            string wordToComplete,
            string completion,
            out string completionText
        )
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                completionText = AddNecessaryQuotes(completion);
                return true;
            }

            if (ValueWildcardPattern.IsMatch(wordToComplete, completion))
            {
                completionText = AddNecessaryQuotes(completion);
                return true;
            }

            // trim quotes and try again
            char quoteStyle;
            ReadOnlySpan<char> wildcardSpan = wordToComplete.AsSpan();
            if (wildcardSpan[0] == '\'')
            {
                quoteStyle = '\'';
            }
            else if (wordToComplete[0] == '"')
            {
                quoteStyle = '"';
            }
            else
            {
                completionText = completion;
                return false;
            }

            var atEnd = wildcardSpan[wildcardSpan.Length - 1] == quoteStyle;
            var sliceLength = atEnd ? wildcardSpan.Length - 2 : wildcardSpan.Length - 1;
            wildcardSpan = wildcardSpan.Slice(1, sliceLength);

            var wildcard = wildcardSpan.ToString() + '*';
            if (!ValueWildcardPattern.IsMatch(wildcard, completion))
            {
                completionText = completion;
                return false;
            }
            switch (quoteStyle)
            {
                case '\'':
                    completionText =
                        '\'' + CodeGeneration.EscapeSingleQuotedStringContent(completion) + '\'';
                    return true;
                case '\"':
                    completionText = quoteStyle + completion.Replace("\"", "`\"") + quoteStyle;
                    return true;
                default:
                    throw new InvalidOperationException(
                        $"Switch expression not matched for quote style: {quoteStyle}."
                    );
            }
        }

        private static string AddNecessaryQuotes(string completionText)
        {
            if (
                completionText.Contains(" ")
                || completionText.Contains("\"")
                || completionText.Contains("'")
            )
            {
                // wrap the text in single quotes
                return '\'' + CodeGeneration.EscapeSingleQuotedStringContent(completionText) + '\'';
            }

            // no quotes needed
            return completionText;
        }
    }
}
