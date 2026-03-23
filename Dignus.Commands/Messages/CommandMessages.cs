using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;

namespace Dignus.Commands.Messages
{
    internal readonly record struct CancelCommandMessage : IActorMessage;
    internal readonly record struct ChangeDirectoryRequestMessage(string Path) : IActorMessage;
    internal readonly record struct CompleteCommandMessage(IActorRef PromptTargetActorRef) : IActorMessage;
    internal readonly record struct StartPromptMessage : IActorMessage;
    internal readonly record struct ConfirmCommandExitMessage : IActorMessage;
    internal readonly record struct RunCommandMessage(string CurrentPath, string CommandLine, IActorRef Sender) : IActorMessage;
}
