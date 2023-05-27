using System;
using System.Management.Automation;
using PSValueWildcard;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsCommon.Get,
        "RedisSession",
        RemotingCapability = RemotingCapability.OwnedByCommand,
        DefaultParameterSetName = "Name"
    )]
    public sealed class GetRedisSessionCommand : RedisCmdletBase
    {
        [Parameter(ParameterSetName = "Name", Position = 0)]
        [ArgumentCompleter(typeof(RedisSessionArgumentCompleter))]
        [SupportsWildcards]
        public string[] Name { get; set; } = Array.Empty<string>();

        [Parameter(ParameterSetName = "InstanceId", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(RedisSessionArgumentCompleter))]
        public Guid[] InstanceId { get; set; } = Array.Empty<Guid>();

        [Parameter(ParameterSetName = "DefaultSessionSwitch")]
        public SwitchParameter DefaultSession { get; set; }

        protected override void ProcessRecord()
        {
            if (Name.Length > 0)
            {
                WriteSessionsByName();
            }
            else if (InstanceId.Length > 0)
            {
                WriteSessionsById();
            }
            else if (DefaultSession)
            {
                WriteDefaultSession();
            }
            else
            {
                WriteAllSessions();
            }
        }

        private void WriteDefaultSession()
        {
            var collection = GetRedisSessionCollection();

            var defaultSession = collection.DefaultSession;

            if (defaultSession is null)
            {
                return;
            }

            WriteObject(defaultSession);
        }

        private void WriteAllSessions()
        {
            var collection = GetRedisSessionCollection();

            var sessions = collection.GetSessions();

            WriteObject(sessions, true);
        }

        private void WriteSessionsById()
        {
            var collection = GetRedisSessionCollection();

            foreach (var id in InstanceId)
            {
                if (collection.TryGetSession(id, out var session))
                {
                    WriteObject(session);
                }
                else
                {
                    var error = ErrorFactory.SessionNotFoundById(id);
                    WriteError(error);
                }
            }
        }

        private void WriteSessionsByName()
        {
            var colleciton = GetRedisSessionCollection();

            foreach (var name in Name)
            {
                var found = false;

                foreach (var session in colleciton.GetSessions())
                {
                    if (
                        ValueWildcardPattern.IsMatch(
                            session.Name,
                            name,
                            ValueWildcardOptions.InvariantIgnoreCase
                        )
                    )
                    {
                        found = true;
                        WriteObject(session);
                    }
                }

                if (!found)
                {
                    if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        WriteDebug($"No sessions matched the wildcard pattern '{name}'.");
                    }
                    else
                    {
                        var error = ErrorFactory.SessionNotFoundByName(name);
                        WriteError(error);
                    }
                }
            }
        }
    }
}
