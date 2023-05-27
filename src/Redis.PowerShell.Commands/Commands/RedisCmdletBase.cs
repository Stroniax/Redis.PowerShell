using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Redis.PowerShell.Commands
{
    public abstract class RedisCmdletBase : PSCmdlet, IDisposable
    {
        protected override void BeginProcessing()
        {
            var sessionState = SessionState;
            ArgumentCompleterUtility.SessionState = SessionState;
            ArgumentCompleterUtility.Redis = GetRedisSessionCollection();
            base.BeginProcessing();
        }

        /// <summary>
        /// Connects to a Redis server using the specified configuration options. If the connection
        /// fails, an error is written to the error stream and the method returns false.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        protected bool TryConnect(ConfigurationOptions configuration, out RedisSession session)
        {
            try
            {
                var multiplexer = ConnectionMultiplexer.Connect(configuration);
                session = new RedisSession(multiplexer);

                return true;
            }
            catch (RedisConnectionException e)
            {
                var error = ErrorFactory.SessionConnectionFailure(e, configuration);
                WriteError(error);

                session = null!;
                return false;
            }
        }

        /// <summary>
        /// Gets the "RedisSession" variable from the current session state. If the variable
        /// does not exist, it is created and added to the session state.
        /// </summary>
        /// <returns></returns>
        private PSVariable GetOrCreateRedisSessionVariable()
        {
            var redisSessionVariable = SessionState.PSVariable.Get("RedisSession");

            if (redisSessionVariable is null)
            {
                var collection = new RedisSessionCollection();
                redisSessionVariable = new PSVariable(
                    name: "RedisSession",
                    value: collection,
                    options: ScopedItemOptions.Private,
                    attributes: null
                );
                SessionState.PSVariable.Set(redisSessionVariable);
            }

            return redisSessionVariable;
        }

        /// <summary>
        /// Gets the <see cref="RedisSessionCollection"/> from the current session state.
        /// </summary>
        /// <returns></returns>
        protected RedisSessionCollection GetRedisSessionCollection()
        {
            var variable = GetOrCreateRedisSessionVariable();
            return (RedisSessionCollection)variable.Value;
        }

        /// <summary>
        /// Override this method to dispose of any unmanaged resources created during the
        /// execution of this cmdlet.
        /// </summary>
        /// <param name="dispsoing"></param>
        protected virtual void Dispose(bool dispsoing) { }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
