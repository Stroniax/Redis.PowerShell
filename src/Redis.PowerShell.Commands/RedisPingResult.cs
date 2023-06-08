using System;

namespace Redis.PowerShell
{
    public sealed class RedisPingResult
    {
        public RedisPingResult(RedisSession session, TimeSpan roundtripTime)
        {
            Session = session;
            RoundtripTime = roundtripTime;
        }

        internal RedisPingResult(RedisSession session, Exception? exception)
        {
            Session = session;
            Exception = exception;
        }

        public RedisSession Session { get; }
        public TimeSpan RoundtripTime { get; }
        internal Exception? Exception { get; }
    }
}
