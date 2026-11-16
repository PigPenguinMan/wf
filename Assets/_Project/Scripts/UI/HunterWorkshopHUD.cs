using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WildFrontier.Core;
using WildFrontier.Data;
using WildFrontier.Monster;
using WildFrontier.Player;
using WildFrontier.Systems;

namespace WildFrontier.UI
{
    /// <summary>
    /// 좌측 상단 헌터 상태바(클래스, HP, 시간/날씨, 동행 펫) 및
    /// 우측 상단/중앙의 [인벤토리 / 재료 압축 / 장비 제작 / 펫 유혹 / 클래스 전환] 통합 워크샵 UI를 제어합니다.
    /// </summary>
    public class HunterWorkshopHUD : MonoBehaviour
    {
        [Header("플레이어 및 데이터 참조")]
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private ItemDataSO roughLeatherItem;      // MAT_LEATHER_01 (거친 가죽)
        [SerializeField] private ItemDataSO toughLeatherItem;      // MAT_LEATHER_02 (질긴 가죽)
        [SerializeField] private ItemDataSO deokguFurItem;         // MAT_DEOKGU_FUR (덕구의 황금 털가죽)
        [SerializeField] private ItemDataSO meatItem;              // MAT_MEAT_01 (질긴 고기)
        [SerializeField] private ItemDataSO sporeItem;             // MAT_SPORE_01 (형광 포자 가루)
        [SerializeField] private ItemDataSO mutScaleItem;          // MAT_MUT_SCALE (변이된 비늘 가죽)
        [SerializeField] private ItemDataSO mutDragonScaleItem;    // MAT_MUT_DRAGONSCALE (용린화 가죽)
        [SerializeField] private CraftingRecipeSO deokguArmorRecipe; // REC_EQ_DEOKGU (덕구 학살자의 견갑)
        [SerializeField] private CraftingRecipeSO mutationRecipe;    // REC_MUT_01 (변이 비늘 가죽 연성)

        [Header("UI 텍스트 참조")]
        [SerializeField] private Text topStatusText;
        [SerializeField] private Text inventoryContentText;
        [SerializeField] private GameObject workshopPanelRoot;

        [Header("가디언 테일즈풍 상단 중앙 타겟 몬스터 체력바")]
        [SerializeField] private GameObject topTargetBarRoot;
        [SerializeField] private Text topTargetNameText;
        [SerializeField] private Text topTargetHpValueText;
        [SerializeField] private Image topTargetHpFillImage;

        public void ConfigureTargetMonsterBar(GameObject barRoot, Text nameTxt, Text hpValTxt, Image fillImg)
        {
            topTargetBarRoot = barRoot;
            topTargetNameText = nameTxt;
            topTargetHpValueText = hpValTxt;
            topTargetHpFillImage = fillImg;
        }

        public void ConfigureWorkshopHUD(
            PlayerCombat combat,
            ItemDataSO leather01,
            ItemDataSO leather02,
            ItemDataSO deokguFur,
            ItemDataSO meat,
            ItemDataSO spore,
            CraftingRecipeSO deokguRecipe,
            Text statusTxt,
            Text invTxt,
            GameObject panelRoot)
        {
            playerCombat = combat;
            roughLeatherItem = leather01;
            toughLeatherItem = leather02;
            deokguFurItem = deokguFur;
            meatItem = meat;
            sporeItem = spore;
            deokguArmorRecipe = deokguRecipe;
            topStatusText = statusTxt;
            inventoryContentText = invTxt;
            workshopPanelRoot = panelRoot;
        }

        public void ConfigureMutationData(ItemDataSO mutScale, ItemDataSO mutDragonScale, CraftingRecipeSO mutRecipe)
        {
            mutScaleItem = mutScale;
            mutDragonScaleItem = mutDragonScale;
            mutationRecipe = mutRecipe;
        }

        private void Update()
        {
            RefreshStatusAndInventory();
            RefreshTopTargetMonsterBar();
        }

        private void RefreshTopTargetMonsterBar()
        {
            if (topTargetBarRoot == null || playerCombat == null) return;

            MonsterBase target = playerCombat.LastTargetedMonster;
            if (target == null || target.Data == null)
            {
                // 아직 타격한 몬스터가 없으면 가장 가까운 살아있는 몬스터(또는 보스)를 자동 표시
                MonsterBase[] all = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
                float minDist = 7.5f;
                foreach (var m in all)
                {
                    if (m != null && !m.IsDead && m.Data != null)
                    {
                        float d = Vector2.Distance(playerCombat.transform.position, m.transform.position);
                        if (d < minDist)
                        {
                            minDist = d;
                            target = m;
                        }
                    }
                }
            }

            if (target == null || target.Data == null)
            {
                topTargetBarRoot.SetActive(false);
                return;
            }

            topTargetBarRoot.SetActive(true);
            if (topTargetNameText != null)
            {
                topTargetNameText.text = target.IsDead
                    ? $"🔪 {target.Data.monsterName} (시체 갈무리 가능)"
                    : target.Data.monsterName;
            }

            int maxHp = Mathf.Max(1, target.Data.maxHp);
            int curHp = Mathf.Clamp(target.CurrentHp, 0, maxHp);

            if (topTargetHpValueText != null)
            {
                topTargetHpValueText.text = $"{curHp} / {maxHp}";
            }

            if (topTargetHpFillImage != null)
            {
                float ratio = (float)curHp / maxHp;
                topTargetHpFillImage.rectTransform.sizeDelta = new Vector2(356f * ratio, -4f);
            }
        }

        private void RefreshStatusAndInventory()
        {
            if (topStatusText != null && playerCombat != null)
            {
                var env = WorldEnvironmentManager.Instance;
                var petSys = PetTamingSystem.Instance;
                var bestiary = BestiaryManager.Instance;

                string timeStr = env != null ? env.CurrentTimeOfDay.ToString() : "Day";
                string weatherStr = env != null ? env.CurrentWeather.ToString() : "Clear";
                string buffStr = (env != null && env.HasCarveLuckBuff) ? "🔥[캠프 요리+15%]" : "";

                string petStr = "없음 ([펫 유혹] 클릭)";
                if (petSys != null && petSys.ActivePet != null && petSys.ActivePet.petOriginData != null)
                {
                    var p = petSys.ActivePet;
                    petStr = $"{p.customNickname} (Lv.{p.currentLevel}) {(p.isSpecialSkillAwakened ? "★특성개방!" : "")}";
                }

                string bestiaryStr = bestiary != null
                    ? $"📖 도감 Lv.{bestiary.TotalMasteryLevels} (공+{bestiary.TotalBonusAttack} / 행운+{bestiary.TotalBonusCarveLuck * 100f:F0}%)"
                    : "📖 도감 Lv.0";

                topStatusText.text =
                    $"<b>[{playerCombat.ActiveClass}]</b> HP: {playerCombat.CurrentHp}/{playerCombat.MaxHp} | 💰 {(InventoryAndCraftingManager.Instance != null ? InventoryAndCraftingManager.Instance.CurrentGold : 0)} G\n" +
                    $"💎 룬: {playerCombat.CurrentRunePresetName}\n" +
                    $"🌦️ {timeStr}/{weatherStr} {buffStr} | {bestiaryStr}\n" +
                    $"🐾 펫: {petStr}";
            }

            if (inventoryContentText != null && InventoryAndCraftingManager.Instance != null)
            {
                var inv = InventoryAndCraftingManager.Instance;
                int cLeather1 = inv.GetItemCount(roughLeatherItem);
                int cLeather2 = inv.GetItemCount(toughLeatherItem);
                int cDeokgu = inv.GetItemCount(deokguFurItem);
                int cMeat = inv.GetItemCount(meatItem);
                int cSpore = inv.GetItemCount(sporeItem);
                int cMutScale = inv.GetItemCount(mutScaleItem);
                int cMutDragon = inv.GetItemCount(mutDragonScaleItem);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<b>📦 [보유 갈무리/채집 재료 현황]</b>");
                sb.AppendLine($"• 거친 가죽 (일반): <b>{cLeather1}</b>개  <i>(5개 모아 질긴 가죽으로 압축 가능)</i>");
                sb.AppendLine($"• 질긴 가죽 (고급): <b>{cLeather2}</b>개  |  덕구 황금 털가죽: <b>{cDeokgu}</b>개");
                sb.AppendLine($"• 질긴 고기: <b>{cMeat}</b>개  |  형광 포자 가루: <b>{cSpore}</b>개");
                sb.AppendLine($"• <color=#38BDF8>변이된 비늘 가죽(희귀): <b>{cMutScale}</b>개</color>  |  <color=#C084FC>용린화 가죽(영웅): <b>{cMutDragon}</b>개</color>");

                if (deokguArmorRecipe != null && deokguArmorRecipe.resultItem != null)
                {
                    int armorCount = inv.GetItemCount(deokguArmorRecipe.resultItem);
                    sb.AppendLine($"\n🛡️ <b>[제작 장비] 덕구 학살자의 견갑</b> 보유: <b>{armorCount}</b>개");
                    sb.AppendLine("   <color=#FBBF24>장비: 덕구 털가죽x1 + 질긴 가죽x3 (300G) | 변이: 질긴 가죽x1 + 포자x2 (120G)</color>");
                }

                inventoryContentText.text = sb.ToString();
            }
        }

        /// <summary>
        /// 인벤토리/압축/제작 패널 열기·닫기 토글
        /// </summary>
        public void OnToggleWorkshopPanel()
        {
            if (workshopPanelRoot != null)
            {
                workshopPanelRoot.SetActive(!workshopPanelRoot.activeSelf);
            }
        }

        /// <summary>
        /// [재료 압축 버튼] 거친 가죽 5개 -> 질긴 가죽 1개 압축 실행
        /// </summary>
        public void OnClickCompressLeather()
        {
            if (InventoryAndCraftingManager.Instance == null || roughLeatherItem == null) return;

            bool ok = InventoryAndCraftingManager.Instance.TryCompressMaterial(roughLeatherItem, 1);
            if (!ok && playerCombat != null)
            {
                FloatingPopupManager.SpawnPopup(playerCombat.transform.position + Vector3.up * 0.9f,
                    "⚠️ 거친 가죽 5개와 50G가 필요합니다!", new Color(1f, 0.45f, 0.45f), 20, 1.4f);
            }
        }

        /// <summary>
        /// [장비 제작 버튼] 덕구 학살자의 견갑 제작 실행
        /// </summary>
        public void OnClickCraftDeokguArmor()
        {
            if (InventoryAndCraftingManager.Instance == null || deokguArmorRecipe == null) return;

            bool ok = InventoryAndCraftingManager.Instance.TryExecuteRecipe(deokguArmorRecipe);
            if (ok && playerCombat != null && deokguArmorRecipe.resultItem != null)
            {
                playerCombat.EquipItemBonus(deokguArmorRecipe.resultItem);
            }
            else if (!ok && playerCombat != null)
            {
                FloatingPopupManager.SpawnPopup(playerCombat.transform.position + Vector3.up * 0.9f,
                    "⚠️ 재료 부족! (덕구 털가죽x1, 질긴 가죽x3, 300G 필요)", new Color(1f, 0.45f, 0.45f), 20, 1.5f);
            }
        }

        /// <summary>
        /// [재료 변이 버튼] 질긴 가죽 x1 + 형광 포자 가루 x2 ➔ 변이된 비늘 가죽(대성공 20% 시 용린화 가죽) 연성 실행
        /// </summary>
        public void OnClickMutateMaterial()
        {
            if (InventoryAndCraftingManager.Instance == null || mutationRecipe == null) return;

            bool ok = InventoryAndCraftingManager.Instance.TryExecuteRecipe(mutationRecipe);
            if (!ok && playerCombat != null)
            {
                FloatingPopupManager.SpawnPopup(playerCombat.transform.position + Vector3.up * 0.9f,
                    "⚠️ 재료 부족! (질긴 가죽x1 + 형광 포자 가루x2 + 120G 필요)", new Color(1f, 0.45f, 0.45f), 20, 1.5f);
            }
        }

        /// <summary>
        /// [펫 유혹 버튼] 플레이어 주변의 가장 가까운 소형 몬스터(뿔토끼 또는 광폭한 덕구) 테이밍 시도!
        /// </summary>
        public void OnClickTameNearestMonster()
        {
            if (playerCombat == null || PetTamingSystem.Instance == null) return;

            MonsterBase[] allMonsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
            MonsterBase nearestTameable = null;
            float minDist = 4.5f;

            foreach (var m in allMonsters)
            {
                if (m != null && !m.IsDead && m.Data != null && m.Data.canBeTamed)
                {
                    float d = Vector2.Distance(playerCombat.transform.position, m.transform.position);
                    if (d <= minDist)
                    {
                        minDist = d;
                        nearestTameable = m;
                    }
                }
            }

            if (nearestTameable == null)
            {
                FloatingPopupManager.SpawnPopup(playerCombat.transform.position + Vector3.up * 0.9f,
                    "🐾 주변에 유혹할 수 있는 소형 몬스터(뿔토끼/덕구)가 없습니다!", new Color(1f, 0.75f, 0.35f), 20, 1.5f);
                return;
            }

            bool tamed = PetTamingSystem.Instance.TryTameMonster(nearestTameable, 5, ignoreBaitRequirementForPrototype: true);
            if (tamed)
            {
                FloatingPopupManager.SpawnPopup(playerCombat.transform.position + Vector3.up * 1.1f,
                    $"🐾 ★펫 유혹 성공! [{nearestTameable.Data.monsterName}] 동행 시작!", new Color(0.4f, 1f, 0.85f), 24, 2.2f);
            }
            else
            {
                FloatingPopupManager.SpawnPopup(nearestTameable.transform.position + Vector3.up * 0.8f,
                    $"🐾 유혹 실패! (몬스터 체력을 낮추면 확률이 올라갑니다)", new Color(1f, 0.6f, 0.3f), 20, 1.5f);
            }
        }

        /// <summary>
        /// [클래스 전환 버튼] 워리어 ➔ 스카우터 ➔ 가디언(반사 탱커) 순환 전환
        /// </summary>
        public void OnClickCycleClass()
        {
            if (playerCombat == null) return;
            playerCombat.CycleCharacterClass();
        }

        /// <summary>
        /// [룬 빌드 전환 버튼] 5대 룬 프리셋 순환 전환
        /// </summary>
        public void OnClickCycleRunePreset()
        {
            if (playerCombat == null) return;
            playerCombat.CycleRunePreset();
        }
    }
}
