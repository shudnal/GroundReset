﻿using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using CodeMonkey.Utils;
using UnityEngine.SceneManagement;

namespace GroundReset;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModName = "GroundReset",
        ModAuthor = "Frogger",
        ModVersion = "1.1.0",
        ModGUID = $"com.{ModAuthor}.{ModName}";

    internal static Action onTimer;
    internal static DateTime lastReset = DateTime.MinValue;
    internal static FunctionTimer timer;

    internal static ConfigEntry<float> timeInMinutesConfig;

    internal static ConfigEntry<float> timePassedInMinutesConfig;

    internal static ConfigEntry<float> savedTimeUpdateIntervalConfig;
    internal static float timeInMinutes = -1;

    internal static float timePassedInMinutes;

    // internal static float fuckingBugDistance;
    internal static float savedTimeUpdateInterval;

    internal static ConfigEntry<bool> pokeCompConfig;

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion);
        OnConfigurationChanged += UpdateConfiguration;

        timeInMinutesConfig = mod.config("General", "TheTriggerTime", 4320f, "");
        // fuckingBugDistanceConfig = mod.config("General", "Fucking Bug Distance", 115f, "");
        savedTimeUpdateIntervalConfig = mod.config("General", "SavedTime Update Interval (seconds)", 120f, "");
        timePassedInMinutesConfig = mod.config("DO NOT TOUCH", "time has passed since the last trigger", 0f,
            new ConfigDescription("", null,
                new ConfigurationManagerAttributes { Browsable = false }));

        pokeCompConfig = mod.config("General", "pokeCompConfig", false, "");


        onTimer += () => Reseter.ResetAllTerrains();
    }

    public void RPC_ResetTerrain(long _)
    {
        lastReset = DateTime.Now;
        FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
        timePassedInMinutes = 0;
        Config.Reload();
        Debug($"Подготовка к сбросу территории {DateTime.Now}");
        if (!ZNet.m_isServer)
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                "<color=yellow>Подготовка к сбросу территории</color>");
    }

    private void UpdateConfiguration()
    {
        Task task = null;
        task = Task.Run(() =>
        {
            if (timeInMinutes != -1 && timeInMinutes != timeInMinutesConfig.Value &&
                SceneManager.GetActiveScene().name == "main")
            {
                FunctionTimer.StopAllTimersWithName("JF_GroundReset");
                FunctionTimer.Create(onTimer, timeInMinutes * 60, "JF_GroundReset", true, true);
            }

            timeInMinutes = timeInMinutesConfig.Value;
            timePassedInMinutes = timePassedInMinutesConfig.Value;
            //   fuckingBugDistance = fuckingBugDistanceConfig.Value;
            savedTimeUpdateInterval = savedTimeUpdateIntervalConfig.Value;
        });

        Task.WaitAll();
        Debug("Configuration Received");
    }
}