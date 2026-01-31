using UnityEngine;
using UnityEditor;

public static class IsometricCameraSetup
{
    [MenuItem("Window/Apply Isometric Camera")]
    public static void ApplyIsometricCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Object.FindFirstObjectByType<Camera>();
            if (mainCam == null)
            {
                Debug.LogError("씬에 카메라가 없습니다.");
                return;
            }
        }

        Undo.RecordObject(mainCam, "Apply Isometric Camera");
        Undo.RecordObject(mainCam.transform, "Apply Isometric Camera Transform");

        mainCam.orthographic = true;
        mainCam.orthographicSize = 8f;
        mainCam.transform.position = new Vector3(0, 12, 12);
        mainCam.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);

        EditorUtility.SetDirty(mainCam);
        EditorUtility.SetDirty(mainCam.transform);
    }
}
