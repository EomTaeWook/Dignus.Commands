using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Messages;

namespace Dignus.Commands.Commands
{
    [SystemCommand("cd")]
    internal class ChangeDirectoryCommand : ICommand
    {
        public Task InvokeAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
        {
            string path = args.Length == 0 ? "/" : args[0];

            sender.Post(new ChangeDirectoryRequestMessage 
            {
                Path = path
            });

            return Task.CompletedTask;
        }

        public string Print()
        {
            return "현재 명령 경로를 변경합니다";
        }
    }
}
