using GroundReset.Compatibility.WardIsLove;
using UnityEngine.SceneManagement;

namespace GroundReset.Patch;

[HarmonyPatch] internal class InitWardsSettings
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] [HarmonyPostfix] [HarmonyWrapSafe]
    internal static void Init(ZNetScene __instance)
    {
        if (SceneManager.GetActiveScene().name != "main") return;
        if (!ZNet.instance.IsServer()) return;

        Reseter.wardsSettingsList = new List<WardSettings>();

        AddWard("guard_stone");
        AddWardThorward();
    }

    private static void AddWard(string name)
    {
        var prefab = ZNetScene.instance.GetPrefab(name.GetStableHashCode());
        if (prefab)
        {
            var areaComponent = prefab.GetComponent<PrivateArea>();
            Reseter.wardsSettingsList.Add(new WardSettings(
                name,
                areaComponent.m_radius));
        }
    }

    private static void AddWardThorward()
    {
        var name = "Thorward";
        var prefab = ZNetScene.instance.GetPrefab(name.GetStableHashCode());
        if (prefab)
            Reseter.wardsSettingsList.Add(new WardSettings(
                name,
                WardIsLovePlugin.WardRange().Value));
    }
}