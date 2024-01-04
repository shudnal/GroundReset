using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Marketplace;
using Marketplace.Modules.TerritorySystem;
using TMPro;
using static Marketplace.Modules.TerritorySystem.TerritorySystem_DataTypes;

namespace GroundReset.Compatibility.kgMarketplace;

public class MarketplaceTerritorySystem : ModCompat
{
    private const string GUID = "MarketplaceAndServerNPCs";

    public static Type TerritoryType() =>
        Type.GetType("Marketplace.Modules.TerritorySystem.TerritorySystem_DataTypes+Territory");

    public static Type TerritorySystemType() =>
        Type.GetType("Marketplace.Modules.TerritorySystem.TerritorySystem_Main_Client, Marketplace");

    public static bool IsLoaded() => Chainloader.PluginInfos.ContainsKey(GUID);

    public static bool PointInTerritory(Vector3 pos)
    {
        DebugWarning(
            $"TerritoryType = {TerritoryType()?.Name ?? "null"}, should be {typeof(Territory).FullName}\nIsLoaded() = {IsLoaded()}"
            + $"\nTerritorySystemType = {TerritorySystemType()?.Name ?? "null"}, should be {typeof(TerritorySystem_Main_Client).FullName}");
        if (IsLoaded() == false) return false;

        // var dictionary = GetField<Dictionary<TerritoryFlags, List<object>>>
        //     (TerritorySystemType(), null, "TerritoriesByFlags"); 
        // foreach (var territories in dictionary.Values)
        // foreach (object territory in territories)
        //     return InvokeMethod<bool>(TerritoryType(), territory, "IsInside", new object[] { pos });

        return false;
    }
}