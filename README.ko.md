# Dignus.Commands

[English](README.md) | 한국어

로컬 콘솔·Telnet을 위한 비동기 명령, 미들웨어, 취소와 자동완성 라이브러리.

## 요구 사항

라이브러리는 net8.0, net9.0, net10.0을 대상으로 한다.

## 설치

```xml
<PackageReference Include="Dignus.Commands" Version="1.1.4" />
```

## 빠른 시작

상세 가이드의 명령 클래스 예제에 있는 Close를 정의한 뒤 로컬 실행기에 등록한다.

```csharp
using Dignus.Commands;

var commandRunner = new LocalCommandRunner();
commandRunner.AddCommand<Close>();
commandRunner.Build();
commandRunner.Run();
```

## 사용법

ICommand, 델리게이트 등록, Telnet, 미들웨어, 취소와 경로 기반 Tab 자동완성은 가이드를 따른다. 명령을 등록한 뒤 실행기의 Build()를 호출한다.

## 문서

- [명령 API와 예제 · 한영](docs/Usage.md)

## 라이선스

MIT 라이선스. [LICENSE](LICENSE)를 참조한다.
