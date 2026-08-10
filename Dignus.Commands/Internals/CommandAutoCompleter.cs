namespace Dignus.Commands.Internals
{
    internal class CommandAutoCompleter(CommandTable commandTable, AliasTable aliasTable)
    {
        public IReadOnlyList<string> GetMatches(string currentPath, string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Any(char.IsWhiteSpace))
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
                    if (command.Contains('/'))
                    {
                        continue;
                    }

                    AddIfMatches(command);
                    continue;
                }

                if (command.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string name = command[pathPrefix.Length..];
                    if (name.Contains('/') == false)
                    {
                        AddIfMatches(name);
                    }
                }
            }

            foreach (string command in commandTable.GetGlobalCommandList())
            {
                AddIfMatches(command);
            }

            foreach (var alias in aliasTable.GetDatas())
            {
                AddIfMatches(alias.Alias);
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
