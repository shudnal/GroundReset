using System.Threading.Tasks;

namespace GroundReset;

[HarmonyPatch]
public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))] [HarmonyPostfix]
    private static void AddCommands()
    {
        new ConsoleCommand("ResetAllChunks",
            "Resets all chunks in world. ResetAllChunks [check wards]", args =>
            {
                RunCommand(args =>
                {
                    if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");
                    if (args.Length < 2 || !bool.TryParse(args[1], out var checkWards))
                        throw new ConsoleCommandException(
                            "First argument - checkWards - must be a boolean: true or false");
                    ResetAll(false, checkWards);

                    args.Context.AddString("Processing...");
                }, args);
            }, true, optionsFetcher: () => new List<string> { "true", "false" });

        new ConsoleCommand("resetAllChunksTimers",
            "", args =>
            {
                var terrCompHash = "_TerrainCompiler".GetStableHashCode();
                RunCommand(args =>
                {
                    if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                    args.Context.AddString("Processing...");
                    var zdos = ZDOMan.instance.m_objectsByID.Values.Where(x => x.GetPrefab() == terrCompHash).ToList();
                    foreach (var zdo in zdos) zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                    args.Context.AddString("Done");
                }, args);
            }, true);

        new ConsoleCommand("InsidePlayerArea",
            "", args =>
            {
                RunCommand(args =>
                {
                    args.Context.AddString(PrivateArea.InsideFactionArea(Player.m_localPlayer.transform.position,
                        Character.Faction.Players)
                        ? "true"
                        : "false");
                }, args);
            }, true);
        new ConsoleCommand("GoThroughHeightmap",
            "", args => RunCommand(args => GoThroughHeightmap(), args), true);
    }

    private static async void GoThroughHeightmap()
    {
        var comp = TerrainComp.FindTerrainCompiler(Player.m_localPlayer.transform.position);
        var zoneCenter =
            ZoneSystem.instance.GetZonePos(ZoneSystem.instance.GetZone(comp.m_nview.GetZDO().GetPosition()));
        var skyLine = new GameObject("SkyLine").AddComponent<LineRenderer>();
        skyLine.positionCount = 2;
        skyLine.material = new Material(Shader.Find("Unlit/Color"));
        skyLine.material.name = "UnlitColor Material";
        skyLine.material.color = Color.white;

        var num = HeightmapWidth + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;
            var worldPos = HmapToWorld(zoneCenter, w, h);
            var inWard = PrivateArea.InsideFactionArea(worldPos, Character.Faction.Players);
            skyLine.material.color = !inWard ? Color.green : Color.red;
            skyLine.SetPosition(0, worldPos);
            skyLine.SetPosition(1, worldPos with { y = 10000 });
            await Task.Delay(10);
        }

        Destroy(skyLine.gameObject);
    }
}