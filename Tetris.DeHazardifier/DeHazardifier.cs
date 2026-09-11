using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HazardPatches;
using System;
using System.Collections.Generic;
using System.Threading;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tetris.DeHazardifier
{
    [BepInPlugin("com.Tetris.DeHazardifier", "Tetris.DeHazardifier", "1.1.2")]
    public class DeHazardifier : BaseUnityPlugin
    {
        private ConfigEntry<bool> _master;
        private ConfigEntry<bool> _barbedWireVisuals;
        private (ConfigEntry<bool> config, ModulePatch[] patches)[] _groups;
        private GameWorld _gameWorld;
        private bool _visualsDirty = true;
        private int _settingsDirty;
        // Only renderers changed by this plugin are restored.
        private readonly HashSet<Renderer> _hiddenRenderers = new HashSet<Renderer>();

        private void Awake()
        {
            _master = Config.Bind("A - De-Hazardifier Enabler", "A - De-Hazardifier Enabled", true, "Enables the De-Hazardifier.");
            var minefields = Config.Bind("A - De-Hazardifier Settings", "A - Minefield Disabler", true, "Disables minefields.");
            var barbedWire = Config.Bind("A - De-Hazardifier Settings", "D - Barbed Wire Disabler", true, "Disables barbed wire damage and speed penalties.");
            _barbedWireVisuals = Config.Bind("A - De-Hazardifier Settings", "E - Barbed Wire Visuals Disabler", true, "Hides renderers on barbed wire objects and their children while the mod is enabled. Does not disable damage or colliders.");
            var snipers = Config.Bind("A - De-Hazardifier Settings", "F - Sniper Border Zones Disabler", true, "Disables sniper border zones.");
            var fire = Config.Bind("A - De-Hazardifier Settings", "G - Fire Damage Disabler", true, "Disables damage taken from standing in fire.");
            _groups = new[]
            {
                (minefields, new ModulePatch[] { new MinefieldCoroutinePatch(), new MinefieldDamagePatch(), new MinefieldViewTriggerPatch() }),
                (barbedWire, new ModulePatch[] { new BarbedWireDamagePatch(), new BarbedWireSpeedPenaltyPatch() }),
                (snipers, new ModulePatch[] { new SniperImitatorDamagePatch(), new SniperImitatorShootPatch(), new SniperFiringZoneShootPatch(), new SniperFiringZoneCoroutinePatch() }),
                (fire, new ModulePatch[] { new FlameDamageTriggerPatch() })
            };
            foreach (var group in _groups)
                group.config.SettingChanged += OnSettingChanged;
            _master.SettingChanged += OnSettingChanged;
            _barbedWireVisuals.SettingChanged += OnSettingChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ApplySettings();
            Logger.LogInfo("Tetris.DeHazardifier 1.1.2 initialized for SPT 4.1.5.");
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            // Configuration callbacks may originate outside Unity's main thread.
            Interlocked.Exchange(ref _settingsDirty, 1);
        }

        private void ApplySettings()
        {
            foreach (var group in _groups)
                foreach (var patch in group.patches)
                    patch.SetEnabled(_master.Value && group.config.Value);
            _visualsDirty = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _visualsDirty = true;
        private void OnSceneUnloaded(Scene scene) => _visualsDirty = true;

        private void Update()
        {
            if (Interlocked.Exchange(ref _settingsDirty, 0) != 0)
                ApplySettings();

            var world = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
            if (_gameWorld != world)
            {
                RestoreVisuals();
                _gameWorld = world;
                _visualsDirty = true;
            }

            if (!_master.Value || !_barbedWireVisuals.Value || world == null || world.MainPlayer == null)
            {
                RestoreVisuals();
                _visualsDirty = true;
                return;
            }

            if (!_visualsDirty)
                return;

            _hiddenRenderers.RemoveWhere(renderer => renderer == null);
            foreach (var wire in FindObjectsOfType<BarbedWire>())
            {
                foreach (var renderer in wire.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null && renderer.enabled)
                    {
                        _hiddenRenderers.Add(renderer);
                        renderer.enabled = false;
                    }
                }
            }
            _visualsDirty = false;
        }

        private void RestoreVisuals()
        {
            foreach (var renderer in _hiddenRenderers)
                if (renderer != null)
                    renderer.enabled = true;
            _hiddenRenderers.Clear();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (_master != null)
                _master.SettingChanged -= OnSettingChanged;
            if (_barbedWireVisuals != null)
                _barbedWireVisuals.SettingChanged -= OnSettingChanged;
            if (_groups != null)
                foreach (var group in _groups)
                {
                    group.config.SettingChanged -= OnSettingChanged;
                    foreach (var patch in group.patches)
                        if (patch.IsActive)
                        {
                            try { patch.Disable(); }
                            catch (Exception exception) { Logger.LogError(exception); }
                        }
                }
            RestoreVisuals();
            _gameWorld = null;
        }
    }

    static class PatchExtensions
    {
        public static void SetEnabled(this ModulePatch patch, bool enabled)
        {
            if (patch.IsActive == enabled)
                return;
            if (enabled)
                patch.Enable();
            else
                patch.Disable();
        }
    }
}

