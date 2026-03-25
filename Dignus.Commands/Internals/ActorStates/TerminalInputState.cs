using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Commands.Internals.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Codecs;
using Dignus.Commands.Network.Messages;
using System.Text;

namespace Dignus.Commands.Internals.ActorStates
{
    internal class TerminalInputState(IStateTransitionContext context,
        string promptText) : IStateBase
    {
        private readonly List<string> _commandHistory = [];
        private int _historyIndex = -1;


        private readonly TelnetConsoleInputDecoder _consoleInput = new();
        public void OnEnter()
        {
            
        }

        public void OnExit()
        {
            
        }

        public ValueTask OnHandleMessage(IActorMessage message, IActorRef sender)
        {
            if (message is IncomingNetworkMessage incomingNetwork)
            {
                HandleInput(incomingNetwork);
            }
            else
            {

            }
            return ValueTask.CompletedTask;
        }
        private void HandleValidCharacter(char character)
        {
            ControlCharacter controlCharacter = (ControlCharacter)character;

            switch (controlCharacter)
            {
                case ControlCharacter.Backspace:
                case ControlCharacter.Delete:
                    {
                        if (_consoleInput.IsBufferEmpty == false)
                        {
                            _consoleInput.RemoveLastCharacterFromBuffer();
                            context.Post(new OutgoingByteMessage(TelnetControlSequence.BackspaceEraseSequence));
                        }
                        return;
                    }

                case ControlCharacter.CarriageReturn:
                    {
                        string commandLine = _consoleInput.GetFinalCommandAndClearBuffer();

                        if (string.IsNullOrWhiteSpace(commandLine) == true)
                        {
                            return;
                        }
                        context.Post(new CommandResponseMessage(string.Empty));
                        context.Post(new CommandLineInputMessage(commandLine));
                        _commandHistory.Add(commandLine);
                        _historyIndex = _commandHistory.Count - 1;
                        return;
                    }

                case ControlCharacter.LineFeed:
                    return;

                case ControlCharacter.EndOfText:
                    {
                        context.Post(new CancelCommandMessage());
                        return;
                    }
            }

            _consoleInput.AppendCharacterToBuffer(character);
            context.Post(new OutgoingByteMessage(Encoding.UTF8.GetBytes(character.ToString())));
        }
        private void HandleTerminalInputKey(TerminalInputKey inputKey)
        {
            if (_commandHistory.Count == 0)
            {
                return;
            }

            if (inputKey == TerminalInputKey.ArrowUp)
            {
                if (_historyIndex < _commandHistory.Count - 1)
                {
                    _historyIndex++;
                }

                ReplaceCurrentInputLine(_commandHistory[_historyIndex]);
                return;
            }

            if (inputKey == TerminalInputKey.ArrowDown)
            {
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                    ReplaceCurrentInputLine(_commandHistory[_historyIndex]);
                    return;
                }

                _historyIndex = -1;
                ReplaceCurrentInputLine(string.Empty);
            }
        }
        private void HandleInput(IncomingNetworkMessage message)
        {
            _consoleInput.DecodeIncomingNetworkBytes(message.Bytes,
                HandleValidCharacter,
                HandleTerminalInputKey);
        }
        private void ReplaceCurrentInputLine(string commandLine)
        {
            int previousInputLength = _consoleInput.CurrentBufferLength;
            _consoleInput.ReplaceBuffer(commandLine);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append('\r');
            stringBuilder.Append(' ', promptText.Length + previousInputLength);
            stringBuilder.Append('\r');
            stringBuilder.Append(promptText);
            stringBuilder.Append(commandLine);

            context.Post(new OutgoingByteMessage(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
        }
    }
}
