using Dignus.Actor.Abstractions;

namespace Dignus.Commands.Network.Messages
{
    internal struct IncomingNetworkMessage : IActorMessage
    {
        public byte[] Bytes { get; set; }
    }
}
