using Dignus.Actor.Network.Messages;

namespace Dignus.Commands.Network.Messages
{
    public class OutgoingMessage(string content, bool appendNewline) : INetworkActorMessage
    {
        public string Content { get; init; } = content;
        public bool AppendNewline { get; init; } = appendNewline;
    }
}
