using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;

namespace Dignus.Commands.Messages
{
    internal readonly record struct StartNegotiationMessage : IActorMessage;
    internal readonly record struct CancelCommandMessage : IActorMessage;
    internal readonly record struct ChangeDirectoryRequestMessage(string Path) : IActorMessage;
    internal readonly record struct CommandCompletedMessage(IActorRef PromptTarget) : IActorMessage;
    internal readonly record struct StartPromptMessage : IActorMessage;
    internal readonly record struct ConfirmCommandExitMessage : IActorMessage;
    internal readonly record struct RunCommandRequestMessage(string CurrentPath, string CommandLine, IActorRef Sender) : IActorMessage;
    internal readonly record struct CommandLineInputMessage(string CommandLine) : IActorMessage;
}
