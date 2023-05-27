using System;
using System.Management.Automation;

namespace Redis.PowerShell
{
    public sealed class TimeSpanToMillisecondsTransformationAttribute
        : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            return inputData switch
            {
                TimeSpan ts => ts.TotalMilliseconds,
                string s when TimeSpan.TryParse(s, out var ts) => ts.TotalMilliseconds,
                _ => inputData,
            };
        }
    }
}
