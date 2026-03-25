using Dignus.Actor.Network.Codec;
using Dignus.Actor.Network.Messages;
using Dignus.Collections;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Messages;
using Dignus.Sockets.Interfaces;
using System.Text;

namespace Dignus.Commands.Network.Codecs
{
    internal class MessageSerializer : IActorMessageSerializer
    {
        public ArraySegment<byte> MakeSendBuffer(IPacket packet)
        {
            var sendPacket = packet as Packet;
            var buffer = new ArrayQueue<byte>();
            buffer.AddRange(sendPacket.Body);
            return buffer.ToArray();
        }

        public ArraySegment<byte> MakeSendBuffer(INetworkActorMessage message)
        {
            if (message is not CommandResponseMessage networkMessage)
            {
                return null;
            }

            if (networkMessage.AppendNewline)
            {
                return Encoding.UTF8.GetBytes($"{networkMessage.Content}\r\n");
            }
            else
            {
                return Encoding.UTF8.GetBytes($"{networkMessage.Content}");
            }
        }
    }
}
