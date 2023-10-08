using System.Threading.Tasks;

namespace GroundReset;

[HarmonyPatch]
public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))] [HarmonyPostfix]
    private static void AddCommands()
    {
        new ConsoleCommand("ResetAllTerrains",
            "Resets all terrains in world. ResetAllTerrains [check wards] [check zones]", args =>
            {
                mod.RunCommand(args =>
                {
                    if (!mod.IsAdmin) throw new Exception("You are not an admin on this server");
                    Reseter.ResetAllTerrains(false, true);

                    args.Context.AddString("Processing...");
                }, args);
            }, isCheat: true);

        new ConsoleCommand("ResetCurrentChunk",
            "Resets all player chunk. ResetCurrentChunk [check wards] [check zones]", args =>
            {
                mod.RunCommand(args =>
                {
                    if (!mod.IsAdmin) throw new Exception("You are not an admin on this server");
                    var comp = TerrainComp.FindTerrainCompiler(Player.m_localPlayer.transform.position);
                    Reseter.ResetTerrainComp(comp
                        .m_nview.GetZDO());

                    args.Context.AddString("Processing...");
                }, args);
            }, isCheat: true);
        new ConsoleCommand("resetAllChunksTimers",
            "", args =>
            {
                var terrCompHash = "_TerrainCompiler".GetStableHashCode();
                mod.RunCommand(args =>
                {
                    if (!mod.IsAdmin) throw new Exception("You are not an admin on this server");

                    args.Context.AddString("Processing...");
                    var zdos = ZDOMan.instance.m_objectsByID.Values.Where(x => x.GetPrefab() == terrCompHash).ToList();
                    foreach (var zdo in zdos) zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                    args.Context.AddString("Done");
                }, args);
            }, isCheat: true);
        new ConsoleCommand("InsidePlayerArea",
            "", args =>
            {
                mod.RunCommand(args =>
                {
                    args.Context.AddString(PrivateArea.InsideFactionArea(Player.m_localPlayer.transform.position,
                        Character.Faction.Players)
                        ? "true"
                        : "false");
                }, args);
            }, isCheat: true);
        new ConsoleCommand("GoThroughHeightmap",
            "", args =>
            {
                mod.RunCommand(args =>
                {
                    GoThroughHeightmap();
                }, args);
            }, isCheat: true);
    }

    private static async void GoThroughHeightmap()
    {
        var comp = TerrainComp.FindTerrainCompiler(Player.m_localPlayer.transform.position);
        var zoneCenter = instance.GetZonePos(instance.GetZone(comp.m_nview.GetZDO().GetPosition()));
        var skyLine = new GameObject("SkyLine").AddComponent<LineRenderer>();
        skyLine.positionCount = 2;
        skyLine.material = new(Shader.Find("Unlit/Color"));
        skyLine.material.name = "UnlitColor Material";
        skyLine.material.color = Color.white;

        var num = Reseter.HeightmapWidth + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;
            var worldPos = Reseter.VertexToWorld(zoneCenter, w, h);
            var inWard = PrivateArea.InsideFactionArea(worldPos, Character.Faction.Players);
            skyLine.material.color = !inWard ? Color.green : Color.red;
            skyLine.SetPosition(0, worldPos);
            skyLine.SetPosition(1, worldPos with { y = 10000 });
            await Task.Delay(10);
        }
    }
}