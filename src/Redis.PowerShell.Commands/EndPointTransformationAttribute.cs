using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;

namespace Redis.PowerShell
{
    public sealed class EndPointTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            var asEnumerable = LanguagePrimitives.GetEnumerable(inputData);
            if (asEnumerable is null)
            {
                return TransformItem(inputData);
            }

            var initialCapacity = inputData is ICollection collection ? collection.Count : 8;
            var results = new List<object>(initialCapacity);
            foreach (var item in asEnumerable)
            {
                results.Add(TransformItem(item));
            }
            return results.ToArray();
        }

        private static object TransformItem(object inputData)
        {
            return inputData switch
            {
                string s when IPAddress.TryParse(s, out var ip) => new IPEndPoint(ip, 6379),
                IPAddress ip => new IPEndPoint(ip, 6379),
                string s when TryParseEndPoint(s, out var ep) => ep,
                _ => inputData,
            };
        }

        private static bool TryParseEndPoint(string s, out EndPoint ep)
        {
            var parts = s.Split(':');
            if (parts.Length != 2)
            {
                ep = default!;
                return false;
            }

            if (!int.TryParse(parts[1], out var port))
            {
                ep = default!;
                return false;
            }

            if (IPAddress.TryParse(parts[0], out var ip))
            {
                ep = new IPEndPoint(ip, port);
                return true;
            }

            ep = new DnsEndPoint(parts[0], port);
            return true;
        }
    }
}
