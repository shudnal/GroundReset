using Marketplace.Modules.TerritorySystem;

namespace GroundReset.Compatibility.kgMarketplace;

public static class MarketplaceTerritorySystem_RAW
{
    public static bool PointInTerritory(Vector3 pos) => TerritorySystem_DataTypes.Territory.GetCurrentTerritory(pos) != null;
}