using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSValueWildcard;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsCommon.Remove,
        "RedisSession",
        RemotingCapability = RemotingCapability.OwnedByCommand,
        DefaultParameterSetName = "Name",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Medium
    )]
    public sealed class RemoveRedisSessionCommand : RedisCmdletBase
    {
        [Parameter(ParameterSetName = "Name", Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(RedisSessionArgumentCompleter))]
        [SupportsWildcards]
        public string[] Name { get; set; } = Array.Empty<string>();

        [Parameter(
            ParameterSetName = "InstanceId",
            ValueFromPipelineByPropertyName = true,
            Mandatory = true
        )]
        [ArgumentCompleter(typeof(RedisSessionArgumentCompleter))]
        public Guid[] InstanceId { get; set; } = Array.Empty<Guid>();

        [Parameter(ParameterSetName = "Session", Mandatory = true)]
        public RedisSession[] Session { get; set; } = Array.Empty<RedisSession>();

        protected override void ProcessRecord()
        {
            var sessions = GetRedisSessionCollection();

            RemoveSessionsById(sessions);

            RemoveSessionsByName(sessions);

            RemoveSessions(sessions);
        }

        private void RemoveSessions(RedisSessionCollection sessions)
        {
            var collection = GetRedisSessionCollection();

            foreach (var session in Session)
            {
                if (collection.TryGetSession(session.InstanceId, out var foundSession))
                {
                    RemoveSession(sessions, foundSession);
                }
                else
                {
                    var error = ErrorFactory.SessionRemoved(session);
                    WriteError(error);
                }
            }
        }

        private void RemoveSessionsByName(RedisSessionCollection sessions)
        {
            var remainingSessions = sessions.GetSessions().ToList();

            foreach (var name in Name)
            {
                var found = TryRemoveSessionsMatchingName(sessions, remainingSessions, name);

                if (!found)
                {
                    ReportNameNotFound(name);
                }
            }
        }

        private void RemoveSessionsById(RedisSessionCollection sessions)
        {
            foreach (var id in InstanceId)
            {
                if (sessions.TryGetSession(id, out var session))
                {
                    RemoveSession(sessions, session);
                }
                else
                {
                    var error = ErrorFactory.SessionNotFoundById(id);
                    WriteError(error);
                }
            }
        }

        private void ReportNameNotFound(string name)
        {
            if (WildcardPattern.ContainsWildcardCharacters(name))
            {
                WriteDebug($"No sessions matched the wildcard pattern '{name}'.");
            }
            else
            {
                var exn = new ItemNotFoundException("The specified session was not found.");
                var error = new ErrorRecord(
                    exn,
                    "SessionNotFound",
                    ErrorCategory.ObjectNotFound,
                    name
                );
                WriteError(error);
            }
        }

        private bool TryRemoveSessionsMatchingName(
            RedisSessionCollection sessions,
            IEnumerable<RedisSession> remainingSessions,
            string name
        )
        {
            var found = false;

            foreach (var session in remainingSessions)
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

                    RemoveSession(sessions, session);
                }
            }

            return found;
        }

        private void RemoveSession(RedisSessionCollection sessions, RedisSession session)
        {
            if (ReferenceEquals(sessions.DefaultSession, session))
            {
                var er = ErrorFactory.DefaultSessionCannotBeRemoved(sessions.DefaultSession);
                WriteError(er);
                return;
            }
            else if (ShouldProcess(session))
            {
                sessions.RemoveSession(session.InstanceId);
            }
        }

        private bool ShouldProcess(RedisSession session)
        {
            return ShouldProcess(
                $"Removing Redis session with id {session.InstanceId} ({session.Name}).",
                $"Remove Redis session with id {session.InstanceId} ({session.Name})?",
                "Remove Redis session"
            );
        }
    }
}
