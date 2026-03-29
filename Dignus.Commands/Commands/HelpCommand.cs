using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals;
using Dignus.Commands.Messages;
using Dignus.DependencyInjection;
using System.Reflection;
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
        private const string Newline = "\r\n";
        public Task InvokeAsync(string[] args, string currentPath, IActorRef sender, CancellationToken cancellationToken)
        {
            var displayPath = currentPath;

            if(string.IsNullOrWhiteSpace(displayPath))
            {
                displayPath = "/";
            }
            var sb = new StringBuilder();
            sb.Append("=== Global Commands ===");
            sb.Append(Newline);
            foreach (var commandName in commandTable.GetGlobalCommandList())
            {
                var command = (IPathCommand)_serviceContainer.GetService(commandName);

                if(command.GetType().GetCustomAttributes< SystemCommandAttribute>().Any())
                {
                    continue;
                }

                sb.Append($"{commandName} : {command.Print()}");
                sb.Append(Newline);
            }

            if (aliasTable.Alias.Count > 0)
            {
                sb.Append($"{Newline}=== Aliases ===");
                foreach (var item in aliasTable.GetDatas())
                {
                    sb.Append($"{item.Alias} : {item.CommandName}");
                    sb.Append(Newline);
                }
            }

            sb.Append($"{Newline}=== Directory Commands (Path: {displayPath}) ===");
            sb.Append(Newline);

            foreach (var commandName in commandTable.GetCommandListByPath(currentPath))
            {
                var command = (IPathCommand)_serviceContainer.GetService(commandName);

                string displayName = commandName;

                if (string.IsNullOrWhiteSpace(currentPath) == false)
                {
                    displayName = commandName[(currentPath.Length + 1)..];
                }

                sb.Append($"{displayName} : {command.Print()}");
                sb.Append(Newline);
            }

            sender.Post(new CommandResponseMessage(sb.ToString()));

            return Task.CompletedTask;
        }

        public string Print()
        {
            return "Displays the available commands.";
        }
    }
}
