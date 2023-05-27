using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsCommon.Set,
        "RedisDefaultSession",
        RemotingCapability = RemotingCapability.None,
        DefaultParameterSetName = "Name",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Medium
    )]
    public sealed class SetDefaultRedisSessionCommand : RedisCmdletBase
    {
        [Parameter(ParameterSetName = "Name", Position = 0, Mandatory = true)]
        [SupportsWildcards]
        public string Name { get; set; } = string.Empty;

        [Parameter(
            ParameterSetName = "InstanceId",
            ValueFromPipelineByPropertyName = true,
            Mandatory = true
        )]
        public Guid InstanceId { get; set; }

        [Parameter(ParameterSetName = "Session", Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public RedisSession? Session { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var collection = GetRedisSessionCollection();
            RedisSession? session;
            if (Name.Length > 0)
            {
                session = ResolveSessionName(collection);
            }
            else if (InstanceId != Guid.Empty)
            {
                session = ResolveSessionId(collection);
            }
            else if (Session != null)
            {
                session = ResolveSessionObject(collection);
            }
            else if (ParameterSetName == "Session" && Session is null)
            {
                // we are setting the session to null intentionally
                RemoveDefaultSession(collection);
                return;
            }
            else
            {
                throw new NotImplementedException(
                    $"Not implemented: parameter set {ParameterSetName}."
                );
            }

            if (session == null)
            {
                return;
            }

            var verboseDescription = collection.DefaultSession is null
                ? $"Setting default session to {session.InstanceId} ({session.Name})."
                : $"Setting default session to {session.InstanceId} ({session.Name}) replacing {collection.DefaultSession.InstanceId} ({collection.DefaultSession.Name}).";
            var verboseWarning = collection.DefaultSession is null
                ? $"Set default session to {session.InstanceId} ({session.Name})?"
                : $"Set default session to {session.InstanceId} ({session.Name}) replacing {collection.DefaultSession.InstanceId} ({collection.DefaultSession.Name})?";

            if (!ShouldProcess(verboseDescription, verboseWarning, "Set default Redis session"))
            {
                return;
            }

            collection.DefaultSession = session;

            if (PassThru)
            {
                WriteObject(session);
            }
        }

        private void RemoveDefaultSession(RedisSessionCollection collection)
        {
            if (collection.DefaultSession is null)
            {
                return;
            }
            if (
                !ShouldProcess(
                    $"Removing default Redis session {collection.DefaultSession.InstanceId} ({collection.DefaultSession.Name}).",
                    $"Remove default Redis session {collection.DefaultSession.InstanceId} ({collection.DefaultSession.Name})?",
                    "Remove default Redis session"
                )
            )
            {
                return;
            }
            collection.DefaultSession = null;
        }

        private RedisSession? ResolveSessionName(RedisSessionCollection collection)
        {
            var session = collection
                .GetSessions()
                .FirstOrDefault(
                    session => string.Equals(session.Name, Name, StringComparison.OrdinalIgnoreCase)
                );

            if (session is null)
            {
                var error = ErrorFactory.SessionNotFoundByName(Name);
                WriteError(error);
                return null;
            }

            return session;
        }

        private RedisSession? ResolveSessionId(RedisSessionCollection collection)
        {
            if (!collection.TryGetSession(InstanceId, out var session))
            {
                var error = ErrorFactory.SessionNotFoundById(InstanceId);
                WriteError(error);
                return null;
            }

            return session;
        }

        private RedisSession? ResolveSessionObject(RedisSessionCollection collection)
        {
            Debug.Assert(Session != null);

            if (!collection.TryGetSession(Session!.InstanceId, out _))
            {
                var error = ErrorFactory.SessionRemoved(Session);
                WriteError(error);
                return null;
            }

            return Session;
        }
    }
}
