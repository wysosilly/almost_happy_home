using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Furniture: 기본은 그리드 큐브. 원하는 모델은 Inspector의 3D Model(선택)에 직접 할당.
/// - Reset All to Cube: 모든 가구에서 Model 자식 제거 → 큐브만 표시 (마젠타/이상한 형태 제거)
/// - Apply Mega Pack: (선택) 매핑된 4개만 Mega Pack 모델 적용
/// </summary>
public static class ApplyFurnitureMegaPackModels
{
    const string OurFurnitureFolder = "Assets/Furniture";
    const string MegaPackPrefix = "Assets/Furniture Mega Pack/Prefabs";

    // 우리 가구 프리팹 이름 -> Mega Pack 상대 경로 (폴더/이름.prefab)
    static readonly Dictionary<string, string> Mapping = new Dictionary<string, string>
    {
        { "Cabi_Bookshelf", "Drawers/Drawer01.prefab" },   // 책장: 서랍장형 선반
        { "Cabi_Fridge", "Kitchen/Refrigerator03.prefab" },
        { "Cabi_SideBoard", "Drawers/Drawer10.prefab" },   // 사이드보드/찬장
        { "Cabi_Wardrobe", "Closets/Closet01.prefab" },    // 옷장
    };

    /// <summary>Furniture 폴더 내 모든 가구: Model 자식 제거 + modelPrefab 비우기 → 전부 큐브 하나만. 원하는 가구만 Inspector 3D Model(선택)에 넣기.</summary>
    [MenuItem("Tools/Furniture/Reset All to Cube Only")]
    public static void ResetAllToCubeOnly()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { OurFurnitureFolder });
        int done = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith(OurFurnitureFolder)) continue;
            if (ResetSinglePrefabToCubeOnly(path)) done++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Furniture] Reset to cube only: {done} prefabs. 원하는 가구만 Inspector에서 3D Model(선택)에 할당하세요.");
    }

    /// <summary>한 프리팹: modelPrefab 비우기 + Model 자식 제거 → 큐브만 남김. 루트 메시 없으면 기본 Cube 할당.</summary>
    static bool ResetSinglePrefabToCubeOnly(string ourPrefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ourPrefabPath);
        if (root == null) return false;
        Furniture f = root.GetComponent<Furniture>();
        if (f == null) { PrefabUtility.UnloadPrefabContents(root); return false; }
        f.modelPrefab = null;
        Transform model = root.transform.Find("Model");
        if (model == null) model = root.transform.Find("FurnitureModel");
        if (model != null) Object.DestroyImmediate(model.gameObject);
        var mf = root.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh == null) {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
        }
        var mr = root.GetComponent<MeshRenderer>();
        if (mr != null && (mr.sharedMaterial == null || mr.sharedMaterial.name.Contains("Default-Material"))) {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (temp.GetComponent<Renderer>().sharedMaterial != null)
                mr.sharedMaterial = temp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(temp);
        }
        PrefabUtility.SaveAsPrefabAsset(root, ourPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }

    [MenuItem("Tools/Furniture/Apply Mega Pack Models (4개)")]
    public static void ApplyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { OurFurnitureFolder });
        int applied = 0, cleaned = 0;
        foreach (string guid in guids)
        {
            string ourPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(ourPath) || !ourPath.StartsWith(OurFurnitureFolder)) continue;
            string ourName = System.IO.Path.GetFileNameWithoutExtension(ourPath);
            if (Mapping.TryGetValue(ourName, out string megaRel))
            {
                if (ApplyModelToFurniturePrefab(ourPath, MegaPackPrefix + "/" + megaRel)) applied++;
            }
            else if (ResetSinglePrefabToCubeOnly(ourPath)) cleaned++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ApplyFurnitureMegaPackModels] Mega Pack 적용: {applied}, 그리드 큐브만 정리: {cleaned}");
    }

    /// <summary>가구 프리팹에서 Model/FurnitureModel 자식 제거. (Mega Pack 적용 시 기존 Model 제거용)</summary>
    static bool RemoveModelChildFromPrefab(string ourPrefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ourPrefabPath);
        if (root == null || root.GetComponent<Furniture>() == null) { if (root != null) PrefabUtility.UnloadPrefabContents(root); return false; }
        Transform model = root.transform.Find("Model");
        if (model == null) model = root.transform.Find("FurnitureModel");
        if (model == null) { PrefabUtility.UnloadPrefabContents(root); return false; }
        Object.DestroyImmediate(model.gameObject);
        PrefabUtility.SaveAsPrefabAsset(root, ourPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }

    static bool ApplyModelToFurniturePrefab(string ourPrefabPath, string megaPackPrefabPath)
    {
        GameObject ourRoot = PrefabUtility.LoadPrefabContents(ourPrefabPath);
        if (ourRoot == null) { Debug.LogWarning("Could not load: " + ourPrefabPath); return false; }

        Furniture f = ourRoot.GetComponent<Furniture>();
        if (f == null) { PrefabUtility.UnloadPrefabContents(ourRoot); return false; }

        GameObject megaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(megaPackPrefabPath);
        if (megaPrefab == null) { Debug.LogWarning("Mega Pack prefab not found: " + megaPackPrefabPath); PrefabUtility.UnloadPrefabContents(ourRoot); return false; }

        // 기존 Model 제거
        Transform oldModel = ourRoot.transform.Find("Model");
        if (oldModel == null) oldModel = ourRoot.transform.Find("FurnitureModel");
        if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

        // Mega Pack 프리팹 인스턴스를 자식으로 추가 후 Unpack → 우리 프리팹에 메시/머티리얼 완전 임베드
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(megaPrefab);
        modelInstance.name = "Model";
        modelInstance.transform.SetParent(ourRoot.transform, false);
        PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // 렌더러 바운드로 크기 계산
        Bounds bounds = ComputeBounds(modelInstance);
        if (bounds.size.x < 0.001f || bounds.size.y < 0.001f || bounds.size.z < 0.001f)
        {
            Debug.LogWarning("Model bounds too small: " + ourPrefabPath);
            Object.DestroyImmediate(modelInstance);
            PrefabUtility.UnloadPrefabContents(ourRoot);
            return false;
        }

        Vector2Int size = f.size;
        if (size.x < 1) size.x = 1;
        if (size.y < 1) size.y = 1;
        float visualHeight = f.visualHeight > 0.01f ? f.visualHeight : 1f;

        // 모델 중심을 피벗에 맞춤 (스케일 적용 전)
        modelInstance.transform.localPosition = -bounds.center;
        // 그리드 크기(size.x, visualHeight, size.y)에 맞게 스케일
        Vector3 scale = new Vector3(
            size.x / bounds.size.x,
            visualHeight / bounds.size.y,
            size.y / bounds.size.z
        );
        modelInstance.transform.localScale = scale;

        PrefabUtility.SaveAsPrefabAsset(ourRoot, ourPrefabPath);
        PrefabUtility.UnloadPrefabContents(ourRoot);
        return true;
    }

    static Bounds ComputeBounds(GameObject root)
    {
        Bounds b = new Bounds(root.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        if (first) return new Bounds(Vector3.zero, Vector3.one);
        return b;
    }
}
