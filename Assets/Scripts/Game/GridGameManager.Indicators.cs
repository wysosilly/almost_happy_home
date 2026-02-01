using UnityEngine;
using System.Collections.Generic;

public partial class GridGameManager
{
    void UpdateIndicators()
    {
        foreach (var obj in activeIndicators) if (obj != null) Destroy(obj);
        activeIndicators.Clear();
        foreach (var obj in _furnitureOverlays) if (obj != null) Destroy(obj);
        _furnitureOverlays.Clear();
        UpdateWallFurnitureLabels();

        Color urgentColor = new Color(1, 0, 0, 0.6f);
        Color warningColor = new Color(1, 0.5f, 0, 0.4f);
        float scaleFactor = 1f / Mathf.Max(indicatorBaseSize, 0.01f);
        bool useAlwaysOnTop = indicatorAlwaysOnTop && isDragging;

        foreach (var d in upcomingDeliveries)
        {
            Color baseColor = (d.turnsLeft == 1) ? urgentColor : warningColor;

            if (useAlwaysOnTop)
            {
                if (d.shapeCells != null && d.shapeCells.Count > 0)
                {
                    foreach (var cell in d.shapeCells)
                    {
                        if (indicatorPrefab != null)
                            activeIndicators.Add(CreateIndicator(
                                new Vector3(d.gridPos.x + cell.x + 0.5f, 0.05f, d.gridPos.y + cell.y + 0.5f),
                                new Vector3(scaleFactor, 0.01f, scaleFactor), baseColor, true));
                    }
                }
                else if (indicatorPrefab != null)
                {
                    activeIndicators.Add(CreateIndicator(
                        new Vector3(d.gridPos.x + (d.size.x * 0.5f), 0.05f, d.gridPos.y + (d.size.y * 0.5f)),
                        new Vector3(d.size.x * scaleFactor, 0.01f, d.size.y * scaleFactor), baseColor, true));
                }
            }
            else
            {
                var cells = new List<Vector2Int>();
                if (d.shapeCells != null && d.shapeCells.Count > 0)
                {
                    foreach (var cell in d.shapeCells)
                        cells.Add(new Vector2Int(d.gridPos.x + cell.x, d.gridPos.y + cell.y));
                }
                else
                {
                    for (int x = 0; x < d.size.x; x++)
                        for (int y = 0; y < d.size.y; y++)
                            cells.Add(new Vector2Int(d.gridPos.x + x, d.gridPos.y + y));
                }
                foreach (var c in cells)
                {
                    var halfCell = new Vector2Int(c.x * 2, c.y * 2);
                    var f = GetFurnitureAt(halfCell, null);
                    if (f != null)
                    {
                        _furnitureOverlays.Add(CreateFurnitureOverlay(f, c, baseColor));
                    }
                    else if (indicatorPrefab != null)
                    {
                        activeIndicators.Add(CreateIndicator(
                            new Vector3(c.x + 0.5f, 0.05f, c.y + 0.5f),
                            new Vector3(scaleFactor, 0.01f, scaleFactor), baseColor, false));
                    }
                }
            }
        }
    }

    GameObject CreateIndicator(Vector3 pos, Vector3 scale, Color color, bool useAlwaysOnTop)
    {
        if (indicatorPrefab == null) return null;
        GameObject ind = Instantiate(indicatorPrefab);
        float yOffset = 0f;
        if (useAlwaysOnTop)
        {
            var shader = Shader.Find("Custom/IndicatorAlwaysOnTop")
                ?? Shader.Find("Custom/IndicatorAlwaysOnTopURP");
            if (shader != null)
            {
                var rend = ind.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_Color", color);
                    rend.material = mat;
                }
            }
        }
        else
        {
            var rend = ind.GetComponent<Renderer>();
            if (rend != null) rend.material.color = color;
        }
        ind.transform.position = new Vector3(pos.x, pos.y + yOffset, pos.z);
        ind.transform.localScale = scale;
        return ind;
    }

    GameObject CreateFurnitureOverlay(Furniture f, Vector2Int cell, Color indicatorColor)
    {
        float topY = GetFurnitureTopY(f);
        if (topY < -100f) return null;
        var overlayColor = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, furnitureOverlayOpacity);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "DeliveryOverlay";
        quad.transform.position = new Vector3(cell.x + 0.5f, topY + 0.005f, cell.y + 0.5f);
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        var coll = quad.GetComponent<Collider>();
        if (coll != null) coll.enabled = false;

        var rend = quad.GetComponent<Renderer>();
        if (rend != null)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = overlayColor;
                mat.renderQueue = 3000;
                rend.material = mat;
            }
            else
            {
                rend.material.color = overlayColor;
            }
        }
        return quad;
    }

    float GetFurnitureTopY(Furniture f)
    {
        if (f == null) return -999f;
        float designTop = f.pivotAtCenter
            ? f.transform.position.y + f.visualHeight * 0.5f
            : f.transform.position.y + f.visualHeight;
        float maxY = float.MinValue;
        foreach (var r in f.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            if (r.gameObject.name.Contains("NameLabel") || r.gameObject.name.Contains("Label"))
                continue;
            if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
        }
        if (maxY <= float.MinValue) return designTop;
        return Mathf.Min(maxY, designTop + 0.2f);
    }

    void UpdateDropPreview()
    {
        foreach (var obj in _dropPreviewIndicators) if (obj != null) Destroy(obj);
        _dropPreviewIndicators.Clear();
        foreach (var obj in _wallPreviewIndicators) if (obj != null) Destroy(obj);
        _wallPreviewIndicators.Clear();

        if (selectedFurniture == null || !isDragging) return;

        if (selectedFurniture.isAttachedToWall)
        {
            bool isValid = IsWallPlacementValid(selectedFurniture);
            Color guideColor = isValid ? new Color(0, 1, 0, 0.45f) : new Color(1, 0, 0, 0.45f);

            // 왼쪽/오른쪽 벽 모두 같은 쪽(방 안)을 보이게: 법선으로 앞면 방향 고정, 벽 up으로 그리드 정렬
            Vector3 n = _lastWallHitNormal.normalized;
            Vector3 wallUp = _lastWallHitRotation * Vector3.up;
            Quaternion guideRot = Quaternion.LookRotation(-n, wallUp);
            Vector3 guidePos = _lastWallHitPoint + n * 0.015f;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "WallDropGuide";
            quad.transform.position = guidePos;
            quad.transform.rotation = guideRot;
            float wx = Mathf.Max(0.1f, selectedFurniture.wallSize.x);
            float wy = Mathf.Max(0.1f, selectedFurniture.wallSize.y);
            quad.transform.localScale = new Vector3(wx, wy, 1f);
            var col = quad.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            var rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = guideColor;
            }
            _wallPreviewIndicators.Add(quad);

            CreateWallGuideFrame(guidePos, guideRot, wx, wy, guideColor);
            return;
        }

        void CreateWallGuideFrame(Vector3 center, Quaternion rot, float width, float height, Color color)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            Vector3 fwd = rot * Vector3.forward;
            Vector3 offset = fwd * 0.015f;
            Vector3 c = center + offset;
            Vector3 a = c - right * hw - up * hh;
            Vector3 b = c + right * hw - up * hh;
            Vector3 d = c + right * hw + up * hh;
            Vector3 e = c - right * hw + up * hh;
            _wallPreviewIndicators.Add(CreateWallGuideLine(a, b));
            _wallPreviewIndicators.Add(CreateWallGuideLine(b, d));
            _wallPreviewIndicators.Add(CreateWallGuideLine(d, e));
            _wallPreviewIndicators.Add(CreateWallGuideLine(e, a));
        }

        GameObject CreateWallGuideLine(Vector3 worldStart, Vector3 worldEnd)
        {
            GameObject line = new GameObject("WallGuideLine");
            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.8f);
            lr.startWidth = lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, worldStart);
            lr.SetPosition(1, worldEnd);
            return line;
        }

        Vector3 dropPos = GetMouseWorldPos() + dragOffset;
        var itemSize = selectedFurniture.EffectiveSize;
        int snapX = Mathf.RoundToInt(dropPos.x * 2f), snapZ = Mathf.RoundToInt(dropPos.z * 2f);
        Vector2Int targetPos = selectedFurniture.pivotAtCenter
            ? new Vector2Int(snapX - itemSize.x, snapZ - itemSize.y)
            : new Vector2Int(snapX, snapZ);

        bool valid = IsPlacementValid(selectedFurniture, targetPos);

        Color previewColor = valid ? new Color(0.2f, 0.8f, 1f, 0.5f) : new Color(1f, 0.3f, 0.2f, 0.5f);
        float scaleFactor = 1f / Mathf.Max(indicatorBaseSize, 0.01f);
        var cells = selectedFurniture.GetOccupiedCellsAt(targetPos);
        const float half = 0.5f * 0.5f;
        foreach (var cell in cells)
        {
            var ind = CreateIndicator(
                new Vector3(cell.x * 0.5f + half, 0.08f, cell.y * 0.5f + half),
                new Vector3(scaleFactor, 0.01f, scaleFactor), previewColor, true);
            if (ind != null) _dropPreviewIndicators.Add(ind);
        }
    }

    void UpdateWallFurnitureLabels()
    {
        foreach (var obj in _wallFurnitureLabels) if (obj != null) Destroy(obj);
        _wallFurnitureLabels.Clear();

        Transform parent = null;
        GameObject wallLabelsRoot = GameObject.Find("WallFurnitureLabels");
        if (wallLabelsRoot == null) { wallLabelsRoot = new GameObject("WallFurnitureLabels"); parent = wallLabelsRoot.transform; }
        else parent = wallLabelsRoot.transform;

        foreach (var f in allFurnitures)
        {
            if (f == null || !f.gameObject.activeInHierarchy || !f.isAttachedToWall) continue;

            GameObject labelObj = new GameObject("WallFurnitureLabel");
            labelObj.transform.SetParent(parent);
            labelObj.transform.position = f.transform.position + f.transform.forward * 0.08f;
            labelObj.transform.rotation = f.transform.rotation;
            labelObj.transform.localScale = Vector3.one * 0.2f;

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            string name = string.IsNullOrEmpty(f.displayName) ? f.gameObject.name : f.displayName;
            tm.text = name + "\n(H:" + f.happyValue + ")";
            tm.fontSize = 24;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.characterSize = 0.15f;
            tm.lineSpacing = 1.1f;

            _wallFurnitureLabels.Add(labelObj);
        }
    }
}
