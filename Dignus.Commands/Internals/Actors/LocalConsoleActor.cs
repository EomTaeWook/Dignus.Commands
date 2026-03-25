using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Commands.Messages;

namespace Dignus.Commands.Internals.Actors
{
    internal class LocalConsoleActor(IActorRef commandExecutionActorRef) : ActorBase
    {
        private string _currentPath = "/";
        private Action _exitRequested;
        private string _moduleName;
        protected override ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if(message is CommandResponseMessage commandResponse)
            {
                Console.WriteLine(commandResponse.Content);
            }
            else if(message is StartPromptMessage)
            {
                ShowPrompt();
            }
            else if(message is CancelCommandMessage)
            {
                commandExecutionActorRef.Post(message, Self);
            }
            else if (message is ChangeDirectoryRequestMessage changeDirectoryRequestMessage)
            {
                HandleDirectoryChanged(changeDirectoryRequestMessage);
            }
            else if(message is ConfirmCommandExitMessage)
            {
                _exitRequested?.Invoke();
            }
            return ValueTask.CompletedTask;
        }
        public void Initialize(string moduleName, Action exitRequested)
        {
            _moduleName = moduleName;
            _exitRequested = exitRequested;
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }
        private void ShowPrompt()
        {
            Console.Write($"{_moduleName}:{_currentPath}> ");
            Task.Run(() => 
            {
                var line = Console.ReadLine();
                var message = new RunCommandRequestMessage(_currentPath, line, Self);
                commandExecutionActorRef.Post(message, Self);
            });
        }
    }
}
