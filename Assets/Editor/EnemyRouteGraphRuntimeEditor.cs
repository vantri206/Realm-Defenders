using UnityEditor;

[CustomEditor(typeof(EnemyRouteGraph))]
public class EnemyRouteGraphRuntimeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
