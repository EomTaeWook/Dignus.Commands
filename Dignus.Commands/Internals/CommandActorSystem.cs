using Dignus.Actor.Core;

namespace Dignus.Commands.Internals
{
    internal static class CommandActorSystem
    {
        public static ActorSystem Instance { get => _actorSystem; }

        private static readonly ActorSystem _actorSystem;
        static CommandActorSystem()
        {
            _actorSystem = new ActorSystem();
        }        
    }
}
