using Dignus.Actor.Core;
using Dignus.Commands.Attributes;
using Dignus.Commands.Commands;
using Dignus.Commands.Interfaces;
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using System.Reflection;
using System.Text.Json;

namespace Dignus.Commands.Internals
{
    public abstract class CommandModuleBase
    {
        internal protected IServiceProvider _serviceProvider;

        private readonly ServiceContainer _serviceContainer;

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
            _serviceContainer = new ServiceContainer();
        }

        private void RegisterCommandActionsFromAssembly(Assembly assembly)
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
        internal void BuildInternal()
        {
            if (_isBuilt == true)
            {
                throw new InvalidOperationException("command module has already been built.");
            }
            _isBuilt = true;

            var callingAssembly = Assembly.GetCallingAssembly();

            _serviceContainer.RegisterDependencies(callingAssembly);
            RegisterCommandActionsFromAssembly(callingAssembly);

            _serviceContainer.RegisterType(_commandTable);
            _serviceContainer.RegisterType(_serviceContainer);

            if (File.Exists(AliasTable.Path) == true)
            {
                var alias = JsonSerializer.Deserialize<List<AliasModel>>(File.ReadAllText(AliasTable.Path));
                _serviceContainer.RegisterType(new AliasTable(alias));
            }
            else
            {
                _serviceContainer.RegisterType(new AliasTable([]));
            }

            _serviceProvider = _serviceContainer.Build();
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

        public void AddCommand<T>(T command) where T : class, IPathCommand
        {
            var commandType = command.GetType();

            if (commandType.IsDefined(typeof(CommandAttribute)))
            {
                var attribute = commandType.GetCustomAttribute<CommandAttribute>();
                AddCommandInternal(attribute.CommandPath, attribute.CommandName, command, false);
            }
            else if (commandType.IsDefined(typeof(GlobalCommandAttribute)))
            {
                var attributes = commandType.GetCustomAttributes<GlobalCommandAttribute>();
                foreach (var attribute in attributes)
                {
                    AddCommandInternal(string.Empty, attribute.CommandName, command, true);
                }
            }
        }

        public void AddCommand<T>() where T : IPathCommand
        {
            AddCommand(typeof(T));
        }

        private void AddCommandInternal<T>(string commandPath, string commandName, T command, bool isGlobalCommand) where T : class, IPathCommand
        {
            if (isGlobalCommand == true)
            {
                _commandTable.AddGlobalCommand(commandName, typeof(T));
            }
            else
            {
                _commandTable.AddCommand(commandPath, commandName, typeof(T));
            }

            _serviceContainer.RegisterType(commandName, command);
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
                if (isGlobalCommand)
                {
                    _commandTable.AddGlobalCommand(commandName, commandType);
                }
                else
                {
                    _commandTable.AddCommand(commandPath, commandName, commandType);
                }

                _serviceContainer.RegisterType(commandName, commandType, LifeScope.Transient);
            }
        }

        public string GetModuleName()
        {
            return _moduleName;
        }
    }
}
