using Dignus.Actor.Core;
using Dignus.Actor.Core.Messages;
using Dignus.Actor.Network;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Decoders;
using Dignus.Commands.Network.Messages;
using System.Text;

namespace Dignus.Commands.Internals.Actors
{
    internal class TelnetClientActor(IActorRef commandExecutionActorRef,
        string moduleName) : SessionActorBase
    {
        private string _currentPath = "/";
        private readonly TelnetProtocolDecoder _protocolDecoder = new();
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
            else if(message is OutgoingNetworkMessage outgoingNetworkMessage)
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
            var bytes = Encoding.UTF8.GetBytes($"{moduleName}:{_currentPath} > ");
            NetworkSession.SendAsync(bytes);
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }
        private void HandleInput(IncomingNetworkMessage message)
        {
            _protocolDecoder.DecodeIncomingNetworkBytes(message.Bytes,
                HandleValidCharacter);
        }

        private void HandleValidCharacter(char character)
        {
            ControlCharacter controlCharacter = (ControlCharacter)character;

            switch (controlCharacter)
            {
                case ControlCharacter.Backspace:
                case ControlCharacter.Delete:
                    {
                        if (_protocolDecoder.IsBufferEmpty == false)
                       {
                            _protocolDecoder.RemoveLastCharacterFromBuffer();
                            NetworkSession.SendAsync(TelnetProtocolDecoder.BackspaceEraseSequence);
                        }
                        return;
                    }

                case ControlCharacter.CarriageReturn:
                    return;

                case ControlCharacter.LineFeed:
                    {
                        string commandLine = _protocolDecoder.GetFinalCommandAndClearBuffer();

                        if (string.IsNullOrWhiteSpace(commandLine) == false)
                        {
                            Post(Self, new CommandResponseMessage());
                            commandExecutionActorRef.Post(new RunCommandMessage(_currentPath, commandLine, Self), Self);
                        }
                        return;
                    }

                case ControlCharacter.EndOfText:
                    {
                        commandExecutionActorRef.Post(new CancelCommandMessage(), Self);
                        return;
                    }
            }

            _protocolDecoder.AppendCharacterToBuffer(character);
            NetworkSession.SendAsync([(byte)character]);
        }
    }
}
