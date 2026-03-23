using Dignus.Actor.Abstractions;
using Dignus.Actor.Network.Codec;
using Dignus.Collections;
using Dignus.Commands.Network.Messages;
using Dignus.Sockets;
using Dignus.Sockets.Interfaces;

namespace Dignus.Commands.Network.Codecs
{
    public class PacketDecoder : IActorMessageDecoder
    {
        public IActorMessage Deserialize(ReadOnlySpan<byte> packet)
        {
            return new IncomingNetworkMessage()
            {
                Bytes = packet.ToArray()
            };
        }

        public bool TryFrame(ISession session, ArrayQueue<byte> buffer, out ArraySegment<byte> packet, out int consumedBytes)
        {
            consumedBytes = buffer.Count;
            return buffer.TrySlice(out packet, consumedBytes);
        }
    }
}
