using System.Collections.Concurrent;
using Sigma.Core.Repositories.Base;
using Sigma.Data;

namespace Sigma.Core.Repositories
{
    /// <summary>
    /// Repository for persisted plugins plus in-memory registry for uploaded plugin packages.
    /// </summary>
    public class PluginRepository : Repository<Plugin>, IPluginRepository
    {
        private static readonly ConcurrentDictionary<string, PluginMetadata> _pluginRegistry = new();

        public PluginRepository(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// Register metadata about an uploaded plugin package.
        /// </summary>
        public void RegisterPlugin(PluginMetadata metadata)
        {
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.Name))
            {
                return;
            }

            _pluginRegistry[metadata.Name] = metadata;
        }

        /// <summary>
        /// Retrieve metadata for a plugin by name.
        /// </summary>
        public PluginMetadata? GetPlugin(string name)
        {
            return _pluginRegistry.TryGetValue(name, out var meta) ? meta : null;
        }

        /// <summary>
        /// Get all registered plugin metadata entries.
        /// </summary>
        public IEnumerable<PluginMetadata> GetPlugins()
        {
            return _pluginRegistry.Values;
        }
    }

    /// <summary>
    /// Simple metadata description for plugin packages stored on the server.
    /// </summary>
    public class PluginMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}
