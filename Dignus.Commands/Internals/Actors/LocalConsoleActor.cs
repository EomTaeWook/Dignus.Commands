using Dignus.Actor.Abstractions;
using Dignus.Actor.Core;
using Dignus.Commands.Messages;

namespace Dignus.Commands.Internals.Actors
{
    internal class LocalConsoleActor(IActorRef commandExecutionActorRef,
        CommandAutoCompleter commandAutoCompleter) : ActorBase
    {
        private string _currentPath = "/";
        private Action _exitRequested;
        private string _moduleName;
        protected override ValueTask OnReceive(IActorMessage message, IActorRef sender)
        {
            if(message is CommandResponseMessage commandResponse)
            {
                Console.WriteLine(commandResponse.Content);
            }
            else if(message is StartPromptMessage)
            {
                ShowPrompt();
            }
            else if(message is CancelCommandMessage)
            {
                commandExecutionActorRef.Post(message, Self);
            }
            else if (message is ChangeDirectoryRequestMessage changeDirectoryRequestMessage)
            {
                HandleDirectoryChanged(changeDirectoryRequestMessage);
            }
            else if(message is ConfirmCommandExitMessage)
            {
                _exitRequested?.Invoke();
            }
            return ValueTask.CompletedTask;
        }
        public void Initialize(string moduleName, Action exitRequested)
        {
            _moduleName = moduleName;
            _exitRequested = exitRequested;
        }
        private void HandleDirectoryChanged(ChangeDirectoryRequestMessage changeDirectoryRequest)
        {
            var result = CommandPathResolver.Resolve(_currentPath, changeDirectoryRequest.Path);
            _currentPath = result;
        }
        private void ShowPrompt()
        {
            string prompt = $"{_moduleName}:{_currentPath}> ";
            Console.Write(prompt);
            Task.Run(() => 
            {
                var line = ReadCommandLine(prompt);
                var message = new RunCommandRequestMessage(_currentPath, line, Self);
                commandExecutionActorRef.Post(message, Self);
            });
        }

        private string ReadCommandLine(string prompt)
        {
            var input = new System.Text.StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input.Length--;
                        Console.Write("\b \b");
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Tab)
                {
                    CompleteInput(prompt, input);
                    continue;
                }

                if (key.KeyChar != '\0' && char.IsControl(key.KeyChar) == false)
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        private void CompleteInput(string prompt, System.Text.StringBuilder input)
        {
            IReadOnlyList<string> matches = commandAutoCompleter.GetMatches(_currentPath, input.ToString());
            if (matches.Count == 0)
            {
                return;
            }

            string completion = matches.Count == 1 ? matches[0] : CommandAutoCompleter.GetCommonPrefix(matches);
            if (completion.Length > input.Length)
            {
                Console.Write(completion[input.Length..]);
                input.Clear();
                input.Append(completion);
                return;
            }

            if (matches.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine(string.Join("  ", matches));
                Console.Write(prompt);
                Console.Write(input);
            }
        }
    }
}
