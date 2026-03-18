namespace Dignus.Commands.Internals
{
    internal class AliasModel
    {
        public string Alias { get; set; }

        public string CommandName { get; set; }
    }

    internal class AliasTable
    {
        public static readonly string Path = $"{AppContext.BaseDirectory}aliasTable.json";
        internal Dictionary<string, AliasModel> Alias { get => _aliasMap; }
        public AliasModel this[string key] => _aliasMap[key];

        readonly Dictionary<string, AliasModel> _aliasMap = new();

        public AliasTable(ICollection<AliasModel> aliasModels)
        {
            foreach (var item in aliasModels)
            {
                _aliasMap.Add(item.Alias, item);
            }
        }
        public void AddAlias(AliasModel alias)
        {
            if (!_aliasMap.TryAdd(alias.Alias, alias))
            {
                _aliasMap[alias.Alias] = alias;
            }
        }
        public void RemoveAlias(string alias)
        {
            _aliasMap.Remove(alias);
        }
        public IReadOnlyCollection<AliasModel> GetDatas()
        {
            return _aliasMap.Values;
        }

    }
}
