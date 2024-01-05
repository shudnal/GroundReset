﻿using GroundReset.Compatibility.WardIsLove;
using UnityEngine.SceneManagement;

namespace GroundReset.Patch;

[HarmonyPatch] internal class InitWardsSettings
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))] [HarmonyPostfix] [HarmonyWrapSafe]
    internal static void Init(ZNetScene __instance)
    {
        if (SceneManager.GetActiveScene().name != "main") return;
        if (!ZNet.instance.IsServer()) return;

        wardsSettingsList = new List<WardSettings>();

        AddWard("guard_stone");
        AddWardThorward();
    }

    private static void AddWard(string name)
    {
        var prefab = ZNetScene.instance.GetPrefab(name.GetStableHashCode());
        if (!prefab) return;

        var areaComponent = prefab.GetComponent<PrivateArea>();
        wardsSettingsList.Add(new WardSettings(name, areaComponent.m_radius));
    }

    private static void AddWardThorward()
    {
        var name = "Thorward";
        var prefab = ZNetScene.instance.GetPrefab(name.GetStableHashCode());
        if (!prefab) return;
        wardsSettingsList.Add(new WardSettings(name, WardIsLovePlugin.WardRange().Value));
    }
}