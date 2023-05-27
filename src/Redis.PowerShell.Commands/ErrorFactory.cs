using System;
using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    internal static class ErrorFactory
    {
        public static ErrorRecord SessionNotFoundById(Guid instanceId)
        {
            var exn = new ItemNotFoundException("No session with the specified id was found.");
            var er = new ErrorRecord(
                exn,
                "InstanceIdNotFound",
                ErrorCategory.ObjectNotFound,
                instanceId
            );
            er.ErrorDetails = new ErrorDetails($"No session with the id '{instanceId}' was found.");

            return er;
        }

        public static ErrorRecord SessionNotFoundByName(string name)
        {
            var exn = new ItemNotFoundException("No session with the specified name was found.");
            var er = new ErrorRecord(
                exn,
                "SessionNameNotFound",
                ErrorCategory.ObjectNotFound,
                name
            );
            er.ErrorDetails = new ErrorDetails($"No session with the name '{name}' was found.");

            return er;
        }

        public static ErrorRecord SessionConnectionFailure(
            RedisConnectionException exception,
            ConfigurationOptions configuration
        )
        {
            var er = new ErrorRecord(
                exception,
                "RedisConnectionFailure",
                ErrorCategory.ConnectionError,
                configuration
            );
            er.ErrorDetails = new ErrorDetails(
                $"Failed to connect to the Redis server at '{configuration.EndPoints[0]}'."
            );

            return er;
        }

        internal static ErrorRecord SessionRemoved(RedisSession session)
        {
            var exn = new ItemNotFoundException("The specified session no longer exists.");
            var er = new ErrorRecord(exn, "SessionRemoved", ErrorCategory.ObjectNotFound, session);
            er.ErrorDetails = new ErrorDetails($"The session '{session.Name}' no longer exists.");

            return er;
        }

        internal static ErrorRecord DefaultSessionCannotBeRemoved(RedisSession defaultSession)
        {
            var exn = new InvalidOperationException(
                "The default Redis session cannot be removed until it is no longer default."
            );
            var er = new ErrorRecord(
                exn,
                "DefaultSessionCannotBeRemoved",
                ErrorCategory.InvalidOperation,
                defaultSession
            );

            er.ErrorDetails = new ErrorDetails(
                $"The default session '{defaultSession.Name}' cannot be removed until it is no longer the default session."
            );
            er.ErrorDetails.RecommendedAction =
                "Use the Set-RedisDefaultSession cmdlet to change the default session.";

            return er;
        }

        internal static ErrorRecord RedisKeyUnknownType(IDatabase db, RedisKey key)
        {
            var exn = new InvalidOperationException(
                "Redis key is of an unknown type and cannot be interpreted."
            );

            var er = new ErrorRecord(
                exn,
                "RedisKeyUnknownType",
                ErrorCategory.InvalidOperation,
                new { database = db, key }
            );

            er.ErrorDetails = new ErrorDetails(
                $"The Redis key {key} on database {db} is of an unknown type and cannot be interpreted."
            );

            return er;
        }
    }
}
