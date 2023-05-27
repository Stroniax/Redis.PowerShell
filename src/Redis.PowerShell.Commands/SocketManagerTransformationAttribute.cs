using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    public sealed class SocketManagerTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            return inputData switch
            {
                nameof(SocketManager.Shared) => SocketManager.Shared,
                nameof(SocketManager.ThreadPool) => SocketManager.ThreadPool,
                _ => inputData
            };
        }
    }
}
