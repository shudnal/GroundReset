namespace GroundReset;

internal record WardSettings(string prefabName, float radius)
{
    public string prefabName = prefabName;
    public float radius = radius;
}