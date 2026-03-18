using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals;
using Dignus.Commands.Messages;
using System.Text;
using System.Text.Json;

namespace Dignus.Commands.Commands
{
    [GlobalCommand("alias")]
    internal class AliasCommand(AliasTable aliasTable) : ICommand
    {
        public Task InvokeAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                foreach (var item in aliasTable.GetDatas())
                {
                    sb.AppendLine($"{item.Alias} : {item.CommandName}");
                }
                sender.Post(new CommandResponseMessage()
                {
                    Content = sb.ToString()
                });
                return Task.CompletedTask;
            }
            else
            {
                var aliasCmd = args[0];
                var cmd = string.Join(' ', args, 1, args.Length - 1);

                if (aliasCmd.Equals(cmd) == true)
                {
                    throw new Exception($"the alias and command are the same.");
                }

                if (string.IsNullOrEmpty(cmd))
                {
                    throw new Exception($"command is empty!");
                }

                aliasTable.AddAlias(new AliasModel()
                {
                    Alias = aliasCmd,
                    CommandName = cmd
                });

                var datas = aliasTable.GetDatas();
                var json = JsonSerializer.Serialize(datas);
                File.WriteAllText(AliasTable.Path, json);

                return Task.CompletedTask;
            }
        }

        public string Print()
        {
            return $"명령어의 별칭을 설정합니다. alias sp start process";
        }
    }
}
