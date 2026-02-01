using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class StageDataEditor : EditorWindow
{
    private StageData targetStageData;

    [MenuItem("Tools/Stage Data Saver")]
    public static void ShowWindow()
    {
        GetWindow<StageDataEditor>("Stage Saver");
    }

    void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; }
    void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

    void OnSceneGUI(SceneView sceneView)
    {
        if (targetStageData == null) return;

        // 씬 뷰에 가이드 그리드 그리기
        Handles.color = new Color(1f, 1f, 1f, 0.3f);
        int w = targetStageData.gridWidth;
        int h = targetStageData.gridHeight;

        for (int i = 0; i <= w; i++)
            Handles.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, h));
        for (int i = 0; i <= h; i++)
            Handles.DrawLine(new Vector3(0, 0, i), new Vector3(w, 0, i));

        // 외곽선 표시
        Handles.color = new Color(1f, 0.9f, 0f, 0.5f);
        Vector3[] verts = new Vector3[] {
            new Vector3(0, 0, 0), new Vector3(w, 0, 0),
            new Vector3(w, 0, h), new Vector3(0, 0, h)
        };
        Handles.DrawSolidRectangleWithOutline(verts, new Color(1, 1, 1, 0.05f), Color.yellow);
        
        sceneView.Repaint();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Scene Furniture to StageData", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        targetStageData = (StageData)EditorGUILayout.ObjectField("Target StageData", targetStageData, typeof(StageData), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Capture & Save Scene Furnitures", GUILayout.Height(40)))
        {
            if (targetStageData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Target StageData asset!", "OK");
                return;
            }

            CaptureSceneToData();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Snap All Furniture to Grid", GUILayout.Height(30)))
        {
            SnapAllToGrid();
        }

        GUILayout.Space(20);
        EditorGUILayout.HelpBox("1. Place furniture prefabs in the scene.\n2. Use 'Snap All Furniture to Grid' to align them.\n3. Click 'Capture & Save' to save to the asset.", MessageType.Info);
    }

    void SnapAllToGrid()
    {
        Furniture[] furnitures = FindObjectsOfType<Furniture>();
        foreach (var f in furnitures)
        {
            Undo.RecordObject(f.transform, "Snap to Grid");
            Undo.RecordObject(f, "Snap to Grid");
            
            // 1. 현재 월드 위치에서 가장 가까운 '정수' 그리드 좌표를 찾습니다.
            Vector2Int fs = f.EffectiveSize;
            float ox = f.pivotAtCenter ? fs.x * 0.5f : 0;
            float oy = f.pivotAtCenter ? fs.y * 0.5f : 0;

            int gx = Mathf.RoundToInt(f.transform.position.x - ox);
            int gz = Mathf.RoundToInt(f.transform.position.z - oy);
            
            // 2. 정수 좌표를 할당합니다.
            f.gridPos = new Vector2Int(gx, gz);
            
            // 3. 가구 내부의 시각적 위치 업데이트 로직을 호출하여 
            // 피벗 설정에 상관없이 그리드 칸에 딱 맞게 이동시킵니다.
            f.UpdateVisualPosition();
            
            EditorUtility.SetDirty(f);
        }
        Debug.Log($"[StageSaver] {furnitures.Length}개의 가구를 정수 그리드 좌표에 맞춰 정렬했습니다.");
    }

    void CaptureSceneToData()
    {
        // 씬에 있는 모든 Furniture 컴포넌트 찾기
        Furniture[] furnitures = FindObjectsOfType<Furniture>();
        
        if (furnitures.Length == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No Furniture objects found in the scene!", "OK");
            return;
        }

        Undo.RecordObject(targetStageData, "Capture Scene to StageData");
        targetStageData.initialFurnitures.Clear();

        int savedCount = 0;
        foreach (var f in furnitures)
        {
            // 프리팹 소스 찾기
            GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(f.gameObject);
            
            if (prefabSource == null)
            {
                Debug.LogWarning($"[StageSaver] {f.name} is not a prefab instance. Skipping.");
                continue;
            }

            // 현재 씬의 월드 좌표를 기반으로 gridPos 갱신 (에디터에서 수동 배치했을 경우 대비)
            Vector2Int fs = f.EffectiveSize;
            float ox = f.pivotAtCenter ? fs.x * 0.5f : 0;
            float oy = f.pivotAtCenter ? fs.y * 0.5f : 0;
            Vector2Int currentGridPos = new Vector2Int(
                Mathf.FloorToInt(f.transform.position.x - ox),
                Mathf.FloorToInt(f.transform.position.z - oy)
            );

            FurnitureSpawnInfo info = new FurnitureSpawnInfo
            {
                prefab = prefabSource,
                gridPos = currentGridPos,
                rotationSteps = f.rotationSteps
            };

            targetStageData.initialFurnitures.Add(info);
            savedCount++;
        }

        EditorUtility.SetDirty(targetStageData);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", $"{savedCount} furniture(s) saved to '{targetStageData.stageName}'.", "OK");
    }
}
