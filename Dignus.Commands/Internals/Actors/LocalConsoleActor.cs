using Dignus.Actor.Core;
using Dignus.Actor.Core.Messages;
using Dignus.Commands.Messages;

namespace Dignus.Commands.Internals.Actors
{
    internal class LocalConsoleActor(IActorRef commandExecutionActorRef, string moduleName) : ActorBase
    {
        private string _currentPath = "/";
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
            return ValueTask.CompletedTask;
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }
        private void ShowPrompt()
        {
            Console.Write($"{moduleName}:{_currentPath}> ");
            Task.Run(() => 
            {
                var line = Console.ReadLine();
                var message = new RunCommandMessage(_currentPath, line, Self);
                commandExecutionActorRef.Post(message, Self);
            });
        }
    }
}
