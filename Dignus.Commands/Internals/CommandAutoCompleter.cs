namespace Dignus.Commands.Internals
{
    internal class CommandAutoCompleter(CommandTable commandTable, AliasTable aliasTable)
    {
        public IReadOnlyList<string> GetMatches(string currentPath, string input)
        {
            if (input == null)
            {
                return [];
            }

            if (TryGetChangeDirectoryArgument(input, out int argumentStart))
            {
                return GetDirectoryMatches(currentPath, input, argumentStart);
            }

            if (input.Any(char.IsWhiteSpace))
            {
                return [];
            }

            var candidates = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalizedPath = currentPath.Trim('/');
            string pathPrefix = string.IsNullOrEmpty(normalizedPath) ? string.Empty : normalizedPath + "/";

            foreach (string command in commandTable.GetCommandList())
            {
                if (string.IsNullOrEmpty(pathPrefix))
                {
                    AddIfMatches(command);
                    continue;
                }

                if (command.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string name = command[pathPrefix.Length..];
                    AddIfMatches(name);
                }
            }

            if (string.IsNullOrEmpty(normalizedPath))
            {
                foreach (string command in commandTable.GetGlobalCommandList())
                {
                    AddIfMatches(command);
                }

                foreach (var alias in aliasTable.GetDatas())
                {
                    AddIfMatches(alias.Alias);
                }
            }

            return [.. candidates];

            void AddIfMatches(string candidate)
            {
                if (candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(candidate);
                }
            }
        }

        private IReadOnlyList<string> GetDirectoryMatches(string currentPath, string input, int argumentStart)
        {
            string pathInput = input[argumentStart..].Replace('\\', '/');
            if (pathInput.Any(char.IsWhiteSpace))
            {
                return [];
            }

            var directories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string command in commandTable.GetCommandList())
            {
                string normalizedCommand = command.Trim('/');
                int separatorIndex = normalizedCommand.LastIndexOf('/');
                if (separatorIndex < 0)
                {
                    continue;
                }

                string directory = normalizedCommand[..separatorIndex];
                while (string.IsNullOrEmpty(directory) == false)
                {
                    directories.Add(directory);

                    separatorIndex = directory.LastIndexOf('/');
                    if (separatorIndex < 0)
                    {
                        break;
                    }

                    directory = directory[..separatorIndex];
                }
            }

            var candidates = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalizedPath = currentPath.Trim('/');
            string currentPathPrefix = string.IsNullOrEmpty(normalizedPath) ? string.Empty : normalizedPath + "/";
            bool isAbsolutePath = pathInput.StartsWith('/');
            string inputPrefix = input[..argumentStart];

            foreach (string directory in directories)
            {
                string candidatePath;
                if (isAbsolutePath)
                {
                    candidatePath = "/" + directory;
                }
                else if (string.IsNullOrEmpty(currentPathPrefix))
                {
                    candidatePath = directory;
                }
                else if (directory.StartsWith(currentPathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    candidatePath = directory[currentPathPrefix.Length..];
                }
                else
                {
                    continue;
                }

                string candidate = inputPrefix + candidatePath;
                if (candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(candidate);
                }
            }

            return [.. candidates];
        }

        private static bool TryGetChangeDirectoryArgument(string input, out int argumentStart)
        {
            argumentStart = 0;
            while (argumentStart < input.Length && char.IsWhiteSpace(input[argumentStart]) == false)
            {
                argumentStart++;
            }

            if (argumentStart == input.Length || input[..argumentStart].Equals("cd", StringComparison.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            while (argumentStart < input.Length && char.IsWhiteSpace(input[argumentStart]))
            {
                argumentStart++;
            }

            return true;
        }

        public static string GetCommonPrefix(IReadOnlyList<string> matches)
        {
            if (matches.Count == 0)
            {
                return string.Empty;
            }

            string prefix = matches[0];
            foreach (string match in matches.Skip(1))
            {
                int length = 0;
                int maximumLength = Math.Min(prefix.Length, match.Length);
                while (length < maximumLength && char.ToUpperInvariant(prefix[length]) == char.ToUpperInvariant(match[length]))
                {
                    length++;
                }

                prefix = prefix[..length];
            }

            return prefix;
        }
    }
}
