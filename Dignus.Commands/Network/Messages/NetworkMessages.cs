using Dignus.Actor.Abstractions;
using Dignus.Actor.Network.Messages;

namespace Dignus.Commands.Network.Messages
{
    internal readonly record struct IncomingNetworkMessage(byte[] Bytes) : IActorMessage;

    internal readonly record struct OutgoingMessage(string Content, bool AppendNewline) : INetworkActorMessage;
}
