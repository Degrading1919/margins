using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Margins.Editor
{
    [CustomEditor(typeof(ProceduralCommercialBuilding))]
    public sealed class ProceduralCommercialBuildingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProceduralCommercialBuilding building =
                (ProceduralCommercialBuilding)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graybox Generation", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate / Regenerate"))
                {
                    Generate(building, building.Request?.Seed ?? 1);
                }

                if (GUILayout.Button("Next Seed"))
                {
                    Generate(building, (building.Request?.Seed ?? 0) + 1);
                }

                if (GUILayout.Button("Clear"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(
                        building.gameObject,
                        "Clear Procedural Commercial Layout");
                    building.ClearGenerated();
                    EditorUtility.SetDirty(building);
                }
            }

            DrawResult(building);
        }

        private static void Generate(
            ProceduralCommercialBuilding building,
            int seed)
        {
            Undo.RegisterFullObjectHierarchyUndo(
                building.gameObject,
                "Generate Procedural Commercial Layout");
            bool success = building.RegenerateWithSeed(seed, out string error);
            EditorUtility.SetDirty(building);
            SceneView.RepaintAll();
            if (!success)
            {
                Debug.LogError(error, building);
            }
        }

        private static void DrawResult(ProceduralCommercialBuilding building)
        {
            ProceduralGenerationResult result = building.LastResult;
            if (result == null)
            {
                EditorGUILayout.HelpBox(
                    "Generate the layout to inspect its seed, zones, circulation, " +
                    "placement bounds, sockets, and validation results.",
                    MessageType.Info);
                return;
            }

            MessageType resultType = result.Success ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(building.LastGenerationSummary)
                    ? "No generation summary is available."
                    : building.LastGenerationSummary,
                resultType);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Seed", result.Seed.ToString());
            EditorGUILayout.LabelField("Archetype", result.Archetype.ToString());
            EditorGUILayout.LabelField("Footprint", result.Footprint?.Kind.ToString() ?? "None");
            EditorGUILayout.LabelField("Zones", result.Zones.Count.ToString());
            EditorGUILayout.LabelField("Circulation paths", result.Circulation.Count.ToString());
            EditorGUILayout.LabelField("Placements", result.Placements.Count.ToString());
            EditorGUILayout.LabelField(
                "Required clearances",
                result.Placements.Sum(item =>
                    item.Clearances.Count(clearance => clearance.Required)).ToString());
            EditorGUILayout.LabelField(
                "Socket placements",
                result.Placements.Count(item =>
                    item.MountingMode == ProceduralMountingMode.Socket).ToString());
            EditorGUILayout.LabelField("Signature", building.LastSignature ?? "None");
            EditorGUI.indentLevel--;

            foreach (ProceduralDiagnostic diagnostic in result.Diagnostics)
            {
                MessageType type = diagnostic.Severity switch
                {
                    ProceduralDiagnosticSeverity.Error => MessageType.Error,
                    ProceduralDiagnosticSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                EditorGUILayout.HelpBox(
                    $"{diagnostic.Code}: {diagnostic.Message}",
                    type);
            }
        }

        [MenuItem("Margins/Procedural Generation/Create Graybox Demo In Scene")]
        private static void CreateDemoInScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ProceduralCommercialGrayboxAssetSetup.DemoPrefabPath);
            if (prefab == null)
            {
                ProceduralCommercialGrayboxAssetSetup.Apply();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ProceduralCommercialGrayboxAssetSetup.DemoPrefabPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Procedural Graybox Demo");
            Selection.activeGameObject = instance;
        }
    }
}
