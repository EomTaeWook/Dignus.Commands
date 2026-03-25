using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;

namespace Dignus.Commands.Internals.ActorStates
{
    internal interface IStateBase
    {
        void OnEnter();
        void OnExit();
        ValueTask OnHandleMessage(IActorMessage message, IActorRef sender);
    }
}
