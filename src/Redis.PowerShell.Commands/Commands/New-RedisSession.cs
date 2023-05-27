using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using StackExchange.Redis;

namespace Redis.PowerShell.Commands
{
    [Cmdlet(
        VerbsCommon.New,
        "RedisSession",
        RemotingCapability = RemotingCapability.OwnedByCommand,
        DefaultParameterSetName = "Configuration",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.Medium
    )]
    [OutputType(typeof(RedisSession))]
    public sealed class NewRedisSessionCommand : RedisCmdletBase
    {
        [Parameter(
            ParameterSetName = "Server",
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true
        )]
        [Alias("HostName", "MachineName", "cn")]
        public string ComputerName { get; set; } = "localhost";

        [Parameter(
            ParameterSetName = "Server",
            Position = 1,
            ValueFromPipelineByPropertyName = true
        )]
        public ushort Port { get; set; } = 6379;

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UseSsl { get; set; }

        [Parameter(ParameterSetName = "Server")]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential? Credential { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(SocketManagerCompleter))]
        [SocketManagerTransformation]
        public SocketManager SocketManager { get; set; } = SocketManager.ThreadPool;

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [Alias("Name")]
        public string? ClientName { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        public string? ChannelPrefix { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [EndPointTransformation]
        public EndPoint[] EndPoints { get; set; } = Array.Empty<EndPoint>();

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [TimeSpanToMillisecondsTransformation]
        public int HeartbeatInterval { get; set; } = 1000;

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [TimeSpanToMillisecondsTransformation]
        public int ConnectTimeout { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        [TimeSpanToMillisecondsTransformation]
        public int ConnectRetry { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        public SwitchParameter AllowAdmin { get; set; }

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        public string[] TrustIssuer { get; set; } = Array.Empty<string>();

        [Parameter(ParameterSetName = "Server", ValueFromPipelineByPropertyName = true)]
        public Proxy Proxy { get; set; }

        private const string ServiceName = "Redis.PowerShell";

        [Parameter(
            ParameterSetName = "Configuration",
            Position = 0,
            ValueFromPipeline = true,
            Mandatory = true
        )]
        public ConfigurationOptions[] Configuration { get; set; } =
            Array.Empty<ConfigurationOptions>();

        protected override void ProcessRecord()
        {
            foreach (var configuration in GetConfigurations())
            {
                if (!ShouldProcess(configuration.ToString(), "Connect to Redis server"))
                {
                    continue;
                }

                if (TryConnect(configuration, out var session))
                {
                    AddRedisSession(session);

                    WriteObject(session);
                }
            }
        }

        private IEnumerable<ConfigurationOptions> GetConfigurations()
        {
            if (Configuration.Length > 0)
            {
                // Ensure that the caller does not adjust our session after it is created
                return CloneOfConfigurations();
            }

            var configuration = new ConfigurationOptions
            {
                AbortOnConnectFail = true,
                AllowAdmin = AllowAdmin,
                ClientName = ClientName,
                ConnectRetry = ConnectRetry,
                ConnectTimeout = ConnectTimeout,
                DefaultDatabase = 0,
                KeepAlive = HeartbeatInterval,
                Proxy = Proxy,
                ResolveDns = true,
                ServiceName = ServiceName,
                Ssl = UseSsl,
                SslHost = ComputerName,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                SocketManager = SocketManager
            };

            // It is pretty likely that the user will run "New-RedisSession localhost:6379" so we
            // should try to parse the ComputerName as an EndPoint first.
            if (
                !MyInvocation.BoundParameters.ContainsKey(nameof(Port))
                && ComputerName.IndexOf(':') is int portDelimiter
                && portDelimiter > -1
            )
            {
                if (ushort.TryParse(ComputerName.Substring(portDelimiter + 1), out var port))
                {
                    configuration.EndPoints.Add(
                        new DnsEndPoint(ComputerName.Substring(0, portDelimiter), port)
                    );
                }
            }

            foreach (var endPoint in EndPoints)
            {
                configuration.EndPoints.Add(endPoint);
            }

            if (Credential != null)
            {
                var networkCredential = Credential.GetNetworkCredential();
                configuration.User = networkCredential.UserName;
                configuration.Password = networkCredential.Password;
            }

            if (ChannelPrefix != null)
            {
                configuration.ChannelPrefix = ChannelPrefix;
            }

            return new[] { configuration };
        }

        private IEnumerable<ConfigurationOptions> CloneOfConfigurations()
        {
            var clonedConfigurations = new ConfigurationOptions[Configuration.Length];
            for (int i = 0; i < Configuration.Length; i++)
            {
                clonedConfigurations[i] = Configuration[i].Clone();
            }
            return clonedConfigurations;
        }

        private void AddRedisSession(RedisSession session)
        {
            var list = GetRedisSessionCollection();

            list.AddSession(session);

            if (list.DefaultSession is null)
            {
                list.DefaultSession = session;
            }
        }
    }
}
