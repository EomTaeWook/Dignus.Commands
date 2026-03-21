using Dignus.Actor.Core.DeadLetter;
using Dignus.Actor.Network;
using Dignus.Commands.Internals;
using Dignus.Commands.Internals.Actors;
using Dignus.Commands.Internals.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Network;
using Dignus.Commands.Pipeline;
using Dignus.DependencyInjection.Extensions;
using Dignus.Framework.Pipeline;
using Dignus.Framework.Pipeline.Interfaces;

namespace Dignus.Commands
{
    public class TelnetCommandRunner(string moduleName = null, int port = 23) : 
        CommandModuleBase(moduleName), 
        ITelnetServerEventHandler
    {
        public event Action<DeadLetterMessage> DeadLetterMessageReceived;
        public event Action<INetworkSessionRef> SessionConnected;
        public event Action<INetworkSessionRef> SessionDisconnected;

        private TelnetServer _telnetServer;
        private readonly int _port = port;

        private readonly AsyncPipeline<CommandPipelineContext> _commandPipeline = new();
        private IServiceProvider _serviceProvider;
        public void Build()
        {
            _serviceContainer.RegisterType(_commandPipeline);
            _commandPipeline.Use(new CommandExecutionMiddleware());

            _serviceProvider = BuildInternal();
            

            _telnetServer = new TelnetServer(this);
        }
        public void AddMiddleware(IAsyncMiddleware<CommandPipelineContext> middlewareInstance)
        {
            _commandPipeline.Use(middlewareInstance);
        }
        public void AddMiddleware(AsyncPipelineDelegate<CommandPipelineContext> middleware)
        {
            _commandPipeline.Use(middleware);
        }
        public void Run()
        {
            if(_telnetServer == null)
            {
                throw new InvalidOperationException("the TelnetServer instance is null. Call the Build method first.");
            }
            _telnetServer.Start(_port);
        }

        public void Close()
        {
            _telnetServer.Close();
        }

        public void OnAccepted(INetworkSessionRef connectedActorRef)
        {
            // 텔넷 협상을 위한 Interpret As Command (IAC) 바이트 정의
            byte interpretAsCommand = 0xFF; // IAC
            byte willCommand = 0xFB;        // WILL
            byte echoOption = 0x01;         // ECHO
            byte suppressGoAheadOption = 0x03; // SUPPRESS GO AHEAD

            byte[] telnetNegotiation =
            [
                interpretAsCommand, willCommand, echoOption,
                interpretAsCommand, willCommand, suppressGoAheadOption
            ];

            connectedActorRef.SendAsync(telnetNegotiation);            

            connectedActorRef.Post(new StartPromptMessage());

            SessionConnected?.Invoke(connectedActorRef);
        }

        void ITelnetServerEventHandler.OnDisconnected(INetworkSessionRef disconnectedActorRef)
        {
            disconnectedActorRef.Post(new CancelCommandMessage());
            SessionDisconnected?.Invoke(disconnectedActorRef);
        }

        void ITelnetServerEventHandler.OnDeadLetterMessage(DeadLetterMessage deadLetterMessage)
        {
            DeadLetterMessageReceived?.Invoke(deadLetterMessage);
        }

        TelnetClientActor ITelnetServerEventHandler.CreateSessionActor()
        {
            var executionActorRef = CommandActorSystem.Instance.Spawn(() =>
            {
                var actor = _serviceProvider.GetService<CommandExecutionActor>();
                return actor;
            });

            return new TelnetClientActor(executionActorRef,
                GetModuleName());
        }
    }
}