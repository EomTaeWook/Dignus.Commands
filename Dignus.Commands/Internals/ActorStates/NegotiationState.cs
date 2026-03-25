using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Commands.Internals.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Messages;

namespace Dignus.Commands.Internals.ActorStates
{
    internal class NegotiationState(IStateTransitionContext context) : IStateBase
    {
        // 텔넷 협상을 위한 Interpret As Command (IAC) 바이트 정의
        const byte InterpretAsCommand = 0xFF; // IAC
        const byte WillCommand = 0xFB;        // WILL
        const byte DoCommand = 0xFD;          // DO

        const byte EchoOption = 0x01;         // ECHO
        const byte SuppressGoAheadOption = 0x03; // SUPPRESS GO AHEAD

        private static readonly byte[] TelnetNegotiation =
            [
                InterpretAsCommand, WillCommand, EchoOption,
                InterpretAsCommand, WillCommand, SuppressGoAheadOption
            ];

        private bool _isEchoAccepted;
        private bool _isSuppressGoAheadAccepted;

        public void OnEnter()
        {
            context.Post(new OutgoingByteMessage(TelnetNegotiation));
        }

        public void OnExit()
        {
        }

        public ValueTask OnHandleMessage(IActorMessage message, IActorRef sender)
        {
            if (message is IncomingNetworkMessage incomingNetwork)
            {
                HandleInput(incomingNetwork.Bytes);
            }

            return ValueTask.CompletedTask;
        }
        private void HandleInput(ReadOnlySpan<byte> bytes)
        {
            for (int index = 0; index <= bytes.Length - 3; index++)
            {
                if (bytes[index] != InterpretAsCommand)
                {
                    continue;
                }

                byte command = bytes[index + 1];
                byte option = bytes[index + 2];

                if (command == DoCommand && option == EchoOption)
                {
                    _isEchoAccepted = true;
                    index += 2;
                    continue;
                }

                if (command == DoCommand && option == SuppressGoAheadOption)
                {
                    _isSuppressGoAheadAccepted = true;
                    index += 2;
                    continue;
                }
            }

            if (_isEchoAccepted && _isSuppressGoAheadAccepted)
            {
                context.ChangeToTerminalInputState();
                context.Post(new StartPromptMessage());
            }
        }
    }
}
