namespace GroundReset;

[Serializable] internal class ChunkData
{
    public bool[] m_modifiedHeight;
    public float[] m_levelDelta;
    public float[] m_smoothDelta;
    public bool[] m_modifiedPaint;
    public Color[] m_paintMask;

    public ChunkData()
    {
        var num = Reseter.HeightmapWidth + 1;
        m_modifiedHeight = new bool[num * num];
        m_levelDelta = new float[m_modifiedHeight.Length];
        m_smoothDelta = new float[m_modifiedHeight.Length];
        m_modifiedPaint = new bool[Reseter.HeightmapWidth * Reseter.HeightmapWidth];
        m_paintMask = new Color[m_modifiedPaint.Length];
    }
}