using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "RedisKey")]
    public sealed class GetRedisKeyCommand : RedisClientCmdlet
    {
        [Parameter]
        public string Key { get; set; } = "*";

        protected override void ProcessRecord()
        {
            foreach (var session in GetDeclaredRedisSessions(out _))
            {
                var database = session.Database;

                var keys = session.Connection.GetServer("localhost", 6379).Keys(pattern: Key);

                WriteObject(keys, true);
            }
        }
    }
}
