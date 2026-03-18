using Dignus.Actor.Core;
using Dignus.Actor.Core.Messages;
using Dignus.Collections;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Pipeline;
using Dignus.DependencyInjection.Extensions;
using Dignus.Framework.Pipeline;

namespace Dignus.Commands.Internals.Actors
{
    internal class CommandExecutionActor(IServiceProvider serviceProvider,
        AsyncPipeline<CommandPipelineContext> commandPipeline,
        Action onExitRequested
        ) : ActorBase
    {
        private CancellationTokenSource _cancellationToken;
        private readonly ArrayQueue<RunCommandMessage> _commandMessages = [];
        protected override async ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if (message is RunCommandMessage runCommand)
            {
                await HandleMessage(runCommand);
            }
            else if(message is CancelCommandMessage cancelCommand)
            {
                await HandleMessage(cancelCommand);
            }
            else if(message is CompleteCommandMessage completeCommand)
            {
                await HandleMessage(completeCommand);
            }

        }
        private async ValueTask HandleMessage(CompleteCommandMessage completeCommandMessage)
        {
            completeCommandMessage.PromptTargetActorRef.Post(new StartPromptMessage(), Self);

            var expiredTokenSource = Interlocked.Exchange(ref _cancellationToken, null);
            if (expiredTokenSource != null)
            {
                expiredTokenSource.Dispose();
                return;
            }

            if (_commandMessages.TryRead(out var item))
            {
                await StartCommandExecutionAsync(item.CurrentPath, item.CommandLine, false, item.Sender);
            }
        }
        private Task HandleMessage(CancelCommandMessage _)
        {
            if (_cancellationToken != null)
            {
                _cancellationToken.Cancel();
            }
            else
            {
                onExitRequested?.Invoke();
            }
            return Task.CompletedTask;
        }

        private async ValueTask HandleMessage(RunCommandMessage runCommandMessage)
        {
            _commandMessages.Add(runCommandMessage);

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
            var cancellationTokenSource = new CancellationTokenSource();

            if (Interlocked.CompareExchange(ref _cancellationToken, cancellationTokenSource, null) != null)
            {
                cancellationTokenSource.Dispose();
                return ValueTask.CompletedTask;
            }
            
            var splits = commandLine.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if(splits.Length == 0)
            {
                Post(Self, new CompleteCommandMessage(sender));
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
                Post(Self, new CompleteCommandMessage(sender));
                return;
            }

            if(currentPath.StartsWith('/'))
            {
                currentPath = currentPath.TrimStart('/');
            }

            var aliasTable = serviceProvider.GetService<AliasTable>();
            if (aliasTable.Alias.TryGetValue(commandName, out var aliasModel) == true && isAlias == false)
            {
                await ExecuteCommandAsync(currentPath, aliasModel.CommandName, args, true, sender, cancellationToken);
                return;
            }

            var commandTable = serviceProvider.GetService<CommandTable>();

            var commandType = commandTable.GetCommandType(currentPath, commandName);

            if (commandType == null)
            {
                commandType = commandTable.GetGlobalCommandType(commandName);

                if(commandType == null)
                {
                    sender.Post(new CommandResponseMessage()
                    {
                        Content = $"Command `{commandName}` was not found. Please type 'help' to see the available commands."
                    }, Self);

                    Post(Self, new CompleteCommandMessage(sender));
                    return;
                }
            }

            try
            {
                var command = (IPathCommand)serviceProvider.GetService(commandType);
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
            finally
            {
                Post(Self, new CompleteCommandMessage(sender));
            }
        }
    }
}
