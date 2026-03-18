using Dignus.Actor.Core;
using Dignus.Actor.Core.DeadLetter;
using Dignus.Actor.Network;
using Dignus.Actor.Network.Options;
using Dignus.Commands.Internals;
using Dignus.Commands.Internals.Actors;
using Dignus.Commands.Internals.Interfaces;
using Dignus.Commands.Network.Codec;
using Dignus.Commands.Network.Decoders;
using System.Net;

namespace Dignus.Commands.Network
{
    internal class TelnetServer
    {
        internal class InnerTelnetServer(
            ITelnetServerEventHandler telnetServerEventHandler,
            ServerOptions options) : 
            TcpServerBase<TelnetClientActor>(CommandActorSystem.Instance, options)
        {
            protected override TelnetClientActor CreateSessionActor()
            {
                return telnetServerEventHandler.CreateSessionActor();
            }

            protected override void OnAccepted(IActorRef connectedActorRef)
            {
                telnetServerEventHandler.OnAccepted(connectedActorRef);
            }

            protected override void OnDeadLetterMessage(DeadLetterMessage deadLetterMessage)
            {
                telnetServerEventHandler.OnDeadLetterMessage(deadLetterMessage);
            }

            protected override void OnDisconnected(IActorRef connectedActorRef)
            {
                telnetServerEventHandler.OnDisconnected(connectedActorRef);
            }
        }

        private readonly InnerTelnetServer _server;

        public TelnetServer(ITelnetServerEventHandler  telnetServerEventHandler)
        {
            _server = new InnerTelnetServer(
                telnetServerEventHandler,
                new ServerOptions()
            {
                Network = new ActorNetworkOptions() 
                {
                    Decoder = new PacketDecoder(),
                    MessageSerializer = new MessageSerializer(),
                }
            });
        }
        public void Start(IPEndPoint ipEndPoint, int backlog = 200)
        {
            _server.Start(ipEndPoint, backlog);

        }
        public void Start(int port, int backlog = 200)
        {
            _server.Start(port, backlog);
        }
        public void Start(string ipString, int port, int backlog = 200)
        {
            IPEndPoint ipEndPoint = new(IPAddress.Parse(ipString), port);
            Start(ipEndPoint, backlog);
        }
        public void Close()
        {
            _server.Close();
        }
        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
