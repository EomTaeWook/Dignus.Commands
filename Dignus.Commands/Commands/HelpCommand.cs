using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals;
using Dignus.Commands.Messages;
using Dignus.DependencyInjection;
using System.Text;

namespace Dignus.Commands.Commands
{
    [SystemCommand("h")]
    [SystemCommand("?")]
    [SystemCommand("help")]
    internal class HelpCommand(AliasTable aliasTable,
        CommandTable commandTable,
        ServiceContainer _serviceContainer) : IPathCommand
    {
        public Task InvokeAsync(string[] args, string currentPath, IActorRef sender, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();

            foreach (var commandName in commandTable.GetGlobalCommandList())
            {
                var command = (IPathCommand)_serviceContainer.GetService(commandName);
                sb.AppendLine($"{commandName} : {command.Print()}");
            }

            if (aliasTable.Alias.Count > 0)
            {
                sb.AppendLine();
            }

            foreach (var item in aliasTable.GetDatas())
            {
                sb.AppendLine($"{item.Alias} : {item.CommandName}");
            }

            sb.AppendLine();

            foreach (var commandName in commandTable.GetCommandListByPath(currentPath))
            {
                var command = (IPathCommand)_serviceContainer.GetService(commandName);

                string displayName = commandName;

                if (string.IsNullOrWhiteSpace(currentPath) == false)
                {
                    displayName = commandName[(currentPath.Length + 1)..];
                }
                sb.AppendLine($"{displayName} : {command.Print()}");
            }

            sender.Post(new CommandResponseMessage(sb.ToString()));

            return Task.CompletedTask;
        }

        public string Print()
        {
            return "현재 등록된 명령어를 보여줍니다.";
        }
    }
}
