using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Redis.PowerShell
{
    public sealed class RedisSessionArgumentCompleter : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters
        )
        {
            foreach (
                var redisSession in ArgumentCompleterUtility.GetRedisSessions(fakeBoundParameters)
            )
            {
                if (
                    ArgumentCompleterUtility.IsMatch(
                        redisSession.Name,
                        wordToComplete,
                        out var completionText
                    )
                )
                {
                    yield return new CompletionResult(
                        completionText,
                        redisSession.Name,
                        CompletionResultType.ParameterValue,
                        redisSession.Name
                    );
                }
            }
        }
    }
}
