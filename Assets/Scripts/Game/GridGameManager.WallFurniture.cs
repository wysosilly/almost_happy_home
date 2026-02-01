using UnityEngine;
using System.Collections.Generic;

public partial class GridGameManager : MonoBehaviour
{
    void HandleWallDrag(Furniture f, RaycastHit hit)
    {
        f.isAttachedToWall = true;
        
        // 1. 벽면 좌표: Y(높이) 그리드 라인이 -halfH, -halfH+0.5…(0.25, 0.75…)이므로 Y만 0.25 오프셋 스냅
        Vector3 localPos = hit.collider.transform.InverseTransformPoint(hit.point);
        Vector3 scale = hit.collider.transform.lossyScale;
        const float gridStep = 0.5f;
        const float yGridOffset = 0.25f; // Y 그리드 라인에 가장자리 맞춤

        float worldY = localPos.y * scale.y;
        float worldZ = localPos.z * scale.z;
        float snappedWorldY = yGridOffset + Mathf.Round((worldY - yGridOffset) / gridStep) * gridStep;
        float snappedWorldZ = Mathf.Round(worldZ / gridStep) * gridStep;
        float snappedY = snappedWorldY / scale.y;
        float snappedZ = snappedWorldZ / scale.z;

        // 2. 시각적 변환 (얇게 만들기)
        Vector3 wallScale = f.originalScale;
        wallScale.x = f.wallThickness; 
        f.transform.localScale = wallScale;
        
        // 3. 위치 및 회전 설정
        f.transform.rotation = hit.collider.transform.rotation;
        // 벽 면에서 살짝 띄워서 겹침 방지 (0.06f)
        f.transform.position = hit.collider.transform.TransformPoint(new Vector3(0.06f, snappedY, snappedZ));
        
        // 정보 저장
        f.wallGridPos = new Vector2(snappedZ, snappedY);

        // 가이드: 그리드와 동일한 축을 쓰도록 벽 transform으로 스냅 위치·회전 저장
        float surfaceX = 0.5f * Mathf.Sign(Vector3.Dot(hit.normal, hit.collider.transform.right));
        _lastWallHitPoint = hit.collider.transform.TransformPoint(surfaceX, snappedY, snappedZ);
        _lastWallHitNormal = hit.normal;
        _lastWallHitRotation = hit.collider.transform.rotation;
    }

    bool IsWallPlacementValid(Furniture wallItem)
    {
        // 1. 바닥 가구와의 충돌 체크
        // 벽 가구가 붙어 있는 벽면의 그리드 좌표와 방향을 알아내야 함
        // (현재는 단순화를 위해 wallItem의 world position 근처의 바닥 가구 체크)
        Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(wallItem.transform.position.x), Mathf.FloorToInt(wallItem.transform.position.z));
        
        foreach (var f in allFurnitures)
        {
            if (f == wallItem || f.isAttachedToWall) continue;
            
            // 바닥 가구의 점유 셀 중 하나가 벽 가구 밑에 있는지 확인
            foreach (var cell in f.GetOccupiedCells())
            {
                if (cell == gridPos)
                {
                    // 바닥 가구의 높이가 벽 가구의 설치 높이보다 높으면 설치 불가
                    if (f.height > wallItem.transform.position.y - 0.5f) // 대략적인 높이 비교
                    {
                        Debug.Log($"[Wall Placement] Failed: {f.displayName} is too high ({f.height})");
                        return false;
                    }
                }
            }
        }
        return true;
    }
}
