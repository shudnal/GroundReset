using System.Threading.Tasks;

// ReSharper disable PossibleLossOfFraction

namespace GroundReset;

public static class Terrains
{
    public static async Task<int> ResetTerrains(bool checkWards)
    {
        watch.Restart();
        var zdos = await ZoneSystem.instance.GetWorldObjectsAsync(terrCompPrefabName);
        Debug($"Found {zdos.Count} chunks to reset");
        var resets = 0;
        foreach (var zdo in zdos)
        {
            ResetTerrainComp(zdo, checkWards);
            resets++;
        }

        var totalSeconds = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds;
        Debug($"{resets} chunks have been reset. Took {totalSeconds} seconds");
        watch.Restart();

        return resets;
    }

    private static void ResetTerrainComp(ZDO zdo, bool checkWards)
    {
        var divider = dividerConfig.Value;
        var resetSmoothValue = resetSmoothing.Value;
        var resetSmoothingLastValue = resetSmoothing.Value;
        var resetPaintValue = resetPaint.Value;
        var resetPaintLastValue = resetPaintLast.Value;
        var minHeightToSteppedReset = minHeightToSteppedResetConfig.Value;
        var zoneCenter = ZoneSystem.instance.GetZonePos(ZoneSystem.instance.GetZone(zdo.GetPosition()));

        var data = LoadOldData(zdo);

        var num = HeightmapWidth + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;

            if (!data.m_modifiedHeight[idx]) continue;
            if (checkWards && IsInWard(zoneCenter, w, h)) continue;

            data.m_levelDelta[idx] /= divider;
            if (data.m_levelDelta[idx] < minHeightToSteppedReset) data.m_levelDelta[idx] = 0;
            if (resetSmoothValue && (resetSmoothingLastValue == false || data.m_levelDelta[idx] == 0))
            {
                data.m_smoothDelta[idx] /= divider;
                if (data.m_smoothDelta[idx] < minHeightToSteppedReset) data.m_smoothDelta[idx] = 0;
            }

            var flag_b = resetSmoothValue ? data.m_smoothDelta[idx] != 0 : false;
            data.m_modifiedHeight[idx] = data.m_levelDelta[idx] != 0 || flag_b;
        }

        if (resetPaintValue)
        {
            num = HeightmapWidth;
            var paintLenMun1 = data.m_modifiedPaint.Length - 1;
            for (var h = 0; h < num; h++)
            for (var w = 0; w < num; w++)
            {
                var idx = h * num + w;
                if (idx > paintLenMun1) continue;
                if (!data.m_modifiedPaint[idx]) continue;
                if (checkWards && IsInWard(zoneCenter, w, h)) continue;
                if (data.m_modifiedHeight.Length > idx + 1 &&
                    data.m_modifiedHeight[idx] &&
                    resetPaintLastValue) continue;

                data.m_modifiedPaint[idx] = false;
                data.m_paintMask[idx] = Color.clear;
            }
        }


        SaveData(zdo, data);

        ClutterSystem.instance?.ResetGrass(zdo.GetPosition(), HeightmapWidth * HeightmapScale / 2);

        foreach (var comp in TerrainComp.s_instances)
            comp.m_hmap?.Poke(false);
    }

    private static void SaveData(ZDO zdo, ChunkData data)
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

    private static ChunkData LoadOldData(ZDO zdo)
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
}