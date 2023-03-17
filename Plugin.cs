﻿using BepInEx;
using BepInEx.Configuration;
using CodeMonkey.Utils;
using HarmonyLib;
using ServerSync;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

#pragma warning disable CS8632
namespace GroundReset
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        #region values
        internal const string ModName = "GroundReset", ModVersion = "1.0.3", ModGUID = "com.Frogger." + ModName;
        private static readonly Harmony harmony = new(ModGUID);
        public static Plugin _self;
        #endregion
        #region ConfigSettings
        static string ConfigFileName = "com.Frogger.GroundReset.cfg";
        DateTime LastConfigChange;
        public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
        void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
        {
            setter(config.Value);
            config.SettingChanged += (_, _) => setter(config.Value);
        }
        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        #endregion
        #region values
        internal static ConfigEntry<float> timeInMinutesConfig;
        internal static ConfigEntry<float> timePassedInMinutesConfig;
        internal static float timeInMinutes = -1;
        internal static float timePassedInMinutes;
        #endregion
        internal static Action onTimer;

        internal static DateTime lastReset;
        internal static FunctionTimer timer;



        private void Awake()
        {
            _self = this;

            #region config
            Config.SaveOnConfigSet = false;

            timeInMinutesConfig = config("General", "TheTriggerTime", 4320f, "");
            timePassedInMinutesConfig = config("DO NOT TOUCH", "time has passed since the last trigger", 0f, description: new ConfigDescription("", null, new ConfigurationManagerAttributes() { Browsable = false }));

            SetupWatcherOnConfigFile();
            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
            Config.SettingChanged += (_, _) => { UpdateConfiguration(); };
            Config.SaveOnConfigSet = true;
            Config.Save();
            #endregion
            onTimer += () =>
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, nameof(ResetTerrain));
            };

            harmony.PatchAll();
        }

        public void RPC_ResetTerrain(long @long)
        {
            lastReset = DateTime.Now;
            FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
            timePassedInMinutes = 0;
            Config.Reload();
            Debug($"on GroundReset Timer {DateTime.Now}");
        }


        public static bool ResetTerrain(TerrainComp __instance, out bool __result)
        {
            __result = false;
            ZDO zdo = __instance.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if(string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return true;
            }
            if(json == lastReset.ToString()) return true;

            DateTime time = Convert.ToDateTime(json); if(time == null) return true;



            bool ward = IsPointInsideWard(__instance);
            if(ward) return true;
            Debug($"Reset Terrain");

            __result = true;
            zdo.Set($"{ModName} time", lastReset.ToString());
            zdo.m_byteArrays?.Remove("TCData".GetStableHashCode());

            return false;
        }

        public static bool IsPointInsideWard(TerrainComp terrain)
        {
            /*foreach(PrivateArea allArea in PrivateArea.m_allAreas)
            {
                if(allArea.m_ownerFaction == Character.Faction.Players && terrain.m_hmap.IsPointInside(allArea.transform.position, allArea.m_radius))
                {
                    return true;
                }
            }*/
            bool flag = PrivateArea.m_allAreas.Any(x => x.m_ownerFaction == Character.Faction.Players && terrain.m_hmap.IsPointInside(x.transform.position, x.m_radius));
            return flag;
        }

        #region tools
        public static void Debug(string msg)
        {
            _self.DebugPrivate(msg);
        }
        private void DebugPrivate(string msg)
        {
            Logger.LogInfo(msg);
        }
        public void DebugError(string msg)
        {
            Logger.LogError($"{msg} Write to the developer and moderator if this happens often.");
        }
        #endregion
        #region Config
        public void SetupWatcherOnConfigFile()
        {
            FileSystemWatcher fileSystemWatcherOnConfig = new(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcherOnConfig.Changed += ConfigChanged;
            fileSystemWatcherOnConfig.IncludeSubdirectories = true;
            fileSystemWatcherOnConfig.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcherOnConfig.EnableRaisingEvents = true;
        }
        private void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                Debug("Reloading Config...");
            }
            catch
            {
                DebugError("Can't reload Config");
            }
        }
        private void UpdateConfiguration()
        {
            Task task = null;
            task = Task.Run(() =>
            {
                if(timeInMinutes != -1 && timeInMinutes != timeInMinutesConfig.Value && SceneManager.GetActiveScene().name == "main")
                {
                    FunctionTimer.StopAllTimersWithName("JF_GroundReset");
                    FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
                }
                timeInMinutes = timeInMinutesConfig.Value;
                timePassedInMinutes = timePassedInMinutesConfig.Value;
            });

            Task.WaitAll();
            Debug("Configuration Received");
        }
        #endregion
    }
}