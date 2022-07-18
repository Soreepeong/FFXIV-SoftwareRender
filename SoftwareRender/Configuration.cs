using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace SoftwareRender {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public bool ConfigVisible = true;

        public bool Enabled = false;

        [NonSerialized]
        private DalamudPluginInterface? _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            _pluginInterface = pluginInterface;
        }

        public void Save() {
            _pluginInterface!.SavePluginConfig(this);
        }
    }
}
