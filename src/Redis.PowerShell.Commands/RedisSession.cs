using System;
using System.Collections.ObjectModel;
using System.Net;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    public sealed class RedisSession : IDisposable
    {
        private RedisSessionCollection? _parent;

        public Guid InstanceId { get; }
        public string Name => Connection.ClientName;
        public string Status => Connection.GetStatus();
        public bool IsConnected => Connection.IsConnected;
        public bool IsDefault
        {
            get => ReferenceEquals(_parent?.DefaultSession, this);
            set
            {
                if (value && _parent is null)
                {
                    throw new InvalidOperationException(
                        "Cannot make the current session default because it is not associated with a session state."
                    );
                }
                if (value)
                {
                    _parent!.DefaultSession = this;
                }
            }
        }
        public ReadOnlyCollection<EndPoint> EndPoints =>
            new ReadOnlyCollection<EndPoint>(Connection.GetEndPoints());

        private IDatabase? _database;

        internal IConnectionMultiplexer Connection { get; private set; }
        internal IDatabase Database => _database ??= Connection.GetDatabase();

        internal RedisSession(IConnectionMultiplexer connection)
        {
            InstanceId = Guid.NewGuid();
            Connection = connection;
        }

        public void Dispose()
        {
            Connection.Dispose();

            if (ReferenceEquals(_parent?.DefaultSession, this))
            {
                _parent.DefaultSession = null;
            }
            _parent?.RemoveSession(InstanceId);
            _parent = null;
        }

        internal void SetParent(RedisSessionCollection parent)
        {
            if (_parent is null)
            {
                _parent = parent;
            }
            else
            {
                throw new InvalidOperationException(
                    "This session is assigned to a different parent."
                );
            }
        }
    }
}
