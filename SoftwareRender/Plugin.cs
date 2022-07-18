using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoftwareRender {
    public unsafe sealed class Plugin : IDalamudPlugin {
        public string Name => "Software Render";

        private readonly DalamudPluginInterface _pluginInterface;
        private readonly Configuration _config;

        private readonly Hook<D3D11CreateDeviceDelegate> _d3d11CreateDeviceHook;

        private delegate int D3D11CreateDeviceDelegate(void* pAdapter, int DriverType, void* Software, uint Flags, int* pFeatureLevels, uint FeatureLevels, uint SDKVersion, void** ppDevice, int* pFeatureLevel, void** ppImmediateContext);

        public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface) {
            _pluginInterface = pluginInterface;

            _config = _pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            _config.Initialize(_pluginInterface);

            _pluginInterface.UiBuilder.Draw += DrawUI;
            _pluginInterface.UiBuilder.OpenConfigUi += () => { _config.ConfigVisible = !_config.ConfigVisible; };

            _d3d11CreateDeviceHook = Hook<D3D11CreateDeviceDelegate>.FromImport(null, "d3d11.dll", "D3D11CreateDevice", 0, D3D11CreateDeviceDetour);
            _d3d11CreateDeviceHook.Enable();
        }

        private void Save() {
            if (_config == null)
                return;

            _config.Save();
        }

        public void Dispose() {
            Save();

            _d3d11CreateDeviceHook.Dispose();
        }

        private int D3D11CreateDeviceDetour(void* pAdapter, int DriverType, void* Software, uint Flags, int* pFeatureLevels, uint FeatureLevels, uint SDKVersion, void** ppDevice, int* pFeatureLevel, void** ppImmediateContext) {
            int res;
            if (_config.Enabled && 0 == (GetAsyncKeyState(Dalamud.Game.ClientState.Keys.VirtualKey.SHIFT) & 0x8000)) {
                res = _d3d11CreateDeviceHook.Original(null, 5 /* D3D_DRIVER_TYPE_WARP */, Software, Flags, pFeatureLevels, FeatureLevels, SDKVersion, ppDevice, pFeatureLevel, ppImmediateContext);
                if (res >= 0) {
                    if (Environment.ProcessorCount > 1)
                        Process.GetCurrentProcess().ProcessorAffinity &= ~(nint)1;
                    return res;
                }
            }

            return _d3d11CreateDeviceHook.Original(pAdapter, DriverType, Software, Flags, pFeatureLevels, FeatureLevels, SDKVersion, ppDevice, pFeatureLevel, ppImmediateContext);
        }

        private void DrawUI() {
            if (!_config.ConfigVisible)
                return;

            if (ImGui.Begin("SoftwareRender Configuration###MainWindow", ref _config.ConfigVisible)) {
                try {
                    if (ImGui.Checkbox("Enable software rendering", ref _config.Enabled))
                        Save();

                    ImGui.Text("You must restart the game for the changes to get applied.");
                    ImGui.Text("Hold Shift to disable software rendering.");
                    ImGui.Text("This may cause BSoD.");
                    ImGui.Text("Terminating ffxiv process via Task Manager will result in a process that simply does not terminate.");

                    if (ImGui.Button("About WARP software renderer..."))
                        Process.Start(new ProcessStartInfo("https://docs.microsoft.com/en-us/windows/win32/direct3darticles/directx-warp") { UseShellExecute = true });
                } finally { ImGui.End(); }
            } else {
                Save();
            }
        }

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Dalamud.Game.ClientState.Keys.VirtualKey vKeys);
    }
}
