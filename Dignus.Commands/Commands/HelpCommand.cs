using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals;
using Dignus.Commands.Messages;
using System.Text;

namespace Dignus.Commands.Commands
{
    [GlobalCommand("h")]
    [GlobalCommand("?")]
    [GlobalCommand("help")]
    internal class HelpCommand(AliasTable aliasTable,
        CommandTable commandTable,
        IServiceProvider serviceProvider) : IPathCommand
    {
        public Task InvokeAsync(string[] args, string currentPath, IActorRef sender, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();

            foreach (var name in commandTable.GetGlobalCommandList())
            {
                var commandType = commandTable.GetGlobalCommandType(name);
                var command = (IPathCommand)serviceProvider.GetService(commandType);
                sb.AppendLine($"{name} : {command.Print()}");
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

            foreach (var name in commandTable.GetCommandListByPath(currentPath))
            {
                var commandType = commandTable.GetCommandType(name);
                var command = (IPathCommand)serviceProvider.GetService(commandType);

                string displayName = name;

                if (string.IsNullOrWhiteSpace(currentPath) == false)
                {
                    displayName = name[(currentPath.Length + 1)..];
                }
                sb.AppendLine($"{displayName} : {command.Print()}");
            }
                        
            sender.Post(new CommandResponseMessage()
            {
                Content = sb.ToString()
            });

            return Task.CompletedTask;
        }

        public string Print()
        {
            return "현재 등록된 명령어를 보여줍니다.";
        }
    }
}
