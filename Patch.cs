using CodeMonkey.Utils;
using UnityEngine.SceneManagement;

namespace GroundReset;

[HarmonyPatch]
internal class Patch
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] [HarmonyPostfix]
    public static void ZNetSceneAwake_StartTimer(ZNetScene __instance)
    {
        if (SceneManager.GetActiveScene().name != "main") return;
        if (!ZNet.instance.IsServer()) return;

        float time;
        if (timePassedInMinutes > 0) time = timePassedInMinutes;
        else time = timeInMinutes;
        time *= 60;

        FunctionTimer.Create(onTimer, time, "JF_GroundReset", true, true);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))] [HarmonyPostfix]
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static void ZNet_SaveTime(ZNet __instance)
    {
        if (!ZNet.instance.IsServer()) return;
        __instance.StartCoroutine(Reseter.SaveTime());
    }
}