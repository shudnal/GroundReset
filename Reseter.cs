using System.Diagnostics;
using System.Threading.Tasks;

namespace GroundReset;

internal static class Reseter
{
    internal static readonly int HeightmapWidth = 64;
    internal static readonly int HeightmapScale = 1;

    internal static async void ResetAllTerrains(bool checkIfNeed = true,
        bool checkWards = true,
        bool ranFromConsole = false)
    {
        var terrCompHash = "_TerrainCompiler".GetStableHashCode();

        var watch = new Stopwatch();
        watch.Restart();
        var result = await Task.Run(() =>
        {
            var result = new List<ZDO>();
            var zdos = ZDOMan.instance.m_objectsByID.Values.Where(x => x.GetPrefab() == terrCompHash).ToList();
            foreach (var zdo in zdos)
            {
                var flag = true;
                if (checkIfNeed) flag = IsNeedToReset(zdo);
                if (!flag) continue;
                result.Add(zdo);
            }

            return result;
        });
        Debug(
            $"ResetAllTerrains first task completed, took {watch.ElapsedMilliseconds} ms, result.Count = '{result.Count}'",
            true);

        watch.Restart();
        var resets = 0;
        if (result.Count > 0)
        {
            lastReset = DateTime.Now;
            foreach (var zdo in result)
            {
                ResetTerrainComp(zdo, checkWards);
                resets++;
            }
        }

        Debug($"Have been reset {resets} chunks, took {watch.ElapsedMilliseconds} ms", true);
        if (ranFromConsole) Console.instance.AddString("<color=green> Done </color>");
    }

    internal static bool IsNeedToReset(ZDO zdo)
    {
        var savedTime = zdo.GetString($"{ModName} time");
        if (!savedTime.IsGood()) return false;

        var flag = savedTime == lastReset.ToString();
        return !flag;
    }

    internal static void ResetTerrainComp(ZDO zdo, bool checkWards)
    {
        var resets = 0;
        var zoneCenter = instance.GetZonePos(instance.GetZone(zdo.GetPosition()));

        var data = LoadOldData(zdo);

        var num = HeightmapWidth + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;

            if (!data.m_modifiedHeight[idx]) continue;

            if (checkWards)
            {
                var worldPos = VertexToWorld(zoneCenter, w, h);
                var inWard = worldPos.InsideAnyActiveFactionArea(Character.Faction.Players);
                if (inWard) continue;
            }

            resets++;
            data.m_modifiedHeight[idx] = false;
            data.m_levelDelta[idx] = 0;
            data.m_smoothDelta[idx] = 0;
        }

        num = HeightmapWidth;
        var paintLenMun1 = data.m_modifiedPaint.Length - 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;
            if (idx > paintLenMun1) continue;
            if (!data.m_modifiedPaint[idx]) continue;

            if (checkWards)
            {
                var worldPos = VertexToWorld(zoneCenter, w, h);
                var inWard = worldPos.InsideAnyActiveFactionArea(Character.Faction.Players);
                if (inWard) continue;
            }

            data.m_modifiedPaint[idx] = false;
            data.m_paintMask[idx] = Color.clear;
        }


        SaveData(zdo, data);

        ClutterSystem.instance?.ResetGrass(zdo.GetPosition(), HeightmapWidth * HeightmapScale / 2);
        zdo.Set($"{ModName} time", lastReset.ToString());

        foreach (var comp in TerrainComp.s_instances)
            if (pokeCompConfig.Value)
                comp.m_hmap?.Poke(false);
    }

    internal static void SaveData(ZDO zdo, ChunkData data)
    {
        var package = new ZPackage();
        package.Write(1);
        package.Write(0); //m_operations
        package.Write(Vector3.zero); //m_lastOpPoint
        package.Write(0); //m_lastOpRadius
        package.Write(data.m_modifiedHeight.Length);
        for (var index = 0; index < data.m_modifiedHeight.Length; ++index)
        {
            package.Write(data.m_modifiedHeight[index]);
            if (data.m_modifiedHeight[index])
            {
                package.Write(data.m_levelDelta[index]);
                package.Write(data.m_smoothDelta[index]);
            }
        }

        package.Write(data.m_modifiedPaint.Length);
        for (var index = 0; index < data.m_modifiedPaint.Length; ++index)
        {
            package.Write(data.m_modifiedPaint[index]);
            if (data.m_modifiedPaint[index])
            {
                package.Write(data.m_paintMask[index].r);
                package.Write(data.m_paintMask[index].g);
                package.Write(data.m_paintMask[index].b);
                package.Write(data.m_paintMask[index].a);
            }
        }

        var bytes = Utils.Compress(package.GetArray());
        zdo.Set(ZDOVars.s_TCData, bytes);
    }

    internal static ChunkData LoadOldData(ZDO zdo)
    {
        var chunkData = new ChunkData();
        var num = HeightmapWidth + 1;
        var byteArray = zdo.GetByteArray(ZDOVars.s_TCData);
        var zPackage = new ZPackage(Utils.Decompress(byteArray));
        zPackage.ReadInt();
        zPackage.ReadInt();
        zPackage.ReadVector3();
        zPackage.ReadSingle();
        var num1 = zPackage.ReadInt();
        if (num1 != chunkData.m_modifiedHeight.Length) DebugWarning("Terrain data load error, height array missmatch");

        for (var index = 0; index < num1; ++index)
        {
            chunkData.m_modifiedHeight[index] = zPackage.ReadBool();
            if (chunkData.m_modifiedHeight[index])
            {
                chunkData.m_levelDelta[index] = zPackage.ReadSingle();
                chunkData.m_smoothDelta[index] = zPackage.ReadSingle();
            } else
            {
                chunkData.m_levelDelta[index] = 0.0f;
                chunkData.m_smoothDelta[index] = 0.0f;
            }
        }

        Debug(
            $"m_modifiedHeight ="
            + $" {(chunkData.m_modifiedHeight.Any(x => x) ? "Has true" : "Only false")}, \n\n");
        // + $" { chunkData.m_modifiedHeight.GetString()}, \n\n"
        //   + $"m_levelDelta = {chunkData.m_levelDelta.GetString()}, \n\nm_smoothDelta = {m_smoothDelta.GetString()}");

        var num2 = zPackage.ReadInt();
        for (var index = 0; index < num2; ++index)
        {
            chunkData.m_modifiedPaint[index] = zPackage.ReadBool();
            if (chunkData.m_modifiedPaint[index])
                chunkData.m_paintMask[index] = new Color
                {
                    r = zPackage.ReadSingle(),
                    g = zPackage.ReadSingle(),
                    b = zPackage.ReadSingle(),
                    a = zPackage.ReadSingle()
                };
            else chunkData.m_paintMask[index] = Color.black;
        }

        return chunkData;
    }

    public static Vector3 VertexToWorld(Vector3 heightmapPos, int x, int y)
    {
        var xPos = ((float)x - HeightmapWidth / 2) * HeightmapScale;
        var zPos = ((float)y - HeightmapWidth / 2) * HeightmapScale;
        return heightmapPos + new Vector3(xPos, 0f, zPos);
    }

    public static void WorldToVertex(Vector3 worldPos, Vector3 heightmapPos, out int x, out int y)
    {
        var vector3 = worldPos - heightmapPos;
        x = Mathf.FloorToInt((float)(vector3.x / (double)HeightmapScale + 0.5)) + HeightmapWidth / 2;
        y = Mathf.FloorToInt((float)(vector3.z / (double)HeightmapScale + 0.5)) + HeightmapWidth / 2;
    }

    private static float CoordDistance(float x, float y, float rx, float ry)
    {
        var num = x - rx;
        var num2 = y - ry;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    public static IEnumerator SaveTime()
    {
        yield return new WaitForSeconds(savedTimeUpdateInterval);

        timePassedInMinutesConfig.Value = timer.Timer / 60;
        ZNetScene.instance.StartCoroutine(SaveTime());
    }
}