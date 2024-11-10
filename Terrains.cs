﻿using System.Text;
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
            await ResetTerrainComp(zdo, checkWards);
            resets++;
        }

        var totalSeconds = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds).TotalSeconds;
        Debug($"{resets} chunks have been reset. Took {totalSeconds} seconds");
        watch.Restart();

        return resets;
    }

    private static async Task ResetTerrainComp(ZDO zdo, bool checkWards)
    {
        var divider = dividerConfig.Value;
        var resetSmooth = resetSmoothingConfig.Value;
        var resetSmoothingLast = resetSmoothingConfig.Value;
        var minHeightToSteppedReset = minHeightToSteppedResetConfig.Value;
        var zoneCenter = GetZonePos(GetZone(zdo.GetPosition()));

        ChunkData data = null;
        try
        {
            data = LoadOldData(zdo);
        }
        catch (Exception e)
        {
            DebugError(e);
            DebugError(debugSb.ToString());
            return;
        }

        if (data == null) return;

        var num = HeightmapWidth + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;

            if (!data.m_modifiedHeight[idx]) continue;
            if (checkWards && IsInWard(zoneCenter, w, h)) continue;

            data.m_levelDelta[idx] /= divider;
            if (Abs(data.m_levelDelta[idx]) < minHeightToSteppedReset) data.m_levelDelta[idx] = 0;
            if (resetSmooth && (resetSmoothingLast == false || data.m_levelDelta[idx] == 0))
            {
                data.m_smoothDelta[idx] /= divider;
                if (Abs(data.m_smoothDelta[idx]) < minHeightToSteppedReset) data.m_smoothDelta[idx] = 0;
            }

            var flag_b = resetSmooth && data.m_smoothDelta[idx] != 0;
            data.m_modifiedHeight[idx] = data.m_levelDelta[idx] != 0 || flag_b;
        }


        num = HeightmapWidth;
        var paintLenMun1 = data.m_modifiedPaint.Length - 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;
            if (idx > paintLenMun1) continue;
            if (!data.m_modifiedPaint[idx]) continue;
            if (checkWards || resetPaintLast)
            {
                var worldPos = HmapToWorld(zoneCenter, w, h);
                if (checkWards && IsInWard(worldPos)) continue;
                if (resetPaintLast)
                {
                    WorldToVertex(worldPos, zoneCenter, out var x, out var y);
                    var heightIdx = y * (HeightmapWidth + 1) + x;
                    if (data.m_modifiedHeight.Length > heightIdx && data.m_modifiedHeight[heightIdx]) continue;
                }
            }

            var currentPaint = data.m_paintMask[idx];
            if (debug_test) Debug($"currentPaint = {currentPaint}");
            if (IsPaintIgnored(currentPaint)) continue;

            data.m_modifiedPaint[idx] = false;
            data.m_paintMask[idx] = Color.clear;
        }

        await SaveData(zdo, data);

        ClutterSystem.instance?.ResetGrass(zoneCenter, HeightmapWidth * HeightmapScale / 2);

        foreach (var comp in TerrainComp.s_instances) comp.m_hmap?.Poke(false);
    }

    private static bool IsPaintIgnored(Color color)
    {
        return paintsToIgnore.Exists(x =>
            Abs(x.r - color.r) < paintsCompairTolerance &&
            Abs(x.b - color.b) < paintsCompairTolerance &&
            Abs(x.g - color.g) < paintsCompairTolerance &&
            Abs(x.a - color.a) < paintsCompairTolerance);
    }

    private static async Task SaveData(ZDO zdo, ChunkData data)
    {
        var package = new ZPackage();
        package.Write(1);
        package.Write(data.m_operations);
        package.Write(data.m_lastOpPoint);
        package.Write(data.m_lastOpRadius);
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
        await Task.Yield();
    }

    private static readonly StringBuilder debugSb = new();

    private static ChunkData LoadOldData(ZDO zdo)
    {
        debugSb.Clear();
        var chunkData = new ChunkData();
        var byteArray = zdo.GetByteArray(ZDOVars.s_TCData);
        if (byteArray == null)
        {
            Debug("ByteArray is null, aborting chunk load");
            return null;
        }

        var zPackage = new ZPackage(Utils.Decompress(byteArray));
        zPackage.ReadInt();
        chunkData.m_operations = zPackage.ReadInt();
        chunkData.m_lastOpPoint = zPackage.ReadVector3();
        chunkData.m_lastOpRadius = zPackage.ReadSingle();
        var num1 = zPackage.ReadInt();
        if (num1 != chunkData.m_modifiedHeight.Length)
        {
            DebugWarning("Terrain data load error, height array missmatch");
            return null;
        }

        var msg =
            $"num1 = {num1}, modifiedHeight = {chunkData.m_modifiedHeight.Length}, levelDelta = {chunkData.m_levelDelta.Length}, smoothDelta = {chunkData.m_smoothDelta.Length}";
        debugSb.AppendLine(msg);

        //ok
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

        msg = $"num2 = {num2}, modifiedPaint = {chunkData.m_modifiedPaint.Length}, paintMask = {chunkData.m_paintMask.Length}";
        debugSb.AppendLine(msg);

        if (num2 != chunkData.m_modifiedPaint.Length)
        {
            if (debug_paintArrayMissmatch) DebugWarning("Terrain data load error, paint array missmatch");
            num2 = Min(num2, chunkData.m_modifiedPaint.Length, chunkData.m_paintMask.Length);
        }

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

        var flag_copyColor = num2 == HeightmapWidth * HeightmapWidth;
        msg = $"flag_copyColor = {flag_copyColor}";
        debugSb.AppendLine(msg);

        if (flag_copyColor)
        {
            var colorArray = new Color[chunkData.m_paintMask.Length];
            chunkData.m_paintMask.CopyTo(colorArray, 0);
            var flagArray = new bool[chunkData.m_modifiedPaint.Length];
            chunkData.m_modifiedPaint.CopyTo(flagArray, 0);
            var num3 = HeightmapWidth + 1;
            msg = $"num3 = {num3}";
            debugSb.AppendLine(msg);
            for (var index1 = 0; index1 < chunkData.m_paintMask.Length; ++index1)
            {
                var num4 = index1 / num3;
                var num5 = (index1 + 1) / num3;
                var index2 = index1 - num4;
                if (num4 == HeightmapWidth)
                    index2 -= HeightmapWidth;
                if (index1 > 0 && (index1 - num4) % HeightmapWidth == 0 && (index1 + 1 - num5) % HeightmapWidth == 0)
                    --index2;
                chunkData.m_paintMask[index1] = colorArray[index2];
                chunkData.m_modifiedPaint[index1] = flagArray[index2];
            }
        }

        return chunkData;
    }
}