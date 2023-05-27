using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    public sealed class SocketManagerCompleter : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters
        )
        {
            if (
                ArgumentCompleterUtility.IsMatch(
                    wordToComplete,
                    nameof(SocketManager.Shared),
                    out var completionText
                )
            )
            {
                yield return new CompletionResult(
                    completionText,
                    "Shared",
                    CompletionResultType.ParameterValue,
                    "Shared"
                );
            }
            if (
                ArgumentCompleterUtility.IsMatch(
                    wordToComplete,
                    nameof(SocketManager.ThreadPool),
                    out completionText
                )
            )
            {
                yield return new CompletionResult(
                    completionText,
                    "ThreadPool",
                    CompletionResultType.ParameterValue,
                    "ThreadPool"
                );
            }
        }
    }
}
