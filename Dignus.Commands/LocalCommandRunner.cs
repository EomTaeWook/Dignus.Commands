using Dignus.Actor.Core;
using Dignus.Commands.Internals;
using Dignus.Commands.Internals.Actors;
using Dignus.Commands.Messages;
using Dignus.Commands.Pipeline;
using Dignus.Framework.Pipeline;
using Dignus.Framework.Pipeline.Interfaces;

namespace Dignus.Commands
{
    public class LocalCommandRunner : CommandModuleBase
    {
        private IActorRef _localConsoleActorRef;

        public event Action ExitRequested;

        private readonly AsyncPipeline<CommandPipelineContext> _commandPipeline = new();

        public LocalCommandRunner(string moduleName = null) : base(moduleName)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        public void Build()
        {
            _serviceContainer.RegisterType(_commandPipeline);
            _commandPipeline.Use(new CommandExecutionMiddleware());

            BuildInternal();

            var executionActorRef = CommandActorSystem.Instance.Spawn(() =>
            {
                var actor = _serviceContainer.GetService<CommandExecutionActor>();

                return actor;
            });

            _localConsoleActorRef = CommandActorSystem.Instance.Spawn(() =>
            {
                var actor = new LocalConsoleActor(executionActorRef);

                actor.Initialize(GetModuleName(), ExitRequested);

                return actor;
            });
        }
        public void AddMiddleware(IAsyncMiddleware<CommandPipelineContext> middlewareInstance)
        {
            _commandPipeline.Use(middlewareInstance);
        }
        public void AddMiddleware(AsyncPipelineDelegate<CommandPipelineContext> middleware)
        {
            _commandPipeline.Use(middleware);
        }
        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            var message = new CancelCommandMessage();
            _localConsoleActorRef.Post(message);
        }
        internal void RequestExit()
        {
            ExitRequested?.Invoke();
        }
        public void Run()
        {
            _localConsoleActorRef.Post(new StartPromptMessage());
        }
    }
}
