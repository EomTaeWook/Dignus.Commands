# Dignus.Commands

[![NuGet](https://img.shields.io/nuget/v/Dignus.Commands.svg)](https://www.nuget.org/packages/Dignus.Commands)

Lightweight command execution system for **local console** and **telnet remote control**.

간단한 **로컬 콘솔 명령 실행**과 **텔넷 기반 원격 명령 실행**을 지원하는 커맨드 시스템입니다.

![Dignus.Commands console demo](https://raw.githubusercontent.com/EomTaeWook/Dignus.Commands/main/Resource/Output.gif)

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

```csharp
using Dignus.Commands;

var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```

## Tab Completion / Tab 자동완성

Type part of a command and press `Tab` to complete it. Command completion is path-aware:
from the root it completes the full command path, while inside a command directory it
completes the path relative to the current location.

명령 일부를 입력한 뒤 `Tab`을 누르면 자동완성됩니다. 루트에서는 전체 명령 경로를,
하위 경로에서는 현재 위치를 기준으로 한 상대 명령 경로를 완성합니다.

For a command registered as `debug/process/l`:

`debug/process/l`로 등록된 명령이 있다면 다음과 같이 동작합니다.

```text
ServerTestConsole:/> de<Tab>
ServerTestConsole:/> debug/process/l

ServerTestConsole:/> cd de<Tab>
ServerTestConsole:/> cd debug

ServerTestConsole:/debug> pro<Tab>
ServerTestConsole:/debug> process/l

ServerTestConsole:/debug/process> <Tab>
l
ServerTestConsole:/debug/process>
```

Pressing `Tab` with no input lists the commands available at the current path. Global
commands and aliases appear as completion candidates only at the root, although global
commands remain executable from every path.

아무것도 입력하지 않고 `Tab`을 누르면 현재 경로에서 사용할 수 있는 명령을 표시합니다.
전역 명령과 별칭은 루트에서만 자동완성 후보로 표시되지만, 전역 명령 자체는 모든 경로에서
실행할 수 있습니다.

When several candidates match, the first `Tab` completes their shared prefix. Press
`Tab` again to display all matching candidates.

여러 후보가 일치하면 첫 번째 `Tab`에서 공통 접두어까지 완성하고, 다시 `Tab`을 누르면
일치하는 후보를 모두 표시합니다.

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
| `?`, `h`, `help` | Show the available commands / 사용 가능한 명령 표시 |
| `cd [path]` | Change the current command path / 현재 명령 경로 변경 |
| `alias [name] [command]` | Create or list aliases / 별칭 생성 또는 목록 표시 |
| `unalias [name]` | Remove an alias / 별칭 제거 |
| `Tab` | Complete input or list candidates / 입력 자동완성 또는 후보 표시 |
| `Ctrl + C` | Cancel the running command / 실행 중인 명령 취소 |

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
