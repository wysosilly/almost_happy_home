using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class FurnitureEnhancementUI : MonoBehaviour
{
    public GameObject panel;
    public Text titleText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    private Furniture _targetFurniture;
    private Action _onComplete;

    public void Show(Furniture target, List<IFurnitureEnhancement> options, Action onComplete)
    {
        _targetFurniture = target;
        _onComplete = onComplete;
        
        panel.SetActive(true);
        titleText.text = $"'{target.displayName}' 가구 강화 선택";

        // 기존 버튼 제거
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var option in options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            btnObj.SetActive(true); // 프리팹이 비활성화되어 있으므로 활성화 필요
            btnObj.GetComponentInChildren<Text>().text = $"{option.EnhancementName}\n<size=11>{option.Description}</size>"; // 14 -> 11
            btnObj.GetComponent<Button>().onClick.AddListener(() => SelectOption(option));
        }
    }

    private void SelectOption(IFurnitureEnhancement option)
    {
        if (option is GridExpansionEnhancement)
        {
            panel.SetActive(false);
            var manager = UnityEngine.Object.FindObjectOfType<GridGameManager>();
            manager.StartExpansionSelection(() => {
                _onComplete?.Invoke();
            });
        }
        else
        {
            _targetFurniture.AddEnhancement(option);
            panel.SetActive(false);
            _onComplete?.Invoke();
        }
    }

    public static FurnitureEnhancementUI Create()
    {
        GameObject canvasObj = new GameObject("FurnitureEnhancementCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0f;
        canvasObj.AddComponent<GraphicRaycaster>();
        if (canvasObj.GetComponent<CanvasFitInScreen>() == null)
            canvasObj.AddComponent<CanvasFitInScreen>();

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 400);
        panelObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.9f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.sizeDelta = new Vector2(450, 50);
        Text title = titleObj.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 22; // 28 -> 22
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;

        GameObject containerObj = new GameObject("OptionsContainer");
        containerObj.transform.SetParent(panelObj.transform, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.45f);
        containerRect.anchorMax = new Vector2(0.5f, 0.45f);
        containerRect.sizeDelta = new Vector2(450, 250);
        VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        // 버튼 프리팹 대용 생성
        GameObject btnPrefab = new GameObject("OptionButton");
        btnPrefab.AddComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
        btnPrefab.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f);
        btnPrefab.AddComponent<Button>();
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnPrefab.transform, false);
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = btnTextRect.offsetMax = Vector2.zero;
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 16; // 20 -> 16
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnPrefab.SetActive(false);

        FurnitureEnhancementUI ui = canvasObj.AddComponent<FurnitureEnhancementUI>();
        ui.panel = panelObj;
        ui.titleText = title;
        ui.optionsContainer = containerObj.transform;
        ui.optionButtonPrefab = btnPrefab;
        
        panelObj.SetActive(false);
        return ui;
    }
}
