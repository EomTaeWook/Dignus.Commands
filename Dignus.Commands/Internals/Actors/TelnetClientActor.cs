using Dignus.Actor.Core;
using Dignus.Actor.Core.Messages;
using Dignus.Actor.Network;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Codecs;
using Dignus.Commands.Network.Messages;
using System.Text;

namespace Dignus.Commands.Internals.Actors
{
    internal class TelnetClientActor(IActorRef commandExecutionActorRef,
        string moduleName) : SessionActorBase
    {
        private string _currentPath = "/";
        private readonly TelnetConsoleInputDecoder _consoleInput = new();
        private readonly List<string> _commandHistory = [];
        private int _historyIndex = -1;
        protected override ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if(message is IncomingNetworkMessage commandLineMessage)
            {
                HandleInput(commandLineMessage);
                return ValueTask.CompletedTask;
            }
            else if(message is CancelCommandMessage)
            {
                commandExecutionActorRef.Post(message, Self);
            }
            else if(message is CommandResponseMessage commandResponse)
            {
                var bytes = Encoding.UTF8.GetBytes($"{commandResponse.Content}\r\n");

                NetworkSession.SendAsync(bytes);
            }
            else if(message is OutgoingMessage outgoingNetworkMessage)
            {
                NetworkSession.SendAsync(outgoingNetworkMessage);
            }
            else if (message is StartPromptMessage)
            {
                ShowPrompt();
            }
            else if(message is ChangeDirectoryRequestMessage changeDirectoryRequestMessage)
            {
                HandleDirectoryChanged(changeDirectoryRequestMessage);
            }

            return ValueTask.CompletedTask;
        }
        private void ShowPrompt() 
        {
            var bytes = Encoding.UTF8.GetBytes(GetPromptText());
            NetworkSession.SendAsync(bytes);
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }
        private void HandleInput(IncomingNetworkMessage message)
        {
            _consoleInput.DecodeIncomingNetworkBytes(message.Bytes,
                HandleValidCharacter,
                HandleTerminalInputKey);
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
        private void ReplaceCurrentInputLine(string commandLine)
        {
            int previousInputLength = _consoleInput.CurrentBufferLength;
            _consoleInput.ReplaceBuffer(commandLine);

            var promptText = GetPromptText();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append('\r');
            stringBuilder.Append(' ', promptText.Length + previousInputLength);
            stringBuilder.Append('\r');
            stringBuilder.Append(promptText);
            stringBuilder.Append(commandLine);

            NetworkSession.SendAsync(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
        }
        private string GetPromptText()
        {
            return $"{moduleName}:{_currentPath} > ";
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
                            NetworkSession.SendAsync(TelnetConsoleInputDecoder.BackspaceEraseSequence);
                        }
                        return;
                    }

                case ControlCharacter.CarriageReturn:
                    return;

                case ControlCharacter.LineFeed:
                    {
                        string commandLine = _consoleInput.GetFinalCommandAndClearBuffer();

                        if (string.IsNullOrWhiteSpace(commandLine) == true)
                        {
                            return;
                        }

                        Post(Self, new CommandResponseMessage());
                        commandExecutionActorRef.Post(new RunCommandMessage(_currentPath, commandLine, Self), Self);
                        _commandHistory.Add(commandLine);
                        _historyIndex = _commandHistory.Count - 1;
                        return;
                    }

                case ControlCharacter.EndOfText:
                    {
                        commandExecutionActorRef.Post(new CancelCommandMessage(), Self);
                        return;
                    }
            }

            _consoleInput.AppendCharacterToBuffer(character);
            NetworkSession.SendAsync([(byte)character]);
        }
    }
}
