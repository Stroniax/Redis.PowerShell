using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Redis.PowerShell
{
    public sealed class RedisSessionCollection
    {
        private RedisSession? _defaultSession;
        private readonly ConcurrentDictionary<Guid, RedisSession> _sessions;
        internal RedisSession? DefaultSession
        {
            get => _defaultSession;
            set
            {
                _defaultSession = value;
                if (value != null && !_sessions.ContainsKey(value.InstanceId))
                {
                    _sessions.TryAdd(value.InstanceId, value);
                }
            }
        }

        public IEnumerable<RedisSession> GetSessions() => _sessions.Values;

        public RedisSessionCollection()
        {
            _sessions = new ConcurrentDictionary<Guid, RedisSession>();
        }

        public bool AddSession(RedisSession session)
        {
            session.SetParent(this);
            return _sessions.TryAdd(session.InstanceId, session);
        }

        public bool RemoveSession(Guid instanceId)
        {
            if (_sessions.TryRemove(instanceId, out var session))
            {
                if (DefaultSession == session)
                {
                    DefaultSession = null;
                }

                return true;
            }

            session.Dispose();
            return true;
        }

        public bool TryGetSession(Guid instanceId, out RedisSession session)
        {
            return _sessions.TryGetValue(instanceId, out session);
        }
    }
}
