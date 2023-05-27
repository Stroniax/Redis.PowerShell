using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Redis.PowerShell
{
    public sealed class ValidateCommandFlags : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is CommandFlags commandFlags)
            {
                Validate(commandFlags, engineIntrinsics);
            }
        }

        private void Validate(CommandFlags commandFlags, EngineIntrinsics engineIntrinsics)
        {
            if (commandFlags.HasFlag(CommandFlags.FireAndForget))
            {
                throw new ArgumentException("FireAndForget is not supported.");
            }
        }
    }
}
