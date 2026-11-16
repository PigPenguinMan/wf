# Wild Frontier x Guardian Tales Prototype - Multi-PC Handoff Guide

## 1. 개발 환경
- **Unity 버전**: `6000.6.9f1`
- **타깃 스타일**: 와일드 프론티어 액션 RPG 코어 루프(사냥 → 시체 갈무리 → 모닥불 요리/버프 → 재료 압축·변이·제작 → 펫 테이밍 → 보스 부위파괴/그로기) + 가디언 테일즈풍 현대식 2등신 SD 픽셀아트 & 멀티 파츠 모션
- **메인 씬**: `Assets/_Project/Scenes/Chapter1_Prototype.unity`

---

## 2. 다른 PC에서 처음 실행하는 방법
1. 이 저장소를 `git clone` 한 뒤 **Unity Hub에서 Unity `6000.6.9f1`로 프로젝트 폴더를 엽니다** (`Library/` 폴더는 첫 오픈 시 자동 생성됩니다).
2. Unity 상단 메뉴에서 **`WildFrontier > Build Playable Prototype Scene (프로토타입 씬 원클릭 생성)`** 을 실행하면:
   - `Assets/_Project/Art/SourceSheets/` 상대 경로의 원본 시트에서 스프라이트를 추출하고,
   - `Assets/_Project/Scripts/Editor/PixelArtSpriteGenerator.cs`로 가디언 테일즈 스타일 파츠·무기·배경 스프라이트를 생성하며,
   - `Chapter1_Prototype.unity` 씬과 챕터1 `ScriptableObject` 데이터 및 모바일 HUD를 즉시 재구성합니다.
3. **`▶ Play`** 버튼으로 즉시 테스트할 수 있습니다 (`WASD` 이동, `Left Shift` 회피, `Space` 스마트 공격/차지/갈무리/채집, `Q` 클래스 스킬, `E` 보조 총기).

---

## 3. 프로젝트 구조 요약 (`Assets/_Project/`)
- `Scripts/Core/`: 게임 공통 Enum 및 인터페이스
- `Scripts/Data/`: 룬, 아이템, 몬스터, 제작 레시피 ScriptableObject 정의
- `Scripts/Player/`:
  - `GuardianTalesCharacterRig.cs`: 8파츠 퍼펫 리그(그림자, 좌/우 발 교차, 몸통, 얼굴 표정 4종, 클래스 투구/후드, 방패/손, 무기 피벗 3타 콤보 & 먼지 구름)
  - `PlayerController.cs`: 이동/회피/공격 전진 스텝(`TriggerAttackLunge`) 및 리그 애니메이션 연동
  - `PlayerCombat.cs`: 클래스 전환(Warrior/Scouter/Guardian), 3연타 콤보·차지 공격, 룬 프리셋 5종, 가시 반사 탱커, 스마트 컨텍스트 상호작용
- `Scripts/Monster/`:
  - `MonsterBase.cs`: AI 상태, 속성 디버프, 보스 장판 예고 및 부위파괴 그로기, 발 그림자, 추격 호핑, 피격 점멸/찌그러짐/넉백
  - `MonsterCorpse.cs`: 몬스터 처치 후 시체 갈무리 상호작용
- `Scripts/Systems/`: 인벤토리/압축/변이/제작, 모닥불 캠프, 야생 채집물, 펫 테이밍, 도감, 몬스터 리스폰, 낮/밤 환경
- `Scripts/UI/`:
  - `MonsterWorldUI.cs`: 몬스터 발밑 compact HP 바 + 라벨
  - `PlayerFootHPBar.cs`: 플레이어 루트 고정 발밑 초록색 HP 바(`Lv.1`)
  - `HunterWorkshopHUD.cs`, `MobileHUDController.cs`, `VirtualJoystickUI.cs`, `FloatingPopupManager.cs`
- `Scripts/Editor/`: `PixelArtSpriteGenerator.cs`, `PrototypeSceneBuilderEditor.cs`, `Chapter1DataGeneratorEditor.cs`
- `Art/SourceSheets/`: 다른 PC에서도 추출을 재현할 수 있도록 포함한 원본 스프라이트 시트 (`sd_chibi_classes_sheet.jpg`, `modern_pixel_monsters_sheet.jpg`, `modern_pixel_props_sheet.jpg`)
- `Docs/`: 기획서(`GAME_DESIGN_AND_PLAN.md`), 셋업(`UNITY_PROJECT_SETUP.md`), 데이터 테이블(`CHAPTER1_DATA_TABLES.md`), 픽셀아트 규격(`PIXEL_ART_SPECIFICATION.md`), 핸드오프(`HANDOFF.md`)

---

## 4. 다른 PC의 Antigravity에서 이어서 작업할 때 프롬프트 예시
> `Docs/HANDOFF.md`와 `Docs/GAME_DESIGN_AND_PLAN.md`를 먼저 읽고 현재 프로젝트 구조를 파악한 뒤 이어서 진행해 줘.
