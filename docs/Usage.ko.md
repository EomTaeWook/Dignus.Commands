# Dignus.Commands

[English](Usage.md) | 한국어

[한국어 개요](../README.ko.md)

로컬 콘솔과 Telnet 원격 명령 실행을 지원한다. 특성·델리게이트 등록, 비동기 실행, CancellationToken 취소, 미들웨어와 Tab 자동완성을 제공한다.

## 빠른 시작

아래 Close 명령을 정의한 뒤 실행기에 등록한다.

```csharp
using Dignus.Commands;

var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```

## 로컬 콘솔과 자동완성

LocalCommandRunner로 로컬 입력을 처리한다. Tab은 현재 명령 경로를 기준으로 동작한다. 루트에서는 전체 경로, 하위에서는 현재 위치에 상대적인 경로를 완성한다. 빈 입력에서 Tab은 현재 경로의 명령을 나열한다. 전역 명령·별칭은 루트에서만 후보로 표시하지만 전역 명령 자체는 어느 경로에서나 실행할 수 있다. 후보가 여러 개면 첫 Tab이 공통 접두어를 채우고 다시 누르면 모두 표시한다.

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

## Telnet

서버를 열고 일반 Telnet 클라이언트로 접속한다. 별도 전용 클라이언트 모듈은 제공하지 않는다.

```csharp
using Dignus.Commands;

var module = new TelnetCommandRunner(port: 50000);

module.AddCommand<Close>();

module.Build();
module.Run();
```

```
telnet 127.0.0.1 50000
```

## 명령 클래스

ICommand를 구현하고 Command 특성을 지정한 뒤 AddCommand로 등록한다.

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

```csharp
module.AddCommand<Close>();
```

## 델리게이트 등록

명령 이름·설명·비동기 델리게이트로도 등록한다. 긴 실행은 전달받은 취소 토큰을 관찰한다.

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

## 미들웨어

명령 실행 파이프라인에 전후 처리를 등록한다. 예제 AuthAttribute와 인증 판정은 소비 프로젝트가 제공하는 계약이다. 다음 단계는 next.InvokeAsync로 연결한다.

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

## 실행 흐름과 취소

사용자 입력 → 파서 → 미들웨어 → 명령 실행 → CommandResponseMessage 순서다. 긴 실행은 토큰을 확인하고 취소 가능한 대기를 사용한다. Telnet 연결이 닫히면 CancelCommandMessage를 전달한다.

```csharp
while (cancellationToken.IsCancellationRequested == false)
{
    await Task.Delay(1000, cancellationToken);
}
```

## 출력

sender에 CommandResponseMessage를 전송한다.

```csharp
sender.Post(new CommandResponseMessage()
{
    Content = "hello"
});
```

## 기본 명령

| 명령 | 설명 |
| --- | --- |
| ?, h, help | 사용 가능한 명령 표시 |
| cd [path] | 현재 명령 경로 변경 |
| alias [name] [command] | 별칭 생성·목록 |
| unalias [name] | 별칭 제거 |
| Tab | 자동완성·후보 목록 |
| Ctrl+C | 실행 중 명령 취소 |

## 수명

실행기 생성 → 명령 등록 → 미들웨어 등록 → Build → Run 순서다. Run은 Build 이후에 호출한다.

```csharp
var module = new LocalCommandRunner();

module.AddCommand<Close>();

module.Build();
module.Run();
```
