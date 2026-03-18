using Dignus.Actor.Core;
using Dignus.Actor.Core.Messages;

namespace Dignus.Commands.Messages
{
    internal struct CancelCommandMessage : IActorMessage
    {
    }
    internal struct ChangeDirectoryRequestMessage : IActorMessage
    {
        public string Path { get; set; }
    }
    internal readonly struct CompleteCommandMessage(IActorRef promptTargetActorRef) : IActorMessage
    {
        public IActorRef PromptTargetActorRef { get; } = promptTargetActorRef;
    }
    internal readonly struct RunCommandMessage(string currentPath, string commandLine, IActorRef sender) : IActorMessage
    {
        public string CommandLine { get; } = commandLine;

        public string CurrentPath { get; } = currentPath;

        public IActorRef Sender { get; } = sender;
    }
    internal struct StartPromptMessage : IActorMessage
    {
    }
}
