using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;

namespace Dignus.Commands.Internals.Interfaces
{
    internal interface IStateTransitionContext
    {
        void Post(IActorMessage message);
        void ChangeToNegotiationState();
        void ChangeToTerminalInputState();
    }
}
