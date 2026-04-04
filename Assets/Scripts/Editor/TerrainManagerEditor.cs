using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor
{
  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();

    TerrainManager terrainManager = (TerrainManager)target;

    GUILayout.Space(10);

    if (GUILayout.Button("Generate Terrain"))
      terrainManager.GenerateEditor();
  }
}
