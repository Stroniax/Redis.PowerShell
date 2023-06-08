using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsCommon.Get,
        "RedisValue",
        DefaultParameterSetName = "Default",
        RemotingCapability = RemotingCapability.OwnedByCommand
    )]
    public sealed class GetRedisValueCommand : RedisClientCmdlet
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        [ValidateNotNullOrEmpty]
        public string[] Key { get; set; } = Array.Empty<string>();

        [Parameter]
        [ValidateCommandFlags]
        public CommandFlags CommandFlags { get; set; }

        protected override void ProcessRecord()
        {
            foreach (var session in GetDeclaredRedisSessions(out _))
            {
                var db = session.Database;

                WriteKeysForDb(session, db);
            }
        }

        private void WriteKeysForDb(RedisSession session, IDatabase db)
        {
            foreach (var (key, server) in ResolveKeys(db))
            {
                WriteKey(session, server, db, key);
            }
        }

        private void WriteKey(RedisSession session, IServer server, IDatabase db, RedisKey key)
        {
            var type = db.KeyType(key, CommandFlags);

            IEnumerable value;
            switch (type)
            {
                case RedisType.None:
                    WriteDebug($"Resolved key {key} but key type is {type}.");
                    return;
                case RedisType.String:
                    value = new[] { db.StringGet(key, CommandFlags) };
                    break;
                case RedisType.List:
                    value = db.ListRange(key, 0, -1, CommandFlags);
                    break;
                case RedisType.Set:
                    value = db.SetMembers(key, CommandFlags);
                    break;
                case RedisType.SortedSet:
                    value = db.SortedSetRangeByRank(key, 0, -1, Order.Ascending, CommandFlags);
                    break;
                case RedisType.Hash:
                    value = db.HashGetAll(key, CommandFlags);
                    break;
                case RedisType.Stream:
                    value = new[] { db.StreamInfo(key, CommandFlags) };
                    break;
                case RedisType.Unknown:
                    var error = ErrorFactory.RedisKeyUnknownType(db, key);
                    WriteError(error);
                    return;
                default:
                    throw new NotImplementedException($"Not implemented: key type {type}.");
            }

            foreach (var component in value)
            {
                var pso = RedisPSMembers.Value(
                    component,
                    key,
                    session.InstanceId,
                    server.EndPoint
                    );
                WriteObject(pso);
            }
        }

        private IEnumerable<(RedisKey, IServer)> ResolveKeys(IDatabase db)
        {
            foreach (var server in db.Multiplexer.GetServers())
            {
                foreach (var key in server.Keys(db.Database, flags: CommandFlags))
                {
                    yield return (key, server);
                }
            }
        }
    }
}
