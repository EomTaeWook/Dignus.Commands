using Dignus.Actor.Abstractions;

namespace Dignus.Commands.Messages
{
    public readonly record struct CommandResponseMessage(string Content) : IActorMessage;
}
