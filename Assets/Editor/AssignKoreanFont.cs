using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 한글 폰트(NanumSquareRoundL SDF) 자동 적용 메뉴
/// </summary>
public static class AssignKoreanFont
{
    const string FONT_GUID = "2a85413532891024699ef0e2db12e02f";

    static TMPro.TMP_FontAsset GetKoreanFont()
    {
        var path = AssetDatabase.GUIDToAssetPath(FONT_GUID);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
    }

    [MenuItem("Tools/Assign Korean Font to Selection UI")]
    public static void Execute()
    {
        var font = GetKoreanFont();
        if (font == null)
        {
            Debug.LogError("NanumSquareRoundL SDF 폰트를 찾을 수 없습니다.");
            return;
        }

        int count = 0;
        foreach (var mgr in Object.FindObjectsOfType<FurnitureSelectionManager>(true))
        {
            var so = new SerializedObject(mgr);
            var prop = so.FindProperty("customFont");
            if (prop != null)
            {
                prop.objectReferenceValue = font;
                so.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }
        }
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"FurnitureSelectionManager {count}개에 한글 폰트 할당 완료. 씬을 저장(Ctrl+S)해주세요.");
        }
        else
        {
            Debug.LogWarning("씬에 FurnitureSelectionManager가 없습니다. SampleScene을 연 상태에서 실행해주세요.");
        }
    }

    /// <summary>씬에 있는 모든 TextMeshProUGUI에 한글 폰트 적용</summary>
    [MenuItem("Tools/Apply Korean Font to All TMP in Scene")]
    public static void ApplyToAllTMPInScene()
    {
        var font = GetKoreanFont();
        if (font == null)
        {
            Debug.LogError("NanumSquareRoundL SDF 폰트를 찾을 수 없습니다.");
            return;
        }

        int count = 0;
        foreach (var tmp in Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true))
        {
            if (tmp.font != font)
            {
                Undo.RecordObject(tmp, "Apply Korean Font");
                tmp.font = font;
                count++;
            }
        }
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"씬 내 TextMeshProUGUI {count}개에 한글 폰트 적용. 씬 저장(Ctrl+S) 권장.");
        }
        else
        {
            Debug.Log("적용할 TMP가 없거나 이미 한글 폰트가 적용되어 있습니다.");
        }
    }

    /// <summary>프로젝트 기본 폰트를 한글로 설정 (새로 만드는 UI에 자동 적용)</summary>
    [MenuItem("Tools/Set Default Font to Korean (TMP Settings)")]
    public static void SetTMPDefaultFontToKorean()
    {
        var font = GetKoreanFont();
        if (font == null)
        {
            Debug.LogError("NanumSquareRoundL SDF 폰트를 찾을 수 없습니다.");
            return;
        }

        var settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(settingsPath);
        if (settings == null)
        {
            settings = Resources.Load<TMPro.TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogError("TMP Settings 에셋을 찾을 수 없습니다.");
                return;
            }
            settingsPath = AssetDatabase.GetAssetPath(settings);
        }

        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("TMP Settings 기본 폰트를 한글(NanumSquareRoundL SDF)으로 설정했습니다. 새로 만드는 TextMeshPro에는 자동 적용됩니다.");
        }
    }
}
