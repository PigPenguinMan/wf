# 🛠️ Unity 2D URP & 32x32 픽셀아트 환경 세팅 가이드

이 문서는 `f:\WF` 워크스페이스에 구축된 **《프로젝트 WF : 픽셀 프론티어》** 코어 아키텍처를 Unity 에디터에서 바로 구동하고, **32×32 픽셀아트(크로노 트리거·문라이터 감성)**가 모바일 화면에서 깨짐 없이 칼같이 출력되도록 세팅하는 가이드입니다.

---

## 1. 폴더 및 스크립트 아키텍처 (`f:\WF\Assets\_Project`)

```text
f:\WF\
├── Docs\
│   └── UNITY_PROJECT_SETUP.md               # 유니티 2D URP 및 32x32 도트 세팅 가이드
└── Assets\
    └── _Project\
        ├── Art\                             # 32x32 스프라이트 시트, 타일맵, 페이퍼돌 도트 에셋
        ├── ScriptableObjects\               # 몬스터, 아이템, 레시피, 룬 데이터 에셋 저장소
        └── Scripts\
            ├── Core\
            │   ├── GameEnums.cs             # 3대 클래스, 5단계 등급, 룬 속성, 컨텍스트 열거형
            │   └── IHoldInteractable.cs     # 시체 갈무리·채집 홀드 상호작용 인터페이스
            ├── Data\
            │   ├── ItemDataSO.cs            # 아이템 및 '재료 압축(Compression)' 규칙 데이터
            │   ├── MonsterDataSO.cs         # 몬스터 스탯, 야행성 여부, '갈무리 드랍 테이블'
            │   ├── CraftingRecipeSO.cs      # 장비 제작, 모닥불 요리, 재료 변이(Mutation) 레시피
            │   └── RuneDataSO.cs            # 화염/전격(공방저하)/냉기/흡혈/가시(반사) 룬 데이터
            ├── Player\
            │   ├── PlayerController.cs      # 8방향 탑다운 픽셀 이동, 회피/대시, 가상 패드 연동
            │   └── PlayerCombat.cs          # 클래스별 전투(차지/반사탱커) & 스마트 액션 버튼 감지
            ├── Monster\
            │   ├── MonsterBase.cs           # 몬스터 AI, 룬 상태이상 적용, 사망 시 시체 전환
            │   └── MonsterCorpse.cs         # [핵심] 시체 갈무리(Carving) 홀드 게이지 & 보상 드랍
            ├── Systems\
            │   ├── InventoryAndCraftingManager.cs # 인벤토리, 재료 압축, 변이, 제작 매니저
            │   └── PetTamingSystem.cs       # 소형(S) 몬스터 유혹(펫 테이밍) 및 동반 성장/특성 개방
            └── UI\
                └── MobileHUDController.cs   # 모바일 스마트 컨텍스트 버튼(⚔️↔🔪 자동 전환) HUD
```

---

## 2. 32×32 픽셀아트를 위한 Unity 필수 설정

### 2.1 스프라이트 임포트 규격 (Sprite Import Settings)
모든 도트 에셋(캐릭터, 타일셋, 몬스터, 아이콘)을 임포트할 때 아래 3가지는 반드시 통일해야 도트가 흐려지거나 경계선이 찢어지지 않습니다.
* **Pixels Per Unit (PPU)**: `32` (32×32 픽셀 = 유니티 월드 1 Unit)
* **Filter Mode**: `Point (no filter)` (안티앨리어싱 블러 제거)
* **Compression**: `None` (색상 번짐 및 아티팩트 방지)

### 2.2 모바일 가로 모드 Pixel Perfect Camera 세팅
`Main Camera`에 **Pixel Perfect Camera (URP)** 컴포넌트를 추가하고 아래와 같이 설정합니다.
* **Assets Pixels Per Unit**: `32`
* **Reference Resolution**: **`480 × 270`** (16:9 ~ 20:9 모바일 화면에 완벽히 대응하며, 화면 세로에 약 8.4타일, 가로에 15타일이 들어와《크로노 트리거》·《문라이터》와 동일한 캐릭터 비율을 보여줍니다)
* **Upscale Render Texture**: `체크 (On)` (회전/이펙트 시 서브픽셀 깨짐 방지)
* **Pixel Snapping**: `체크 (On)`

### 2.3 Y-Sorting (탑다운 원근감 정렬)
탑다운 2D RPG에서 캐릭터가 나무나 거대 몬스터의 앞/뒤로 자연스럽게 지나가도록 설정합니다.
1. `Edit > Project Settings > Graphics > URP 2D Renderer` (또는 `Transparency Sort Mode`) 이동
2. **Transparency Sort Mode**: `Custom Axis`
3. **Transparency Sort Axis**: `X: 0, Y: 1, Z: 0`
4. 모든 캐릭터/몬스터/나무 스프라이트의 **Pivot**을 `Bottom (하단 중앙)`으로 설정하고, `Sprite Renderer > Sprite Sort Point`를 `Pivot`으로 변경합니다.

---

## 3. 프로토타입 씬(Scene) 빠른 조립 순서

1. **GameManager 오브젝트 생성**:
   - 빈 GameObject(`@Managers`)를 만들고 `InventoryAndCraftingManager`, `PetTamingSystem` 컴포넌트를 부착합니다.
2. **Player 오브젝트 생성**:
   - `Rigidbody2D` (Gravity Scale = 0, Freeze Rotation Z = 체크), `CircleCollider2D`, `PlayerController`, `PlayerCombat` 부착.
   - `PlayerCombat`의 `Monster And Interact Layer`에 `Monster` 레이어를 지정합니다.
3. **Monster 프리팹 생성 (예: 초반 준보스 '덕구' 또는 야생 멧돼지)**:
   - Project 창 우클릭 ➔ `Create > WildFrontier > Data > Monster Data`로 데이터 에셋 생성 후 갈무리 드랍 테이블(`carvingLootTable`) 등록.
   - 씬의 몬스터 오브젝트에 `MonsterBase`와 `MonsterCorpse`를 부착하고 레이어를 `Monster`로 설정.
4. **모바일 HUD 연결**:
   - Canvas 아래 우측 하단 액션 버튼에 `MobileHUDController`를 연결하고, `EventTrigger` 컴포넌트로 `PointerDown` ➔ `OnSmartActionPointerDown()`, `PointerUp` ➔ `OnSmartActionPointerUp()`을 연결합니다.
   - 이제 평소에는 **⚔️ 공격/차지**로 작동하다가, 몬스터를 처치하고 시체 위에 서면 버튼이 자동으로 **🔪 갈무리(Hold)**로 전환됩니다!
