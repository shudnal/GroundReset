using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;

namespace GroundReset;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModName = "GroundReset",
        ModAuthor = "Frogger",
        ModVersion = "2.0.1",
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

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGUID);
        OnConfigurationChanged += UpdateConfiguration;

        timeInMinutesConfig = config("General", "TheTriggerTime", 4320f, "");
        savedTimeUpdateIntervalConfig = config("General", "SavedTime Update Interval (seconds)", 120f, "");
        timePassedInMinutesConfig = config("DO NOT TOUCH", "time has passed since the last trigger", 0f,
            new ConfigDescription("", null,
                new ConfigurationManagerAttributes { Browsable = false }));

        onTimer += () =>
        {
            Debug("Timer Triggered, Resetting...");
            Reseter.ResetAllTerrains();
        };
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