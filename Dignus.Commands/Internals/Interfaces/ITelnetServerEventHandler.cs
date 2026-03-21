using Dignus.Actor.Core;
using Dignus.Actor.Core.DeadLetter;
using Dignus.Actor.Network;
using Dignus.Commands.Internals.Actors;

namespace Dignus.Commands.Internals.Interfaces
{
    internal interface ITelnetServerEventHandler
    {
        TelnetClientActor CreateSessionActor();
        void OnAccepted(INetworkSessionRef connectedActorRef);
        void OnDisconnected(INetworkSessionRef connectedActorRef);
        void OnDeadLetterMessage(DeadLetterMessage deadLetterMessage);
    }
}
