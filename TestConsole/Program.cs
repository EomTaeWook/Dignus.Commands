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

var module = new LocalCommandRunner();

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
        context.SenderActorRef.Post(new CommandResponseMessage()
        {
            Content = "invalid auth"
        });
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
        sender.Post(new CommandResponseMessage()
        {
            Content = $"sleep : {count++}"
        });

        await Task.Delay(2000, cancellationToken);
    }

    sender.Post(new CommandResponseMessage()
    {
        Content = $"end sleep : {count++}"
    });
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





