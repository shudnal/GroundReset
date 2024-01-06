using BepInEx;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;

namespace GroundReset;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModName = "GroundReset",
        ModAuthor = "Frogger",
        ModVersion = "2.4.2",
        ModGUID = $"com.{ModAuthor}.{ModName}";

    internal static Action onTimer;
    internal static FunctionTimer timer;

    internal static ConfigEntry<float> timeInMinutesConfig;
    internal static ConfigEntry<float> timePassedInMinutesConfig;
    internal static ConfigEntry<float> savedTimeUpdateIntervalConfig;
    internal static ConfigEntry<float> dividerConfig;
    internal static ConfigEntry<float> minHeightToSteppedResetConfig;
    internal static float timeInMinutes = -1;
    internal static float timePassedInMinutes;
    internal static float savedTimeUpdateInterval;

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGUID);
        OnConfigurationChanged += UpdateConfiguration;

        timeInMinutesConfig = config("General", "TheTriggerTime", 4320f, "Time in minutes before reset.");
        dividerConfig = config("General", "Divider", 1.7f,
            "The divider for the terrain restoration. Current value will be divided by this value.");
        minHeightToSteppedResetConfig = config("General", "Min Height To Stepped Reset", 0.2f,
            "If the height is lower than this value, the terrain will be reset instantly.");
        savedTimeUpdateIntervalConfig = config("General", "SavedTime Update Interval (seconds)", 120f,
            "How often elapsed time will be saved to config file.");
        timePassedInMinutesConfig = config("DO NOT TOUCH", "time has passed since the last trigger", 0f,
            new ConfigDescription("", null,
                new ConfigurationManagerAttributes { Browsable = false }));

        onTimer += () =>
        {
            Debug("Timer Triggered, Resetting...");
            ResetAll();
            InitTimer();
        };
    }

    private void UpdateConfiguration()
    {
        if (Math.Abs(timeInMinutes - timeInMinutesConfig.Value) > 1f
            && SceneManager.GetActiveScene().name == "main") InitTimer();

        timeInMinutes = timeInMinutesConfig.Value;
        timePassedInMinutes = timePassedInMinutesConfig.Value;
        savedTimeUpdateInterval = savedTimeUpdateIntervalConfig.Value;
        Debug("Configuration Received");
    }

    private static void InitTimer()
    {
        FunctionTimer.StopAllTimersWithName("JF_GroundReset");
        FunctionTimer.Create(onTimer, timeInMinutesConfig.Value * 60, "JF_GroundReset", true, true);
    }
}