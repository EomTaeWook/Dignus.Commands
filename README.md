# Dignus.Commands

English | [한국어](README.ko.md)

Command execution for local consoles and Telnet, with asynchronous commands, middleware, cancellation, and completion.

## Requirements

The library targets net8.0, net9.0, and net10.0.

## Installation

```xml
<PackageReference Include="Dignus.Commands" Version="1.1.4" />
```

## Quick start

Define Close using the command-class example in the usage guide, then register it with the local runner.

```csharp
using Dignus.Commands;

var commandRunner = new LocalCommandRunner();
commandRunner.AddCommand<Close>();
commandRunner.Build();
commandRunner.Run();
```

## Usage

The guide covers ICommand, delegate registration, Telnet, middleware, cancellation, and path-aware Tab completion. Build the runner after registering commands.

## Documentation

- [Command API and examples / English and Korean](docs/Usage.md)

## License

MIT License. See [LICENSE](LICENSE).
