using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;

namespace Dignus.Commands.Commands
{
    [SystemCommand("clr")]
    internal class ConsoleClearCommand : ICommand
    {
        public Task InvokeAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
        {
            Console.Clear();
            return Task.CompletedTask;
        }

        public string Print()
        {
            return "console 창을 지웁니다.";
        }
    }
}
