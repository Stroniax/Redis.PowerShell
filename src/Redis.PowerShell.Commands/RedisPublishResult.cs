using System;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    public sealed class RedisPublishResult
    {
        public RedisPublishResult(RedisChannel channel, RedisValue value, Guid sessionInstanceId, long subscriberCount)
        {
            Channel = channel;
            Value = value;
            SessionInstanceId = sessionInstanceId;
            SubscriberCount = subscriberCount;
        }

        public RedisChannel Channel { get; }
        public RedisValue Value { get; }
        public Guid SessionInstanceId { get; }
        public long SubscriberCount { get; }

        public override bool Equals(object? obj)
        {
            return obj is RedisPublishResult result &&
                Channel.Equals(result.Channel) &&
                Value.Equals(result.Value) &&
                SessionInstanceId.Equals(result.SessionInstanceId) &&
                SubscriberCount == result.SubscriberCount;
        }

        public override int GetHashCode()
        {
            int hashCode = 2002852865;
            hashCode = hashCode * -1521134295 + Channel.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            hashCode = hashCode * -1521134295 + SessionInstanceId.GetHashCode();
            hashCode = hashCode * -1521134295 + SubscriberCount.GetHashCode();
            return hashCode;
        }
    }
}