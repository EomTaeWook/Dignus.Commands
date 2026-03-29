using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals;
using System.Text.Json;

namespace Dignus.Commands.Commands
{
    [SystemCommand("unalias")]
    internal class UnAliasCommand(AliasTable aliasTable) : ICommand
    {
        public Task InvokeAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
        {
            foreach (var item in args)
            {
                aliasTable.RemoveAlias(item);
            }
            var datas = aliasTable.GetDatas();
            var json = JsonSerializer.Serialize(datas);
            File.WriteAllText(AliasTable.Path, json);
            return Task.CompletedTask;
        }

        public string Print()
        {
            return "Removes the specified command aliases.";
        }
    }
}
