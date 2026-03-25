using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Collections;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Pipeline;
using Dignus.DependencyInjection;
using Dignus.Framework.Pipeline;

namespace Dignus.Commands.Internals.Actors
{
    internal class CommandExecutionActor(ServiceContainer _serviceContainer,
        AsyncPipeline<CommandPipelineContext> commandPipeline
        ) : ActorBase
    {
        private CancellationTokenSource _cancellationToken;
        private readonly ArrayQueue<RunCommandRequestMessage> _commandMessages = [];
        protected override async ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if (message is RunCommandRequestMessage runCommand)
            {
                await HandleMessage(runCommand);
            }
            else if(message is CancelCommandMessage cancelCommand)
            {
                await HandleMessage(cancelCommand, sender);
            }
            else if (message is CommandCompletedMessage completeCommand)
            {
                await HandleMessage(completeCommand);
            }
        }
        private async ValueTask HandleMessage(CommandCompletedMessage commandCompletedMessage)
        {
            var expiredTokenSource = Interlocked.Exchange(ref _cancellationToken, null);
            if (expiredTokenSource != null)
            {
                expiredTokenSource.Dispose();
            }
            commandCompletedMessage.PromptTarget.Post(new StartPromptMessage(), Self);

            if (_commandMessages.TryRead(out var item))
            {
                await StartCommandExecutionAsync(item.CurrentPath, item.CommandLine, false, item.Sender);
            }
        }
        private Task HandleMessage(CancelCommandMessage _, IActorRef sender)
        {
            if (_cancellationToken != null)
            {
                _cancellationToken.Cancel();
            }
            else
            {
                sender.Post(new ConfirmCommandExitMessage(), Self);
            }
            return Task.CompletedTask;
        }

        private async ValueTask HandleMessage(RunCommandRequestMessage message)
        {
            _commandMessages.Add(message);

            if (_cancellationToken != null)
            {
                return;
            }

            if (_commandMessages.TryRead(out var item))
            {
                await StartCommandExecutionAsync(item.CurrentPath, item.CommandLine, false, item.Sender);
            }
        }

        private ValueTask StartCommandExecutionAsync(string currentPath, string commandLine, bool isAlias, IActorRef sender)
        {
            if(string.IsNullOrWhiteSpace(commandLine))
            {
                return ValueTask.CompletedTask;
            }

            var cancellationTokenSource = new CancellationTokenSource();

            if (Interlocked.CompareExchange(ref _cancellationToken, cancellationTokenSource, null) != null)
            {
                cancellationTokenSource.Dispose();
                return ValueTask.CompletedTask;
            }
            
            var splits = commandLine.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if(splits.Length == 0)
            {
                sender.Post(new CommandCompletedMessage(), Self);
                return ValueTask.CompletedTask;
            }

            var _ = ExecuteCommandAsync(currentPath, splits[0], splits[1..], isAlias, sender, cancellationTokenSource.Token);

            return ValueTask.CompletedTask;
        }

        private async ValueTask ExecuteCommandAsync(
            string currentPath,
            string commandName,
            string[] args,
            bool isAlias,
            IActorRef sender,
            CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(commandName))
            {
                Post(Self, new CommandCompletedMessage(sender));
                return;
            }

            if(currentPath.StartsWith('/'))
            {
                currentPath = currentPath.TrimStart('/');
            }

            var aliasTable = _serviceContainer.GetService<AliasTable>();
            if (aliasTable.Alias.TryGetValue(commandName, out var aliasModel) == true && isAlias == false)
            {
                await ExecuteCommandAsync(currentPath, aliasModel.CommandName, args, true, sender, cancellationToken);
                return;
            }

            var commandTable = _serviceContainer.GetService<CommandTable>();

            var resolvedCommandName = commandTable.GetCommand(currentPath, commandName);

            if (resolvedCommandName == null)
            {
                var error = $"Command `{commandName}` was not found. Please type 'help' to see the available commands.";
                sender.Post(new CommandResponseMessage(error, true), Self);

                Post(Self, new CommandCompletedMessage(sender));
                return;
            }

            try
            {
                var command = (IPathCommand)_serviceContainer.GetService(resolvedCommandName);
                var context = new CommandPipelineContext()
                {
                    CancellationToken = cancellationToken,
                    Command = command,
                    CurrentPath = currentPath,
                    CommandArguments = args,
                    SenderActorRef = sender
                };

               await commandPipeline.InvokeAsync(ref context);
            }
            catch (OperationCanceledException)
            {
            }
            catch(Exception ex)
            {
                sender.Post(new CommandResponseMessage(ex.Message, true), Self);
            }
            finally
            {
                Post(Self, new CommandCompletedMessage(sender));
            }
        }
    }
}
