using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(VerbsData.Publish, "RedisMessage")]
    public sealed class PublishRedisMessageCommand : RedisClientCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RedisValue Value { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public RedisChannel Channel { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            foreach (var session in GetDeclaredRedisSessions(out _))
            {
                var subscriber = session.Connection.GetSubscriber();
                var subscriberCount = subscriber.Publish(Channel, Value, CommandFlags.None);
                if (PassThru)
                {
                    var result = new RedisPublishResult(Channel, Value, session.InstanceId, subscriberCount);
                    WriteObject(result);
                }
            }
            base.ProcessRecord();
        }
    }
}