using Dignus.Collections;

namespace Dignus.Commands.Internals
{
    internal class CommandTable
    {
        private readonly UniqueSet<string> _commandNames = [];
        private readonly UniqueSet<string> _globalCommandNames = [];

        public string AddCommand(string path, string commandName)
        {
            var key = commandName;
            if(string.IsNullOrWhiteSpace(path) == false)
            {
                key = $"{path}/{commandName}";
            }
            _commandNames.Add(key);
            return key;
        }

        public string AddGlobalCommand(string commandName)
        {
            _globalCommandNames.Add(commandName);
            return commandName;
        }
        public IEnumerable<string> GetCommandListByPath(string currentPath) 
        {
            var findNames = new ArrayQueue<string>();

            foreach (var command in _commandNames)
            {
                if(command.StartsWith(currentPath) == false)
                {
                    continue;
                }

                findNames.Add(command);
            }
            return findNames;
        }
        public string GetCommand(string currentPath, string commandName)
        {
            string key;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                key = commandName;
            }
            else
            {
                key = $"{currentPath}/{commandName}";
            }

            if (_commandNames.Contains(key))
            {
                return key;
            }

            if (_globalCommandNames.Contains(commandName))
            {
                return commandName;
            }

            return null;
        }

        public IEnumerable<string> GetGlobalCommandList()
        {
            return _globalCommandNames;
        }
        public IEnumerable<string> GetCommandList()
        {
            return _commandNames;
        }
    }
}
