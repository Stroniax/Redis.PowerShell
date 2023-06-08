using System;
using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Register, "RedisSubscription")]
    public sealed class RegisterRedisSubscriptionCommand : RedisClientCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Channel { get; set; } = string.Empty;

        [Parameter(Mandatory = true)]
        [Alias("Action")]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string SourceIdentifier { get; set; } = nameof(RedisEventPublisher.RedisEvent);

        [Parameter]
        public PSObject? Data { get; set; }

        protected override void ProcessRecord()
        {
            var sessions = GetDeclaredRedisSessions(out _);
            foreach (var session in sessions)
            {
                var subscriber = session.Connection.GetSubscriber();

                var publisher = new RedisEventPublisher();

                var eventSubscriber = Events.SubscribeEvent(
                    publisher,
                    nameof(RedisEventPublisher.RedisEvent),
                    SourceIdentifier,
                    Data,
                    ScriptBlock,
                    false,
                    true
                );

                WriteObject(eventSubscriber);

                subscriber.Subscribe(
                    Channel,
                    (channel, value) => publisher.RaiseEvent(this, new RedisSubscriptionEventArgs(channel, value)),
                    CommandFlags.None
                );
                
            }

            base.ProcessRecord();
        }
        private sealed class RedisSubscriptionEventArgs
        {
            public RedisSubscriptionEventArgs(
                RedisChannel channel, RedisValue value
            )
            {
                Channel = channel;
                Value = value;
            }
            public RedisChannel Channel { get; }
            public RedisValue Value { get; set; }
        }
        private delegate void RedisSubscriberEventHandler(object sender, RedisSubscriptionEventArgs args);
        private class RedisEventPublisher
        {
            public event RedisSubscriberEventHandler? RedisEvent;

            internal void RaiseEvent(object sender, RedisSubscriptionEventArgs args) => RedisEvent?.Invoke(sender, args);
        }
    }
}