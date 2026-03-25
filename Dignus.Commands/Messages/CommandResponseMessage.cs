using Dignus.Actor.Network.Messages;

namespace Dignus.Commands.Messages
{
    public record CommandResponseMessage(string Content, bool AppendNewline = true) : INetworkActorMessage;
}
