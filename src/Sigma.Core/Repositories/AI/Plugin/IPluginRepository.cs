using System.Collections.Generic;
using Sigma.Core.Repositories.Base;

namespace Sigma.Core.Repositories
{
    public interface IPluginRepository : IRepository<Plugin>
    {
        void RegisterPlugin(PluginMetadata metadata);
        PluginMetadata? GetPlugin(string name);
        IEnumerable<PluginMetadata> GetPlugins();
    }
}
