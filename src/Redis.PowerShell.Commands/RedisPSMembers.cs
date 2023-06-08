using System;
using System.Management.Automation;
using System.Net;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    internal static class RedisPSMembers
    {
        public static PSObject Value(
            object value,
            string key,
            Guid redisSessionId,
            EndPoint redisSessionEndPoint
        )
        {
            var pso = PSObject.AsPSObject(value);
            pso.Members.Add(new PSNoteProperty("Key", key));
            pso.Members.Add(new PSNoteProperty("SessionInstanceId", redisSessionId));
            pso.Members.Add(new PSNoteProperty("SessionEndPoint", redisSessionEndPoint));

            return pso;
        }
    }
}