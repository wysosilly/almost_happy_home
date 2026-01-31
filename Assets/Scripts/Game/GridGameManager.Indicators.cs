using UnityEngine;
using System.Collections.Generic;

public partial class GridGameManager
{
    void UpdateIndicators()
    {
        foreach (var obj in activeIndicators) Destroy(obj);
        activeIndicators.Clear();
        foreach (var obj in _furnitureOverlays) Destroy(obj);
        _furnitureOverlays.Clear();

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
                    var f = GetFurnitureAt(c, null);
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
        foreach (var obj in _dropPreviewIndicators) Destroy(obj);
        _dropPreviewIndicators.Clear();
        if (selectedFurniture == null || !isDragging) return;

        Vector3 dropPos = GetMouseWorldPos() + dragOffset;
        var itemSize = selectedFurniture.EffectiveSize;
        Vector2Int targetPos = selectedFurniture.pivotAtCenter
            ? new Vector2Int(Mathf.FloorToInt(dropPos.x - itemSize.x * 0.5f), Mathf.FloorToInt(dropPos.z - itemSize.y * 0.5f))
            : new Vector2Int(Mathf.FloorToInt(dropPos.x), Mathf.FloorToInt(dropPos.z));

        bool valid = IsPlacementValid(selectedFurniture, targetPos);

        Color previewColor = valid ? new Color(0.2f, 0.8f, 1f, 0.5f) : new Color(1f, 0.3f, 0.2f, 0.5f);
        float scaleFactor = 1f / Mathf.Max(indicatorBaseSize, 0.01f);
        var cells = selectedFurniture.GetOccupiedCellsAt(targetPos);

        foreach (var cell in cells)
        {
            var ind = CreateIndicator(
                new Vector3(cell.x + 0.5f, 0.08f, cell.y + 0.5f),
                new Vector3(scaleFactor, 0.01f, scaleFactor), previewColor, true);
            if (ind != null) _dropPreviewIndicators.Add(ind);
        }
    }
}
