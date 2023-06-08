using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsDiagnostic.Test,
        "RedisConnection",
        ConfirmImpact = ConfirmImpact.Low,
        SupportsShouldProcess = true,
        RemotingCapability = RemotingCapability.OwnedByCommand
    )]
    public sealed class TestRedisConnectionCommand : RedisClientCmdlet
    {
        [Parameter]
        public SwitchParameter Quiet { get; set; }

        private readonly List<Task<RedisPingResult>> _pings = new List<Task<RedisPingResult>>();

        protected override void ProcessRecord()
        {
            foreach (var sn in GetDeclaredRedisSessions(out _))
            {
                if (ShouldProcess(sn.InstanceId.ToString(), "send Redis PING"))
                {
                    _pings.Add(PingSession(sn));
                }
            }
        }

        protected override void EndProcessing()
        {
            while (_pings.Count > 0 && !Stopping)
            {
                var index = Task.WaitAny(_pings.ToArray(), 100);
                if (index == -1)
                {
                    continue;
                }

                var completedTask = _pings[index];
                _pings.RemoveAt(index);

                if (Quiet)
                {
                    WriteObject(completedTask.Result.Exception is null);
                }
                else if (completedTask.Result.Exception is null)
                {
                    WriteObject(completedTask.Result);
                }
                else
                {
                    var error = new ErrorRecord(
                        completedTask.Result.Exception,
                        "RedisPingFailure",
                        ErrorCategory.ConnectionError,
                        completedTask.Result.Session
                    );

                    WriteError(error);
                }
            }
        }

        private async Task<RedisPingResult> PingSession(RedisSession session)
        {
            try
            {
                var timespan = await session.Database.PingAsync();
                return new RedisPingResult(session, timespan);
            }
            catch (Exception e)
            {
                return new RedisPingResult(session, e);
            }
        }
    }
}
