# almost_happy_home - 프로젝트 정리 가이드

## 완료된 정리

### 1. 프로젝트명
- **ProjectSettings**: `productName` → `almost_happy_home` 적용됨
- **폴더명 변경**: Unity를 종료한 후 `Practice` 폴더를 `almost_happy_home`으로 수동 변경 후 프로젝트를 다시 열어주세요.

### 2. Scripts 폴더 구조
```
Assets/Scripts/
├── Core/
│   ├── GameData.cs       (UpcomingDelivery, StageConfig)
│   └── Furniture.cs
├── Game/
│   ├── GridGameManager.cs
│   ├── GridGameManager.Indicators.cs
│   └── GridGameManager.GameFlow.cs
├── UI/
│   ├── FurnitureSelectionManager.cs
│   └── GameStartUI.cs
└── Camera/
    ├── IsometricCamera.cs
    └── IsometricCameraController.cs
```

### 3. Prefabs
- `DeliveryIndicator.prefab` → `Assets/Prefabs/` 로 이동됨

### 4. Inspector 참조
- 모든 참조는 GUID 기반이므로 자동으로 유지됩니다.
- Unity 에디터에서 프로젝트를 열면 참조가 올바르게 연결됩니다.

## 수동 정리 권장

Unity 에디터에서 다음 에셋을 드래그하여 폴더로 이동해주세요 (참조는 GUID로 유지됨):

1. **Materials**: `New Material 1.mat` ~ `New Material 8.mat`, `New Material.mat` → `Assets/Materials/`
2. **Fonts**: `NanumSquareRoundL SDF.asset`, `NanumSquareRoundL.ttf` → `Assets/Fonts/`

(Unity 에디터에서 드래그 앤 드롭으로 이동하면 참조가 유지됩니다.)
