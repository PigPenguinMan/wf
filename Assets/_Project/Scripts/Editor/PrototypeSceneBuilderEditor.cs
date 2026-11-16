#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using WildFrontier.Core;
using WildFrontier.Data;
using WildFrontier.Monster;
using WildFrontier.Player;
using WildFrontier.Systems;
using WildFrontier.UI;

namespace WildFrontier.Editor
{
    /// <summary>
    /// Unity 상단 메뉴 [WildFrontier > Build Playable Prototype Scene (프로토타입 씬 원클릭 생성)]을 실행하면
    /// 1챕터 데이터 에셋, 32x32 플레이스홀더 도트 스프라이트, 플레이어, 모닥불 캠프, 채집물,
    /// 몬스터(초원 뿔토끼, 준보스 광폭한 덕구, 보스 고대 수호목 엔트), 모바일 스마트 HUD까지
    /// 완전히 조립된 씬을 생성하고 즉시 플레이 가능한 상태로 만들어 줍니다.
    /// </summary>
    public static class PrototypeSceneBuilderEditor
    {
        private const string SpriteFolder = "Assets/_Project/Art/GeneratedSprites";
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string DataBasePath = "Assets/_Project/ScriptableObjects/Chapter1";

        [MenuItem("WildFrontier/Build Playable Prototype Scene (프로토타입 씬 원클릭 생성)")]
        public static void BuildPrototypeScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[WildFrontier] 현재 Unity가 스크립트를 컴파일 중입니다. 우측 하단 컴파일 로딩이 끝난 뒤 다시 클릭해 주세요!");
                return;
            }

            // 플레이 모드(Play Mode) 실행 중에 메뉴를 클릭한 경우:
            // 자동으로 플레이 모드를 종료한 뒤 에디터 모드에서 씬 생성을 이어서 실행합니다!
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[WildFrontier] 현재 Play 모드가 실행 중이어서 자동으로 Play 모드를 종료한 뒤 씬을 재생성합니다...");
                EditorApplication.playModeStateChanged += OnPlayModeChangedForRebuild;
                EditorApplication.isPlaying = false;
                return;
            }

            // 1. 1챕터 ScriptableObject 데이터 먼저 자동 생성
            Chapter1DataGeneratorEditor.GenerateAllChapter1Data();

            EnsureDirectory(SpriteFolder);
            EnsureDirectory(SceneFolder);

            // 2. 가디언 테일즈(ref/img) 스타일 최신 고밀도 픽셀 스프라이트 시트 일괄 추출 & 에셋 생성
            Sprite grassFloorSprite = PixelArtSpriteGenerator.GenerateGrassFloorSprite(SpriteFolder);
            Sprite giantStumpSprite = PixelArtSpriteGenerator.GenerateGiantTreeStumpSprite(SpriteFolder);
            Sprite forestTreeSprite = PixelArtSpriteGenerator.GenerateForestTreeSprite(SpriteFolder);
            Sprite sunbeamSprite = PixelArtSpriteGenerator.GenerateSunbeamSprite(SpriteFolder);
            Sprite hunterSprite = PixelArtSpriteGenerator.GenerateHunterSprite(SpriteFolder);
            Sprite scouterSprite = PixelArtSpriteGenerator.TryExtractSDChibiClassSprite(SpriteFolder, "Spr_Hunter_Scouter", 0.37f, 0.655f, 0.06f, 0.84f) ?? hunterSprite;
            Sprite guardianSprite = PixelArtSpriteGenerator.TryExtractSDChibiClassSprite(SpriteFolder, "Spr_Hunter_Guardian", 0.65f, 0.985f, 0.06f, 0.92f) ?? hunterSprite;
            Sprite oversizedSwordSprite = PixelArtSpriteGenerator.GenerateOversizedWeaponSprite(SpriteFolder);
            Sprite slashArcSprite = PixelArtSpriteGenerator.GenerateSlashArcSprite(SpriteFolder);
            Sprite rabbitSprite = PixelArtSpriteGenerator.GenerateHornedRabbitSprite(SpriteFolder);
            Sprite mushroomMonsterSprite = PixelArtSpriteGenerator.GenerateSporeMushroomMonsterSprite(SpriteFolder);
            Sprite beetleSprite = PixelArtSpriteGenerator.GenerateSpikedBeetleSprite(SpriteFolder);
            Sprite wolfSprite = PixelArtSpriteGenerator.GenerateWindWolfSprite(SpriteFolder);
            Sprite deokguSprite = PixelArtSpriteGenerator.GenerateBerserkDeokguSprite(SpriteFolder);
            Sprite entBossSprite = PixelArtSpriteGenerator.GenerateAncientEntBossSprite(SpriteFolder);
            Sprite campfireSprite = PixelArtSpriteGenerator.GenerateCampfireSprite(SpriteFolder);
            Sprite mushroomSprite = PixelArtSpriteGenerator.GenerateGlowMushroomSprite(SpriteFolder);
            Sprite attackSwordIcon = PixelArtSpriteGenerator.GenerateAttackSwordIconSprite(SpriteFolder) ?? hunterSprite;
            Sprite carveKnifeSprite = PixelArtSpriteGenerator.GenerateCarveKnifeIconSprite(SpriteFolder);
            Sprite ringSprite = CreateCircleRingSprite("Spr_UI_Ring_64", 64);
            Sprite whiteUiSprite = CreateSolidSquareSprite("Spr_UI_White_16", 16);

            // 2-1. 가디언 테일즈 스타일 멀티 파츠 리그 / 무기 / 흙길 / 석판 스프라이트 생성
            PixelArtSpriteGenerator.GenerateGTHeadSprites(SpriteFolder,
                out Sprite gtHeadNormal, out Sprite gtHeadBlink, out Sprite gtHeadAttack, out Sprite gtHeadHurt);
            PixelArtSpriteGenerator.GenerateGTClassHeadgearSprites(SpriteFolder,
                out Sprite gtHelmWarrior, out Sprite gtHoodScouter, out Sprite gtHelmGuardian);
            PixelArtSpriteGenerator.GenerateGTBodyAndLimbSprites(SpriteFolder,
                out Sprite gtBody, out Sprite gtFoot, out Sprite gtHand, out Sprite gtShield, out Sprite gtShadow, out Sprite gtDustPuff);
            PixelArtSpriteGenerator.GenerateGTClassWeaponSprites(SpriteFolder,
                out Sprite gtWpnWarrior, out Sprite gtWpnScouter, out Sprite gtWpnGuardian);
            Sprite dirtRoadSprite = PixelArtSpriteGenerator.GenerateDirtRoadSprite(SpriteFolder);
            Sprite stoneSlabSprite = PixelArtSpriteGenerator.GenerateStoneSlabSprite(SpriteFolder);

            // 3. 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3-1. 가디언 테일즈풍 고해상도 소프트 그리드 잔디 바닥 (Ground_MeadowFloor)
            GameObject groundGo = new GameObject("Ground_MeadowFloor");
            groundGo.transform.position = Vector3.zero;
            SpriteRenderer groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.sprite = grassFloorSprite;
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.size = new Vector2(40f, 26f);
            groundSr.sortingOrder = -10;

            // 3-1b. 황토 흙길(Road_DirtPath) 및 이끼 석판(Prop_StoneSlab) 배치
            if (dirtRoadSprite != null)
            {
                GameObject roadGo = new GameObject("Road_DirtPath");
                roadGo.transform.position = new Vector3(3.5f, -0.35f, 0f);
                SpriteRenderer roadSr = roadGo.AddComponent<SpriteRenderer>();
                roadSr.sprite = dirtRoadSprite;
                roadSr.drawMode = SpriteDrawMode.Tiled;
                roadSr.size = new Vector2(26f, 2.1f);
                roadSr.sortingOrder = -8;
            }

            if (stoneSlabSprite != null)
            {
                Vector3[] slabPositions =
                {
                    new Vector3(-1.6f, 0.45f, 0f),
                    new Vector3(1.4f, -1.35f, 0f),
                    new Vector3(4.9f, 0.05f, 0f),
                    new Vector3(7.6f, -0.55f, 0f)
                };
                for (int i = 0; i < slabPositions.Length; i++)
                {
                    GameObject slabGo = new GameObject($"Prop_StoneSlab_{i + 1:00}");
                    slabGo.transform.position = slabPositions[i];
                    SpriteRenderer slabSr = slabGo.AddComponent<SpriteRenderer>();
                    slabSr.sprite = stoneSlabSprite;
                    slabSr.sortingOrder = -6;
                }
            }

            // 3-2. ref/img 첫 번째 사진의 거대 나무 그루터기, 사냥터 외곽 숲 나무 6그루, 대각선 햇살 샤프트 배치
            GameObject stumpGo = new GameObject("Prop_GiantTreeStump");
            stumpGo.transform.position = new Vector3(0f, 2.2f, 0f);
            SpriteRenderer stumpSr = stumpGo.AddComponent<SpriteRenderer>();
            stumpSr.sprite = giantStumpSprite;
            stumpSr.sortingOrder = 2;

            if (forestTreeSprite != null)
            {
                Vector3[] treePositions =
                {
                    new Vector3(-6.5f, 3.8f, 0f),
                    new Vector3(-6.0f, -3.5f, 0f),
                    new Vector3(0.5f, 4.5f, 0f),
                    new Vector3(6.8f, 4.0f, 0f),
                    new Vector3(7.5f, -3.8f, 0f),
                    new Vector3(12.5f, 2.5f, 0f)
                };
                for (int i = 0; i < treePositions.Length; i++)
                {
                    GameObject treeGo = new GameObject($"Prop_ForestTree_{i + 1:00}");
                    treeGo.transform.position = treePositions[i];
                    SpriteRenderer treeSr = treeGo.AddComponent<SpriteRenderer>();
                    treeSr.sprite = forestTreeSprite;
                    treeSr.sortingOrder = 3;
                }
            }

            GameObject sunbeamGo = new GameObject("VFX_SunbeamShafts");
            sunbeamGo.transform.position = new Vector3(1.2f, 1.0f, 0f);
            sunbeamGo.transform.localScale = new Vector3(2.2f, 1.8f, 1f);
            SpriteRenderer sunbeamSr = sunbeamGo.AddComponent<SpriteRenderer>();
            sunbeamSr.sprite = sunbeamSprite;
            sunbeamSr.sortingOrder = 20;

            // 4. @Managers 생성 (인벤토리/압축/제작, 펫 테이밍, 도감, 서식지 리스폰, 낮/밤·날씨 환경, 플로팅 팝업)
            GameObject managersGo = new GameObject("@Managers");
            managersGo.AddComponent<InventoryAndCraftingManager>();
            managersGo.AddComponent<PetTamingSystem>();
            managersGo.AddComponent<WorldEnvironmentManager>();
            managersGo.AddComponent<FloatingPopupManager>();
            managersGo.AddComponent<BestiaryManager>();
            MonsterSpawnerManager spawnerManager = managersGo.AddComponent<MonsterSpawnerManager>();

            // 5. Main Camera 생성 (32 PPU 탑다운 뷰)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.2f; // 480x270 (32 PPU) 기준 약 8.4 타일 높이
            cam.backgroundColor = new Color(0.18f, 0.26f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            TopDownCameraFollow camFollow = camGo.AddComponent<TopDownCameraFollow>();

            // 6. 데이터 에셋 로드
            var runeFire = AssetDatabase.LoadAssetAtPath<RuneDataSO>($"{DataBasePath}/Runes/RUNE_FIRE_01.asset");
            var runeThorns = AssetDatabase.LoadAssetAtPath<RuneDataSO>($"{DataBasePath}/Runes/RUNE_THORNS_02.asset");
            var runeLightning = AssetDatabase.LoadAssetAtPath<RuneDataSO>($"{DataBasePath}/Runes/RUNE_LIGHTNING_03.asset");
            var runeIce = AssetDatabase.LoadAssetAtPath<RuneDataSO>($"{DataBasePath}/Runes/RUNE_ICE_02.asset");
            var runeVampire = AssetDatabase.LoadAssetAtPath<RuneDataSO>($"{DataBasePath}/Runes/RUNE_VAMPIRE_02.asset");
            var leather01 = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_LEATHER_01.asset");
            var leather02 = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_LEATHER_02.asset");
            var deokguFur = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_DEOKGU_FUR.asset");
            var meat01 = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_MEAT_01.asset");
            var sporeItem = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_SPORE_01.asset");
            var mutScaleItem = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_MUT_SCALE.asset");
            var mutDragonScaleItem = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"{DataBasePath}/Items/MAT_MUT_DRAGONSCALE.asset");
            var deokguRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeSO>($"{DataBasePath}/Recipes/REC_EQ_DEOKGU.asset");
            var mutationRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeSO>($"{DataBasePath}/Recipes/REC_MUT_01.asset");
            var cookSoupRecipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeSO>($"{DataBasePath}/Recipes/REC_COOK_02.asset");
            var rabbitData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_101_RABBIT.asset");
            var mushroomData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_102_MUSHROOM.asset");
            var beetleData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_103_BEETLE.asset");
            var wolfData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_104_WOLF.asset");
            var deokguData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_105_DEOKGU.asset");
            var entBossData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>($"{DataBasePath}/Monsters/MON_106_ENT_BOSS.asset");

            // 7. Player_Hunter 생성 (가디언 테일즈식 8파츠 퍼펫 리그 + 3타 콤보 무기 피벗 + 발밑 초록색 HP 바)
            GameObject playerGo = new GameObject("Player_Hunter");
            playerGo.transform.position = Vector3.zero;

            GameObject rigVisualRoot = new GameObject("RigVisualRoot");
            rigVisualRoot.transform.SetParent(playerGo.transform, false);

            GameObject footShadowGo = new GameObject("FootShadow");
            footShadowGo.transform.SetParent(rigVisualRoot.transform, false);
            footShadowGo.transform.localPosition = new Vector3(0f, -0.41f, 0f);
            SpriteRenderer footShadowSr = footShadowGo.AddComponent<SpriteRenderer>();
            footShadowSr.sprite = gtShadow;
            footShadowSr.sortingOrder = 5;

            GameObject leftFootGo = new GameObject("LeftFoot");
            leftFootGo.transform.SetParent(rigVisualRoot.transform, false);
            leftFootGo.transform.localPosition = new Vector3(-0.11f, -0.34f, 0f);
            SpriteRenderer leftFootSr = leftFootGo.AddComponent<SpriteRenderer>();
            leftFootSr.sprite = gtFoot;
            leftFootSr.sortingOrder = 7;

            GameObject rightFootGo = new GameObject("RightFoot");
            rightFootGo.transform.SetParent(rigVisualRoot.transform, false);
            rightFootGo.transform.localPosition = new Vector3(0.11f, -0.34f, 0f);
            SpriteRenderer rightFootSr = rightFootGo.AddComponent<SpriteRenderer>();
            rightFootSr.sprite = gtFoot;
            rightFootSr.sortingOrder = 8;

            GameObject bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(rigVisualRoot.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, -0.13f, 0f);
            SpriteRenderer bodySr = bodyGo.AddComponent<SpriteRenderer>();
            bodySr.sprite = gtBody;
            bodySr.sortingOrder = 9;

            GameObject headGo = new GameObject("Head");
            headGo.transform.SetParent(rigVisualRoot.transform, false);
            headGo.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            SpriteRenderer headSr = headGo.AddComponent<SpriteRenderer>();
            headSr.sprite = gtHeadNormal;
            headSr.sortingOrder = 10;

            GameObject headgearGo = new GameObject("ClassHeadgear");
            headgearGo.transform.SetParent(headGo.transform, false);
            headgearGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            SpriteRenderer headgearSr = headgearGo.AddComponent<SpriteRenderer>();
            headgearSr.sprite = gtHelmWarrior;
            headgearSr.sortingOrder = 11;

            GameObject offHandGo = new GameObject("OffHand");
            offHandGo.transform.SetParent(rigVisualRoot.transform, false);
            offHandGo.transform.localPosition = new Vector3(-0.23f, -0.14f, 0f);
            SpriteRenderer offHandSr = offHandGo.AddComponent<SpriteRenderer>();
            offHandSr.sprite = gtShield != null ? gtShield : gtHand;
            offHandSr.sortingOrder = 12;

            GameObject weaponPivotGo = new GameObject("WeaponPivot");
            weaponPivotGo.transform.SetParent(rigVisualRoot.transform, false);
            weaponPivotGo.transform.localPosition = new Vector3(0.22f, -0.15f, 0f);

            GameObject weaponSpriteGo = new GameObject("WeaponSprite");
            weaponSpriteGo.transform.SetParent(weaponPivotGo.transform, false);
            weaponSpriteGo.transform.localPosition = Vector3.zero;
            SpriteRenderer weaponSr = weaponSpriteGo.AddComponent<SpriteRenderer>();
            weaponSr.sprite = gtWpnWarrior != null ? gtWpnWarrior : oversizedSwordSprite;
            weaponSr.sortingOrder = 13;

            GameObject slashGo = new GameObject("SlashArc_VFX");
            slashGo.transform.SetParent(rigVisualRoot.transform, false);
            slashGo.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            SpriteRenderer slashSr = slashGo.AddComponent<SpriteRenderer>();
            slashSr.sprite = slashArcSprite;
            slashSr.sortingOrder = 15;
            slashSr.enabled = false;

            GuardianTalesCharacterRig gtRig = playerGo.AddComponent<GuardianTalesCharacterRig>();
            gtRig.ConfigureRig(
                rigVisualRoot.transform,
                footShadowSr,
                leftFootGo.transform,
                rightFootGo.transform,
                bodyGo.transform,
                bodySr,
                headGo.transform,
                headSr,
                headgearSr,
                offHandGo.transform,
                offHandSr,
                weaponPivotGo.transform,
                weaponSr,
                gtHeadNormal,
                gtHeadBlink,
                gtHeadAttack,
                gtHeadHurt,
                gtHelmWarrior,
                gtHoodScouter,
                gtHelmGuardian,
                gtWpnWarrior,
                gtWpnScouter,
                gtWpnGuardian,
                gtHand,
                gtShield,
                gtDustPuff);

            Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            CircleCollider2D playerCol = playerGo.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.4f;

            PlayerController playerController = playerGo.AddComponent<PlayerController>();
            playerController.ConfigureRig(gtRig);

            PlayerCombat playerCombat = playerGo.AddComponent<PlayerCombat>();
            playerCombat.ConfigureCombat(CharacterClass.Warrior, ~0, runeFire, runeLightning);
            playerCombat.ConfigureClassSprites(null, hunterSprite, scouterSprite, guardianSprite);
            playerCombat.ConfigureCharacterRig(gtRig);
            playerCombat.ConfigureWeaponRig(weaponPivotGo.transform, slashSr);
            playerCombat.ConfigureRunePresets(runeFire, runeLightning, runeIce, runeVampire, runeThorns);

            PlayerFootHPBar playerFootHp = playerGo.AddComponent<PlayerFootHPBar>();
            playerFootHp.Configure(playerCombat);

            camFollow.SetTarget(playerGo.transform);

            // 8. 모닥불 캠프 (Campfire_Base) 배치 및 행운 포자 스프 요리 연결
            GameObject campGo = new GameObject("Campfire_Base");
            campGo.transform.position = new Vector3(-2.5f, 1.5f, 0f);
            SpriteRenderer campSr = campGo.AddComponent<SpriteRenderer>();
            campSr.sprite = campfireSprite;
            campSr.sortingOrder = 5;
            CircleCollider2D campCol = campGo.AddComponent<CircleCollider2D>();
            campCol.isTrigger = true;
            campCol.radius = 0.7f;
            CampfireInteractable campfire = campGo.AddComponent<CampfireInteractable>();
            campfire.ConfigureCampfire(campSr, cookSoupRecipe);
            AttachStaticWorldLabel(campGo.transform, "🔥 모닥불 캠프 (요리/버프)", 1.05f, new Color(1f, 0.8f, 0.35f));

            // 9. 야생 채집물 (형광 버섯 군락) 2개 배치
            SpawnGatherNode("Gather_GlowMushroom_01", new Vector3(-3.2f, -1.2f, 0f), mushroomSprite, sporeItem);
            SpawnGatherNode("Gather_GlowMushroom_02", new Vector3(1.5f, 2.8f, 0f), mushroomSprite, sporeItem);

            // 10. 1챕터 6종 몬스터 서식지 배치 + 자동 리스폰 슬롯 등록 (발밑 compact HP 바 적용)
            SpawnAndRegisterMonster(spawnerManager, "Monster_HornedRabbit_01", new Vector3(2.5f, 0.5f, 0f), rabbitSprite, rabbitData, 0.45f, -0.46f, 10f);
            SpawnAndRegisterMonster(spawnerManager, "Monster_HornedRabbit_02", new Vector3(3.5f, -1.5f, 0f), rabbitSprite, rabbitData, 0.45f, -0.46f, 10f);
            SpawnAndRegisterMonster(spawnerManager, "Monster_SporeMushroom_01", new Vector3(-1.8f, -2.2f, 0f), mushroomMonsterSprite, mushroomData, 0.45f, -0.46f, 10f);
            SpawnAndRegisterMonster(spawnerManager, "Monster_SpikedBeetle_01", new Vector3(4.2f, 2.3f, 0f), beetleSprite, beetleData, 0.58f, -0.50f, 12f);
            SpawnAndRegisterMonster(spawnerManager, "Monster_WindWolf_01", new Vector3(6.2f, -1.8f, 0f), wolfSprite, wolfData, 0.60f, -0.52f, 12f);
            SpawnAndRegisterMonster(spawnerManager, "MiniBoss_BerserkDeokgu", new Vector3(5.8f, 1.2f, 0f), deokguSprite, deokguData, 0.72f, -0.58f, 18f);
            SpawnAndRegisterMonster(spawnerManager, "Boss_AncientEnt", new Vector3(9.5f, 0f, 0f), entBossSprite, entBossData, 1.45f, -0.74f, 30f);

            // 11. 모바일 스마트 HUD Canvas (조이스틱 + 스마트 버튼 + 클래스 스킬/견제 사격 + 인벤토리/압축/변이/제작 창) 생성
            CreateMobileHUD(playerCombat, playerController, attackSwordIcon, carveKnifeSprite, campfireSprite, mushroomSprite, ringSprite, whiteUiSprite,
                leather01, leather02, deokguFur, meat01, sporeItem, deokguRecipe, mutScaleItem, mutDragonScaleItem, mutationRecipe);

            // 12. 씬 저장
            string scenePath = $"{SceneFolder}/Chapter1_Prototype.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"★ [WildFrontier] 프로토타입 씬 생성 완료! ({scenePath})\n" +
                      "▶ Unity Play 버튼을 누른 뒤 WASD로 이동하고, 우측 하단 [공격/갈무리 스마트 버튼]을 길게 눌러 사냥과 시체 갈무리, 모닥불 캠프를 즉시 테스트해 보세요!");
        }

        private static void OnPlayModeChangedForRebuild(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChangedForRebuild;
                EditorApplication.delayCall += BuildPrototypeScene;
            }
        }

        [MenuItem("WildFrontier/Force Recompile Scripts (스크립트 재컴파일)")]
        public static void ForceRecompileScripts()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[WildFrontier] 스크립트 재컴파일을 요청했습니다.");
        }

        private static void SpawnGatherNode(string name, Vector3 pos, Sprite sprite, ItemDataSO item)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var node = go.AddComponent<GatheringNodeInteractable>();
            node.ConfigureNode("야생 형광 버섯 군락", item, sr);
            AttachStaticWorldLabel(go.transform, "🌿 형광 버섯", 0.85f, new Color(0.45f, 0.95f, 1f));
        }

        private static void SpawnAndRegisterMonster(MonsterSpawnerManager spawner, string name, Vector3 pos, Sprite sprite,
            MonsterDataSO data, float radius, float uiHeightOffset, float respawnDelay)
        {
            MonsterBase monster = SpawnMonster(name, pos, sprite, data, radius, uiHeightOffset);
            if (spawner != null)
            {
                spawner.RegisterSpawnSlot(name, pos, sprite, data, radius, uiHeightOffset, respawnDelay, monster);
            }
        }

        private static MonsterBase SpawnMonster(string name, Vector3 pos, Sprite sprite, MonsterDataSO data, float radius, float uiHeightOffset)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;

            GameObject visualGo = new GameObject("MonsterVisual");
            visualGo.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            MonsterCorpse corpse = go.AddComponent<MonsterCorpse>();
            MonsterBase monster = go.AddComponent<MonsterBase>();
            monster.ConfigureMonster(data, sr, corpse);

            MonsterWorldUI worldUi = go.AddComponent<MonsterWorldUI>();
            worldUi.InitializeWorldUI(monster, uiHeightOffset);
            return monster;
        }

        private static void AttachStaticWorldLabel(Transform parent, string label, float yOffset, Color color)
        {
            GameObject canvasGo = new GameObject("WorldLabel");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, yOffset, 0f);
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 25;
            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 30f);
            rt.localScale = Vector3.one * 0.011f;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(canvasGo.transform, false);
            RectTransform txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(220f, 30f);
            Text txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.color = color;
            txt.text = label;

            Outline ol = txtGo.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
            ol.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static void CreateMobileHUD(PlayerCombat combat, PlayerController controller,
            Sprite atkSprite, Sprite carveKnifeSprite, Sprite campSprite, Sprite gatherSprite, Sprite ringSprite, Sprite whiteUiSprite,
            ItemDataSO leather01, ItemDataSO leather02, ItemDataSO deokguFur, ItemDataSO meat01, ItemDataSO sporeItem, CraftingRecipeSO deokguRecipe,
            ItemDataSO mutScaleItem, ItemDataSO mutDragonScaleItem, CraftingRecipeSO mutationRecipe)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // EventSystem
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // Canvas
            GameObject canvasGo = new GameObject("Mobile_HUD_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            MobileHUDController hud = canvasGo.AddComponent<MobileHUDController>();
            HunterWorkshopHUD workshopHud = canvasGo.AddComponent<HunterWorkshopHUD>();

            // =========================================================================
            // A. 좌측 하단 가상 플로팅 조이스틱 (VirtualJoystickUI)
            // =========================================================================
            GameObject joyBgGo = new GameObject("VirtualJoystick_Area");
            joyBgGo.transform.SetParent(canvasGo.transform, false);
            RectTransform joyBgRect = joyBgGo.AddComponent<RectTransform>();
            joyBgRect.anchorMin = new Vector2(0f, 0f);
            joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(165f, 145f);
            joyBgRect.sizeDelta = new Vector2(170f, 170f);
            Image joyBgImg = joyBgGo.AddComponent<Image>();
            joyBgImg.sprite = ringSprite;
            joyBgImg.color = new Color(0.85f, 0.90f, 1.0f, 0.45f);

            GameObject joyKnobGo = new GameObject("Joystick_Handle");
            joyKnobGo.transform.SetParent(joyBgGo.transform, false);
            RectTransform joyKnobRect = joyKnobGo.AddComponent<RectTransform>();
            joyKnobRect.anchoredPosition = Vector2.zero;
            joyKnobRect.sizeDelta = new Vector2(72f, 72f);
            Image joyKnobImg = joyKnobGo.AddComponent<Image>();
            joyKnobImg.color = new Color(0.95f, 0.85f, 0.35f, 0.85f);

            VirtualJoystickUI joystick = joyBgGo.AddComponent<VirtualJoystickUI>();
            joystick.ConfigureJoystick(controller, joyBgRect, joyKnobRect, 58f);

            // =========================================================================
            // B. 좌측 상단 헌터 상태 & 룬 빌드 / 도감 / 펫 정보창 (상단 중앙 보스바와 겹치지 않도록 폭 430px)
            // =========================================================================
            GameObject topBarGo = new GameObject("Panel_TopLeftHunterStatus");
            topBarGo.transform.SetParent(canvasGo.transform, false);
            RectTransform topBarRect = topBarGo.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(0f, 1f);
            topBarRect.pivot = new Vector2(0f, 1f);
            topBarRect.anchoredPosition = new Vector2(20f, -18f);
            topBarRect.sizeDelta = new Vector2(430f, 110f);
            Image topBarBg = topBarGo.AddComponent<Image>();
            topBarBg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);

            Text statusText = CreateUIText(topBarGo.transform, "StatusText", font, 14, TextAnchor.MiddleLeft,
                new Vector2(12f, 0f), new Vector2(-24f, -10f));
            statusText.supportRichText = true;

            // =========================================================================
            // C. 우측 상단 퀵 액션 버튼 바 (클래스 전환 / 룬 빌드 전환 / 주변 펫 유혹 / 인벤토리·제작 창 토글)
            // =========================================================================
            CreateQuickButton(canvasGo.transform, "Btn_CycleClass", "⚔️ 클래스 전환\n(워리어/스카웃/반탱)",
                new Vector2(-330f, -45f), new Vector2(138f, 54f), new Color(0.24f, 0.32f, 0.48f, 0.92f), font,
                workshopHud.OnClickCycleClass);

            CreateQuickButton(canvasGo.transform, "Btn_TamePet", "🐾 주변 펫 유혹\n(뿔토끼/버섯/덕구)",
                new Vector2(-190f, -45f), new Vector2(130f, 54f), new Color(0.20f, 0.44f, 0.34f, 0.92f), font,
                workshopHud.OnClickTameNearestMonster);

            CreateQuickButton(canvasGo.transform, "Btn_ToggleWorkshop", "🎒 인벤토리/제작\n(열기 · 닫기)",
                new Vector2(-70f, -45f), new Vector2(102f, 54f), new Color(0.45f, 0.30f, 0.18f, 0.92f), font,
                workshopHud.OnToggleWorkshopPanel);

            CreateQuickButton(canvasGo.transform, "Btn_CycleRune", "💎 룬 빌드 전환\n(화염전격/냉기흡혈/반탱)",
                new Vector2(-330f, -106f), new Vector2(138f, 50f), new Color(0.32f, 0.24f, 0.52f, 0.92f), font,
                workshopHud.OnClickCycleRunePreset);

            // =========================================================================
            // D. 인벤토리 & 재료 압축 & 재료 변이 & 장비 제작 팝업 패널
            // =========================================================================
            GameObject workshopGo = new GameObject("Panel_InventoryAndCrafting");
            workshopGo.transform.SetParent(canvasGo.transform, false);
            RectTransform wsRect = workshopGo.AddComponent<RectTransform>();
            wsRect.anchorMin = new Vector2(0f, 1f);
            wsRect.anchorMax = new Vector2(0f, 1f);
            wsRect.pivot = new Vector2(0f, 1f);
            wsRect.anchoredPosition = new Vector2(20f, -138f);
            wsRect.sizeDelta = new Vector2(560f, 325f);
            Image wsBg = workshopGo.AddComponent<Image>();
            wsBg.color = new Color(0.10f, 0.11f, 0.16f, 0.92f);

            Text invText = CreateUIText(workshopGo.transform, "InventoryItemsText", font, 15, TextAnchor.UpperLeft,
                new Vector2(14f, -12f), new Vector2(-28f, -105f));
            invText.supportRichText = true;

            // 패널 하단 [재료 압축] / [재료 변이] / [장비 제작] 버튼 3종
            CreatePanelButton(workshopGo.transform, "Btn_CompressLeather",
                "⚗️ 재료 압축\n(거친 가죽 5 + 50G\n➔ 질긴 가죽 1)",
                new Vector2(98f, 45f), new Vector2(170f, 64f), new Color(0.22f, 0.42f, 0.55f, 1f), font,
                workshopHud.OnClickCompressLeather);

            CreatePanelButton(workshopGo.transform, "Btn_MutateMaterial",
                "🧬 재료 변이\n(질긴 가죽1+포자2\n➔ 비늘/용린 가죽)",
                new Vector2(280f, 45f), new Vector2(170f, 64f), new Color(0.38f, 0.25f, 0.56f, 1f), font,
                workshopHud.OnClickMutateMaterial);

            CreatePanelButton(workshopGo.transform, "Btn_CraftDeokguArmor",
                "🔨 레어 장비 제작\n[덕구 학살자의 견갑]\n제작 및 자동 장착",
                new Vector2(462f, 45f), new Vector2(170f, 64f), new Color(0.55f, 0.34f, 0.16f, 1f), font,
                workshopHud.OnClickCraftDeokguArmor);

            workshopHud.ConfigureWorkshopHUD(combat, leather01, leather02, deokguFur, meat01, sporeItem, deokguRecipe,
                statusText, invText, workshopGo);
            workshopHud.ConfigureMutationData(mutScaleItem, mutDragonScaleItem, mutationRecipe);

            // 시야가 시원하도록 워크샵 창은 시작 시 닫아두고 우측 상단 [🎒 인벤토리/제작] 버튼으로 열 수 있게 설정
            workshopGo.SetActive(false);

            // =========================================================================
            // E. 가디언 테일즈(ref/img) 스타일 상단 중앙 타겟 몬스터/보스 체력바
            // =========================================================================
            GameObject bossBarRoot = new GameObject("TopCenter_TargetMonsterBar");
            bossBarRoot.transform.SetParent(canvasGo.transform, false);
            RectTransform bossBarRect = bossBarRoot.AddComponent<RectTransform>();
            bossBarRect.anchorMin = new Vector2(0.5f, 1f);
            bossBarRect.anchorMax = new Vector2(0.5f, 1f);
            bossBarRect.pivot = new Vector2(0.5f, 1f);
            bossBarRect.anchoredPosition = new Vector2(0f, -18f);
            bossBarRect.sizeDelta = new Vector2(360f, 52f);

            Text targetNameTxt = CreateUIText(bossBarRoot.transform, "TargetMonsterName", font, 19, TextAnchor.UpperCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f));
            targetNameTxt.fontStyle = FontStyle.Bold;
            targetNameTxt.text = "광폭한 덕구";
            Outline nameOl = targetNameTxt.gameObject.AddComponent<Outline>();
            nameOl.effectColor = new Color(0f, 0f, 0f, 0.9f);
            nameOl.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject barBgGo = new GameObject("HPBar_Frame");
            barBgGo.transform.SetParent(bossBarRoot.transform, false);
            RectTransform barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.5f, 0f);
            barBgRect.anchorMax = new Vector2(0.5f, 0f);
            barBgRect.pivot = new Vector2(0.5f, 0f);
            barBgRect.anchoredPosition = new Vector2(0f, 2f);
            barBgRect.sizeDelta = new Vector2(360f, 24f);
            Image barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.08f, 0.08f, 0.10f, 0.95f);

            GameObject barFillGo = new GameObject("HPBar_Fill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            RectTransform barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin = new Vector2(0f, 0f);
            barFillRect.anchorMax = new Vector2(0f, 1f);
            barFillRect.pivot = new Vector2(0f, 0.5f);
            barFillRect.anchoredPosition = new Vector2(2f, 0f);
            barFillRect.sizeDelta = new Vector2(356f, -4f);
            Image barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.color = new Color(0.95f, 0.18f, 0.18f, 1f);

            Text targetHpValTxt = CreateUIText(barBgGo.transform, "HPValueText", font, 14, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero);
            targetHpValTxt.fontStyle = FontStyle.Bold;
            targetHpValTxt.text = "550 / 550";

            workshopHud.ConfigureTargetMonsterBar(bossBarRoot, targetNameTxt, targetHpValTxt, barFillImg);

            // 우측 하단 스마트 액션 버튼 (공격/차지 <-> 시체 갈무리 <-> 채집 <-> 모닥불 캠프 자동 전환)
            GameObject smartBtnGo = new GameObject("Btn_SmartContextAction");
            smartBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform btnRect = smartBtnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0f);
            btnRect.anchorMax = new Vector2(1f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(-150f, 140f);
            btnRect.sizeDelta = new Vector2(150f, 150f);

            Image btnBg = smartBtnGo.AddComponent<Image>();
            btnBg.color = new Color(0.16f, 0.14f, 0.22f, 0.92f);

            // 갈무리 & 차지 홀드 게이지 원형 링
            GameObject ringGo = new GameObject("HoldProgressRing");
            ringGo.transform.SetParent(smartBtnGo.transform, false);
            RectTransform ringRect = ringGo.AddComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.sizeDelta = new Vector2(16f, 16f);
            Image ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = (int)Image.Origin360.Top;
            ringImg.fillAmount = 0f;
            ringImg.color = new Color(1.0f, 0.85f, 0.20f, 1.0f);

            // 아이콘
            GameObject iconGo = new GameObject("ActionIcon");
            iconGo.transform.SetParent(smartBtnGo.transform, false);
            RectTransform iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0f, 12f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = atkSprite;

            // 텍스트 라벨
            GameObject labelGo = new GameObject("ActionLabel");
            labelGo.transform.SetParent(smartBtnGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0f, -45f);
            labelRect.sizeDelta = new Vector2(140f, 36f);
            Text labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = "공격 / 차지";

            // EventTrigger로 PointerDown / PointerUp 영구 바인딩
            EventTrigger trigger = smartBtnGo.AddComponent<EventTrigger>();
            AddEventTriggerListener(trigger, EventTriggerType.PointerDown, hud.OnSmartActionPointerDown);
            AddEventTriggerListener(trigger, EventTriggerType.PointerUp, hud.OnSmartActionPointerUp);

            // 회피(Dodge) 버튼
            GameObject dodgeBtnGo = new GameObject("Btn_Dodge");
            dodgeBtnGo.transform.SetParent(canvasGo.transform, false);
            RectTransform dodgeRect = dodgeBtnGo.AddComponent<RectTransform>();
            dodgeRect.anchorMin = new Vector2(1f, 0f);
            dodgeRect.anchorMax = new Vector2(1f, 0f);
            dodgeRect.anchoredPosition = new Vector2(-310f, 100f);
            dodgeRect.sizeDelta = new Vector2(95f, 95f);
            Image dodgeBg = dodgeBtnGo.AddComponent<Image>();
            dodgeBg.color = new Color(0.22f, 0.30f, 0.42f, 0.9f);
            Button dodgeBtn = dodgeBtnGo.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(dodgeBtn.onClick, hud.OnDodgeButtonClicked);

            GameObject dodgeTxtGo = new GameObject("Text");
            dodgeTxtGo.transform.SetParent(dodgeBtnGo.transform, false);
            RectTransform dodgeTxtRect = dodgeTxtGo.AddComponent<RectTransform>();
            dodgeTxtRect.anchorMin = Vector2.zero;
            dodgeTxtRect.anchorMax = Vector2.one;
            dodgeTxtRect.sizeDelta = Vector2.zero;
            Text dodgeTxt = dodgeTxtGo.AddComponent<Text>();
            dodgeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dodgeTxt.fontSize = 18;
            dodgeTxt.alignment = TextAnchor.MiddleCenter;
            dodgeTxt.color = Color.white;
            dodgeTxt.text = "회피\n(Step)";

            // 클래스 액티브 스킬 버튼 (Q)
            CreateSkillActionButton(canvasGo.transform, "Btn_ClassSkill", "⚡ 대지 돌파\n(Q)",
                new Vector2(-150f, 305f), new Vector2(115f, 95f), new Color(0.42f, 0.24f, 0.56f, 0.92f),
                font, whiteUiSprite, hud.OnClassSkillButtonClicked, out Text classSkillLabel, out Image classSkillCdFill);

            // 보조 총기 견제 사격 버튼 (E)
            CreateSkillActionButton(canvasGo.transform, "Btn_SubGun", "🔫 견제 사격\n(E)",
                new Vector2(-295f, 225f), new Vector2(105f, 90f), new Color(0.50f, 0.34f, 0.16f, 0.92f),
                font, whiteUiSprite, hud.OnSubGunButtonClicked, out _, out Image subGunCdFill);

            hud.ConfigureHUD(combat, controller, iconImg, ringImg, labelText, atkSprite, carveKnifeSprite, gatherSprite, campSprite);
            hud.ConfigureSkillButtons(classSkillLabel, classSkillCdFill, subGunCdFill);
        }

        private static void CreateSkillActionButton(Transform canvasParent, string name, string defaultLabel,
            Vector2 anchoredPos, Vector2 size, Color bgColor, Font font, Sprite cdSprite, UnityAction onClickAction,
            out Text labelOut, out Image cooldownFillOut)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(canvasParent, false);
            RectTransform rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = btnGo.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGo.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(btn.onClick, onClickAction);

            // 쿨타임 방사형 오버레이 (raycastTarget = false로 버튼 클릭 방해 금지)
            GameObject cdGo = new GameObject("CooldownOverlay");
            cdGo.transform.SetParent(btnGo.transform, false);
            RectTransform cdRect = cdGo.AddComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.sizeDelta = Vector2.zero;
            cooldownFillOut = cdGo.AddComponent<Image>();
            cooldownFillOut.sprite = cdSprite;
            cooldownFillOut.type = Image.Type.Filled;
            cooldownFillOut.fillMethod = Image.FillMethod.Radial360;
            cooldownFillOut.fillOrigin = (int)Image.Origin360.Top;
            cooldownFillOut.fillAmount = 0f;
            cooldownFillOut.color = new Color(0f, 0f, 0f, 0.62f);
            cooldownFillOut.raycastTarget = false;

            labelOut = CreateUIText(btnGo.transform, "Text", font, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            labelOut.fontStyle = FontStyle.Bold;
            labelOut.text = defaultLabel;
            labelOut.raycastTarget = false;
        }

        private static Text CreateUIText(Transform parent, string name, Font font, int fontSize, TextAnchor anchor,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Text txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            return txt;
        }

        private static void CreateQuickButton(Transform canvasParent, string name, string label,
            Vector2 anchoredPos, Vector2 size, Color bgColor, Font font, UnityAction onClickAction)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(canvasParent, false);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;
            Button btn = btnGo.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(btn.onClick, onClickAction);

            Text txt = CreateUIText(btnGo.transform, "Text", font, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            txt.text = label;
        }

        private static void CreatePanelButton(Transform panelParent, string name, string label,
            Vector2 anchoredPos, Vector2 size, Color bgColor, Font font, UnityAction onClickAction)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(panelParent, false);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            Image img = btnGo.AddComponent<Image>();
            img.color = bgColor;
            Button btn = btnGo.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(btn.onClick, onClickAction);

            Text txt = CreateUIText(btnGo.transform, "Text", font, 13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            txt.text = label;
        }

        private static void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            UnityEventTools.AddVoidPersistentListener(entry.callback, action);
            trigger.triggers.Add(entry);
        }

        private static Sprite CreatePixelSprite(string name, int width, int height, Color bodyColor, Color accentColor)
        {
            string path = $"{SpriteFolder}/{name}.png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color outline = new Color(0.10f, 0.08f, 0.16f, 1f);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float nx = (x - width * 0.5f) / (width * 0.42f);
                        float ny = (y - height * 0.5f) / (height * 0.42f);
                        float dist = nx * nx + ny * ny;

                        if (dist > 1.0f)
                            tex.SetPixel(x, y, Color.clear);
                        else if (dist > 0.78f)
                            tex.SetPixel(x, y, outline);
                        else if (y > height * 0.58f && Mathf.Abs(nx) < 0.45f)
                            tex.SetPixel(x, y, accentColor);
                        else
                            tex.SetPixel(x, y, bodyColor);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateCircleRingSprite(string name, int size)
        {
            string path = $"{SpriteFolder}/{name}.png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x - size * 0.5f) / (size * 0.5f);
                        float dy = (y - size * 0.5f) / (size * 0.5f);
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        tex.SetPixel(x, y, (r >= 0.78f && r <= 0.96f) ? Color.white : Color.clear);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateSolidSquareSprite(string name, int size)
        {
            string path = $"{SpriteFolder}/{name}.png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] cols = new Color[size * size];
                for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
                tex.SetPixels(cols);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
#endif
