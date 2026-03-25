using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Actor.Network;
using Dignus.Commands.Internals.ActorStates;
using Dignus.Commands.Internals.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Network.Messages;
using System.Text;

namespace Dignus.Commands.Internals.Actors
{
    internal class TelnetClientActor(IActorRef commandExecutionActorRef,
        string moduleName) : SessionActorBase, IStateTransitionContext
    {
        private IStateBase _currentState;

        private string _currentPath = "/";
        public void ChangeState(IStateBase newState)
        {
            if(_currentState != null)
            {
                _currentState.OnExit();
            }

            _currentState = newState;

            _currentState.OnEnter();
        }
        protected override ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if(message is IncomingNetworkMessage)
            {
                return _currentState.OnHandleMessage(message, sender);
            }
            else if(message is StartNegotiationMessage)
            {
                ChangeToNegotiationState();
            }
            else if(message is CancelCommandMessage)
            {
                commandExecutionActorRef.Post(message, Self);
            }
            else if(message is CommandResponseMessage commandResponse)
            {
                NetworkSession.SendAsync(commandResponse);
            }
            else if(message is CommandLineInputMessage commandLineInput)
            {
                commandExecutionActorRef.Post(new RunCommandRequestMessage(_currentPath, commandLineInput.CommandLine, Self), Self);
            }
            else if (message is OutgoingByteMessage outgoingByteMessage)
            {
                NetworkSession.SendAsync(outgoingByteMessage.Bytes);
            }
            else if(message is ConfirmCommandExitMessage)
            {
                NetworkSession.Kill();
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
            NetworkSession.SendAsync(new CommandResponseMessage(GetPromptText(), false));
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }

        private string GetPromptText()
        {
            return $"{moduleName}:{_currentPath} > ";
        }

        public void ChangeToNegotiationState()
        {
            VerifyContext();

            ChangeState(new NegotiationState(this));
        }

        public void ChangeToTerminalInputState()
        {
            VerifyContext();

            ChangeState(new TerminalInputState(this, GetPromptText()));
        }
        public void Post(IActorMessage message)
        {
            Self.Post(message, Self);
        }
    }
}
