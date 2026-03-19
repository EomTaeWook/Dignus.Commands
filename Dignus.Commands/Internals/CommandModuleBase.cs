using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Commands;
using Dignus.Commands.Interfaces;
using Dignus.Commands.Internals.Actors;
using Dignus.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace Dignus.Commands.Internals
{
    public abstract class CommandModuleBase
    {
        protected IServiceProvider _serviceProvider;
        protected readonly ServiceContainer _serviceContainer = new();
        private readonly CommandTable _commandTable = new();
        private bool _isBuilt = false;
        private readonly string _moduleName;

        public CommandModuleBase(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName) == true)
            {
                moduleName = Assembly.GetEntryAssembly().GetName().Name;
            }
            _moduleName = moduleName;
        }

        public void RegisterCommandActionsFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes<CommandAttribute>().Any() ||
                    type.GetCustomAttributes<GlobalCommandAttribute>().Any())
                {
                    AddCommand(type);
                }
            }
        }
        internal IServiceProvider BuildInternal()
        {
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("command module has already been built.");
            }
            _isBuilt = true;

            var callingAssembly = Assembly.GetCallingAssembly();
            RegisterCommandActionsFromAssembly(callingAssembly);

            _serviceContainer.RegisterType(_commandTable);

            if (File.Exists(AliasTable.Path) == true)
            {
                var alias = JsonSerializer.Deserialize<List<AliasModel>>(File.ReadAllText(AliasTable.Path));
                _serviceContainer.RegisterType(new AliasTable(alias));
            }
            else
            {
                _serviceContainer.RegisterType(new AliasTable([]));
            }
            _serviceContainer.RegisterType<CommandExecutionActor, CommandExecutionActor>();
            _serviceContainer.RegisterType(_serviceContainer);
            return _serviceProvider = _serviceContainer.Build();
        }
        public void AddGlobalCommand(string commandName, string desc, Func<string[], IActorRef, CancellationToken, Task> action)
        {
            AddCommandInternal(string.Empty, commandName, new ActionCommand(action, desc), true);
        }
        public void AddCommand(string commandName, string desc, Func<string[], IActorRef, CancellationToken, Task> action)
        {
            AddCommand(string.Empty, commandName, desc, action);
        }

        public void AddCommand(string commandPath, string commandName, string desc, Func<string[], IActorRef, CancellationToken, Task> action)
        {
            var actionCommand = new ActionCommand(action, desc);
            AddCommandInternal(commandPath, commandName, new ActionCommand(action, desc), false);
        }

        public void AddCommand<T>(string commandPath, string commandName, T command) where T : class, IPathCommand
        {
            AddCommandInternal(commandPath, commandName, command, false);
        }
        public void AddGlobalCommand<T>(string commandName, T command) where T : class, IPathCommand
        {
            AddCommandInternal(string.Empty, commandName, command, true);
        }
        public void AddCommand<T>() where T : IPathCommand
        {
            AddCommand(typeof(T));
        }

        public void AddCommand(Type commandType)
        {
            if (typeof(IPathCommand).IsAssignableFrom(commandType) == false || commandType.IsInterface == true)
            {
                throw new InvalidCastException(nameof(commandType));
            }

            var commandNames = new List<string>();
            var commandPath = string.Empty;
            bool isGlobalCommand = false;

            if (commandType.IsDefined(typeof(CommandAttribute)) == true)
            {
                var attr = commandType.GetCustomAttribute<CommandAttribute>();
                commandPath = attr.CommandPath;
                commandNames.Add(attr.CommandName);
            }
            else if (commandType.IsDefined(typeof(GlobalCommandAttribute)) == true)
            {
                isGlobalCommand = true;

                var attrs = commandType.GetCustomAttributes<GlobalCommandAttribute>();
                foreach (var attr in attrs)
                {
                    commandNames.Add(attr.CommandName);
                }
            }
            else
            {
                commandNames.Add(commandType.Name);
            }

            foreach (var commandName in commandNames)
            {
                string commandKey;
                if (isGlobalCommand)
                {
                    commandKey = _commandTable.AddGlobalCommand(commandName);
                }
                else
                {
                    commandKey = _commandTable.AddCommand(commandPath, commandName);
                }
                _serviceContainer.RegisterType(commandKey, commandType, LifeScope.Transient);
            }
        }

        private void AddCommandInternal<T>(string commandPath, string commandName, T command, bool isGlobalCommand) where T : class, IPathCommand
        {
            string commandKey;
            if (isGlobalCommand == true)
            {
                commandKey = _commandTable.AddGlobalCommand(commandName);
            }
            else
            {
                commandKey = _commandTable.AddCommand(commandPath, commandName);
            }

            _serviceContainer.RegisterType(commandKey, command);
        }

        public string GetModuleName()
        {
            return _moduleName;
        }
    }
}
