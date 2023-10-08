﻿using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Compatibility.WardIsLove;

public class WardIsLovePlugin : ModCompat
{
    private const string GUID = "Azumatt.WardIsLove";
    private static readonly System.Version MinVersion = new(2, 3, 3);

    public static Type ClassType() { return Type.GetType("WardIsLove.WardIsLovePlugin, WardIsLove"); }

    public static bool IsLoaded()
    {
        return Chainloader.PluginInfos.ContainsKey(GUID) &&
               Chainloader.PluginInfos[GUID].Metadata.Version >= MinVersion;
    }

    public static ConfigEntry<bool> WardEnabled()
    {
        return GetField<ConfigEntry<bool>>(ClassType(), null, "WardEnabled");
    }
}