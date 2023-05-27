using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Redis.PowerShell.Commands
{
    public abstract class RedisClientCmdlet : RedisCmdletBase
    {
        public RedisClientCmdlet()
        {
            _temporarySessions = new List<RedisSession>();
        }

        private readonly List<RedisSession> _temporarySessions;

        [Parameter(ParameterSetName = "Session", Mandatory = true)]
        public RedisSession[] Session { get; set; } = Array.Empty<RedisSession>();

        [Parameter(
            ParameterSetName = "InstanceId",
            ValueFromPipelineByPropertyName = true,
            Mandatory = true
        )]
        public Guid[] InstanceId { get; set; } = Array.Empty<Guid>();

        [Parameter(
            ParameterSetName = "Default",
            ValueFromPipelineByPropertyName = true,
            Position = 98
        )]
        public string? ComputerName { get; set; }

        [Parameter(
            ParameterSetName = "Default",
            ValueFromPipelineByPropertyName = true,
            Position = 99
        )]
        public ushort Port { get; set; } = 6379;

        [Parameter(ParameterSetName = "Configuration", Mandatory = true)]
        public ConfigurationOptions[] Configuration { get; set; } =
            Array.Empty<ConfigurationOptions>();

        protected IEnumerable<RedisSession> GetDeclaredRedisSessions(out bool hadErrors)
        {
            hadErrors = false;

            if (_temporarySessions.Count > 0)
            {
                // We have already created the sessions. It is not possible for the user to define
                // sessions to create at call time AND provide cached sessions, so we can skip
                // just return all the cached sessions.
                return _temporarySessions;
            }
            else if (InstanceId.Length > 0)
            {
                return GetSessionsById(out hadErrors);
            }
            else if (Session.Length > 0)
            {
                return Session;
            }
            else if (TryCreateSessionsFromConfiguration(out var sessions, out hadErrors))
            {
                return sessions;
            }
            else if (TryGetDefaultRedisSession(out var defaultSession))
            {
                return new[] { defaultSession };
            }
            else
            {
                hadErrors = true;
                return Array.Empty<RedisSession>();
            }
        }

        private bool TryCreateSessionsFromConfiguration(
            out List<RedisSession> sessions,
            out bool hadErrors
        )
        {
            hadErrors = false;
            var hadConnections = false;
            foreach (var configuration in GetConfigurationOptions())
            {
                hadConnections = true;
                if (TryConnect(configuration, out var session))
                {
                    _temporarySessions.Add(session);
                }
                else
                {
                    hadErrors = true;
                }
            }
            sessions = _temporarySessions;
            return hadConnections;
        }

        private List<RedisSession> GetSessionsById(out bool hadErrors)
        {
            var foundSessions = new List<RedisSession>();
            hadErrors = false;

            foreach (var id in InstanceId)
            {
                if (TryGetRedisSessionById(id, out var session))
                {
                    foundSessions.Add(session);
                }
                else
                {
                    hadErrors = true;
                }
            }

            return foundSessions;
        }

        private IEnumerable<ConfigurationOptions> GetConfigurationOptions()
        {
            foreach (var configuration in Configuration)
            {
                yield return configuration;
            }
            if (ComputerName != null)
            {
                yield return new ConfigurationOptions
                {
                    EndPoints = { $"{ComputerName}:{Port}" },
                    AllowAdmin = true,
                };
            }
        }

        private bool TryGetDefaultRedisSession(out RedisSession session)
        {
            var variable = GetRedisSessionCollection();

            if (variable.DefaultSession != null)
            {
                session = variable.DefaultSession;
                return true;
            }

            var configuration = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" },
                ConnectRetry = 3,
            };

            if (TryConnect(configuration, out session))
            {
                variable.DefaultSession = session;
                return true;
            }
            return false;
        }

        private bool TryGetRedisSessionById(Guid instanceId, out RedisSession session)
        {
            var list = GetRedisSessionCollection();

            if (list.TryGetSession(instanceId, out session))
            {
                return true;
            }
            var exn = new ItemNotFoundException("No session with the specified id was found.");
            var er = new ErrorRecord(
                exn,
                "SessionNotFound",
                ErrorCategory.ObjectNotFound,
                instanceId
            );
            WriteError(er);

            session = null!;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var session in _temporarySessions)
                {
                    session.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
