// See https://aka.ms/new-console-template for more information
using Dignus.Actor.Core;
using Dignus.Commands;
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Messages;
using Dignus.Commands.Pipeline;
using Dignus.Framework.Pipeline;
using Dignus.Log;
using ServerTestConsole.Attributes;
using System.Diagnostics;
using System.Reflection;


LogBuilder.Configuration(LogConfigXmlReader.Load($"{AppContext.BaseDirectory}DignusLog.config"));
LogBuilder.Build();

var module = new TelnetCommandRunner();

module.SessionConnected += Module_SessionConnected;
module.SessionDisconnected += Module_SessionDisconnected;

void Module_SessionDisconnected(IActorRef obj)
{
    LogHelper.Info($"Module_SessionDisconnected : {obj.GetHashCode()}");
}

void Module_SessionConnected(IActorRef obj)
{
    LogHelper.Info($"Module_SessionConnected : {obj.GetHashCode()}");
}

//module.ExitRequested += Module_ExitRequested;

//void Module_ExitRequested()
//{
//    LogHelper.Info("Module_ExitRequested");
//}

module.AddCommand<Close>();

module.AddCommand("process","l", "loop desc", TestAsync);

module.AddMiddleware((ref CommandPipelineContext context, ref AsyncPipelineNext<CommandPipelineContext> next) =>
{
    var auth = context.Command.GetType().GetCustomAttribute<AuthAttribute>();
    if(auth == null)
    {
        return next.InvokeAsync(ref context);
    }
    if (auth.Execute(context) == false)
    {
        context.SenderActorRef.Post(new CommandResponseMessage("invalid auth"));
        return Task.CompletedTask;
    }
    return next.InvokeAsync(ref context);
});

module.Build();

module.Run();

while (true)
{
    await Task.Delay(33);
}

async Task TestAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
{
    var count = 0;
    while (cancellationToken.IsCancellationRequested == false)
    {
        sender.Post(new CommandResponseMessage($"sleep : {count++}"));

        await Task.Delay(2000, cancellationToken);
    }

    sender.Post(new CommandResponseMessage($"end sleep : {count++}"));
}

[Auth]
[Command("close")]
internal class Close : ICommand
{
    public Task InvokeAsync(string[] args, IActorRef sender, CancellationToken cancellationToken)
    {
        Process.GetCurrentProcess().Close();
        return Task.CompletedTask;
    }

    public string Print()
    {
        return "close process";
    }
}





