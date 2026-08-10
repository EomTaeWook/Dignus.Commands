# Dignus.Commands

[![NuGet](https://img.shields.io/nuget/v/Dignus.Commands.svg)](https://www.nuget.org/packages/Dignus.Commands)

Lightweight command execution system for **local console** and **telnet remote control**.

간단한 **로컬 콘솔 명령 실행**과 **텔넷 기반 원격 명령 실행**을 지원하는 커맨드 시스템입니다.

![picture](/Resource/Output.gif)

---

# Features / 특징

### English
- Attribute based command registration
- Delegate based command registration
- Async command execution
- `CancellationToken` based cancellation
- Middleware pipeline support
- Local console command runner
- Telnet remote command runner
- Tab command auto-completion

### Korean
- Attribute 기반 커맨드 등록
- Delegate 기반 커맨드 등록
- 비동기 커맨드 실행
- `CancellationToken` 기반 취소 지원
- 미들웨어 파이프라인 지원
- 로컬 콘솔 실행 지원
- 텔넷 기반 원격 명령 실행
- Tab 키 명령 자동완성

---

# Quick Start

```csharp
using Dignus.Commands;

var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```

---

# Local Command Runner

Run commands directly from the local console.

로컬 콘솔에서 직접 명령을 입력받아 실행합니다.

Type part of a command and press `Tab` to complete it. When several commands match,
`Tab` completes their shared prefix; press it again to view the candidates.

명령 일부를 입력한 뒤 `Tab`을 누르면 자동완성됩니다. 여러 명령이 일치하면 공통 접두어까지
완성하고, 한 번 더 누르면 후보를 표시합니다.

```csharp
using Dignus.Commands;

var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```

---

# Telnet Command Runner

Start a telnet server and execute commands remotely.

텔넷 서버를 열고 외부 텔넷 클라이언트에서 접속하여 명령을 실행합니다.

```csharp
using Dignus.Commands;

var module = new TelnetCommandRunner(port: 50000);

module.AddCommand<Close>();

module.Build();
module.Run();
```

Connect via telnet:

```
telnet 127.0.0.1 50000
```

Note

> There is **no dedicated telnet client module**.  
> You connect using any telnet client.

---

# Command Class

Create a command by implementing `ICommand`.

```csharp
using Dignus.Commands.Attributes;
using Dignus.Commands.Interfaces;
using Dignus.Actor.Core;
using System.Diagnostics;

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
```

Register the command

```csharp
module.AddCommand<Close>();
```

---

# Delegate Command

Commands can also be registered using delegates.

```csharp
module.AddCommand("loop", "loop example", TestAsync);

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
```

---

# Middleware

You can add middleware to the command execution pipeline.

```csharp
module.AddMiddleware((ref CommandPipelineContext context, ref AsyncPipelineNext<CommandPipelineContext> next) =>
{
    var auth = context.Command.GetType().GetCustomAttribute<AuthAttribute>();

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
```

---

# Command Execution Flow

```
User Input
     │
     ▼
Command Parser
     │
     ▼
Middleware Pipeline
     │
     ▼
Command Execution
     │
     ▼
CommandResponseMessage
```

---

# Cancellation

Long running commands should support cancellation.

```csharp
while (cancellationToken.IsCancellationRequested == false)
{
    await Task.Delay(1000, cancellationToken);
}
```

If a telnet connection closes, a `CancelCommandMessage` will be sent.

---

# Output Message

Send output using `CommandResponseMessage`.

```csharp
sender.Post(new CommandResponseMessage()
{
    Content = "hello"
});
```

---

# Default Commands

| Command | Description |
|-------|-------------|
| `?` | show command list |
| `Ctrl + C` | cancel running command |

---

# Module Lifecycle

```csharp
var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```

Execution order

1. Create module
2. Register commands
3. Register middleware
4. `Build()`
5. `Run()`

`Run()` must be called **after** `Build()`.

---
