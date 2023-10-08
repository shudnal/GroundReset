using System.Diagnostics;
using System.Threading.Tasks;

namespace GroundReset;

internal static class Reseter
{
    internal static readonly int HeightmapWidth = 64;
    internal static readonly int HeightmapScale = 1;

    internal static async void ResetAllTerrains(bool checkIfNeed = true, bool ranFromConsole = false)
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
                ResetTerrainComp(zdo);
                resets++;
            }
        }

        Debug($"Сброшено {resets} чанков, took {watch.ElapsedMilliseconds} ms", true);
        if (ranFromConsole) Console.instance.AddString("<color=green> Done </color>");
    }

    internal static bool IsNeedToReset(ZDO zdo)
    {
        var savedTime = zdo.GetString($"{ModName} time");
        if (!savedTime.IsGood()) return false;

        var flag = savedTime == lastReset.ToString();
        return !flag;
    }

    internal static void ResetTerrainComp(ZDO zdo /*, bool checkZones = true*/)
    {
        var resets = 0;
        bool[] m_modifiedHeight;
        float[] m_levelDelta;
        float[] m_smoothDelta;
        bool[] m_modifiedPaint;
        Color[] m_paintMask;
        int width;
        int scale;
        var zoneCenter = instance.GetZonePos(instance.GetZone(zdo.GetPosition()));

        LoadOldData(zdo, out m_modifiedHeight, out m_levelDelta, out m_smoothDelta, out m_modifiedPaint,
            out m_paintMask,
            out width, out scale);

        var num = width + 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;

            if (!m_modifiedHeight[idx]) continue;

            var worldPos = VertexToWorld(zoneCenter, w, h);
            var inWard = PrivateArea.InsideFactionArea(worldPos, Character.Faction.Players);
            if (inWard) continue;

            resets++;
            m_modifiedHeight[idx] = false;
            m_levelDelta[idx] = 0;
            m_smoothDelta[idx] = 0;
        }

        num = width;
        var paintLenMun1 = m_modifiedPaint.Length - 1;
        for (var h = 0; h < num; h++)
        for (var w = 0; w < num; w++)
        {
            var idx = h * num + w;
            if (idx > paintLenMun1) continue;
            if (!m_modifiedPaint[idx]) continue;

            var worldPos = VertexToWorld(zoneCenter, w, h);
            var inWard = PrivateArea.InsideFactionArea(worldPos, Character.Faction.Players);
            if (inWard) continue;

            m_modifiedPaint[idx] = false;
            m_paintMask[idx] = Color.clear;
        }


        SaveData();

        ClutterSystem.instance?.ResetGrass(zdo.GetPosition(), width * scale / 2);
        zdo.Set($"{ModName} time", lastReset.ToString());

        foreach (var comp in TerrainComp.s_instances)
            if (pokeCompConfig.Value)
                comp.m_hmap?.Poke(false);

        void SaveData()
        {
            var package = new ZPackage();
            package.Write(1);
            package.Write(0); //m_operations
            package.Write(Vector3.zero); //m_lastOpPoint
            package.Write(0); //m_lastOpRadius
            package.Write(m_modifiedHeight.Length);
            for (var index = 0; index < m_modifiedHeight.Length; ++index)
            {
                package.Write(m_modifiedHeight[index]);
                if (m_modifiedHeight[index])
                {
                    package.Write(m_levelDelta[index]);
                    package.Write(m_smoothDelta[index]);
                }
            }

            package.Write(m_modifiedPaint.Length);
            for (var index = 0; index < m_modifiedPaint.Length; ++index)
            {
                package.Write(m_modifiedPaint[index]);
                if (m_modifiedPaint[index])
                {
                    package.Write(m_paintMask[index].r);
                    package.Write(m_paintMask[index].g);
                    package.Write(m_paintMask[index].b);
                    package.Write(m_paintMask[index].a);
                }
            }

            var bytes = Utils.Compress(package.GetArray());
            zdo.Set(ZDOVars.s_TCData, bytes);
        }
    }

    internal static void LoadOldData(ZDO zdo,
        out bool[] m_modifiedHeight,
        out float[] m_levelDelta,
        out float[] m_smoothDelta,
        out bool[] m_modifiedPaint,
        out Color[] m_paintMask,
        out int width,
        out int scale)
    {
        byte[] byteArray = zdo.GetByteArray(ZDOVars.s_TCData);
        if (byteArray == null) throw new Exception("No data found, byteArray == null");
        ZPackage zpackage = new ZPackage(Utils.Decompress(byteArray));
        zpackage.ReadInt();
        width = HeightmapWidth;
        scale = HeightmapScale;
        var m_operations = zpackage.ReadInt(); //m_operations
        var m_lastOpPoint = zpackage.ReadVector3(); //m_lastOpPoint
        var m_lastOpRadius = zpackage.ReadSingle(); //m_lastOpRadius
        var num1 = zpackage.ReadInt(); //m_modifiedHeight.Length

        var num = width + 1;
        m_modifiedHeight = new bool[num * num];
        m_levelDelta = new float[num * num];
        m_smoothDelta = new float[num * num];

        for (var index = 0; index < width; ++index)
        {
            m_modifiedHeight[index] = zpackage.ReadBool();
            if (m_modifiedHeight[index])
            {
                m_levelDelta[index] = zpackage.ReadSingle();
                m_smoothDelta[index] = zpackage.ReadSingle();
            } else
            {
                m_levelDelta[index] = 0.0f;
                m_smoothDelta[index] = 0.0f;
            }
        }

        var num2 = zpackage.ReadInt();
        m_modifiedPaint = new bool[width * width];
        m_paintMask = new Color[width * width];
        for (var index = 0; index < num2; ++index)
        {
            m_modifiedPaint[index] = zpackage.ReadBool();
            if (m_modifiedPaint[index])
                m_paintMask[index] = new Color
                {
                    r = zpackage.ReadSingle(),
                    g = zpackage.ReadSingle(),
                    b = zpackage.ReadSingle(),
                    a = zpackage.ReadSingle()
                };
            else m_paintMask[index] = Color.black;
        }

        Debug(
            $"Chunk data loaded. m_operations = {m_operations}, m_lastOpPoint = {m_lastOpPoint}, m_lastOpRadius = {m_lastOpRadius}, "
            + $"m_modifiedHeight.Length = {num1}, m_modifiedPaint.Length = {num2}, m_paintMask.Length = {num2}\n"
            + $"m_modifiedHeight.Any is true = {m_modifiedHeight.Any(x => x == true)}");
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