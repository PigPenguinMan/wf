using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;
using WildFrontier.Monster;

namespace WildFrontier.Systems
{
    /// <summary>
    /// [와일드 프론티어 핵심 3] 소형(S) 몬스터 및 특수 네임드('덕구' 등) 유혹(펫 테이밍) 및 동반 성장 시스템.
    /// 주인공과 동등한 레벨까지 키우면 일정 확률로 특수 패시브 능력이 개방됩니다.
    /// </summary>
    public class PetTamingSystem : MonoBehaviour
    {
        public static PetTamingSystem Instance { get; private set; }

        [System.Serializable]
        public class TamedPetInstance
        {
            public MonsterDataSO petOriginData;
            public string customNickname;
            public int currentLevel;
            public int currentExp;
            public bool isSpecialSkillAwakened; // 주인공 레벨 도달 시 개방되는 특수 능력 여부
        }

        [Header("현재 동행 중인 펫")]
        [SerializeField] private TamedPetInstance activePet;

        private Transform _spawnedPetVisual;
        private Transform _followTarget;

        public TamedPetInstance ActivePet => activePet;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (_spawnedPetVisual != null && _followTarget != null)
            {
                Vector3 targetPos = _followTarget.position + new Vector3(-0.75f, 0.35f, 0f);
                _spawnedPetVisual.position = Vector3.Lerp(_spawnedPetVisual.position, targetPos, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// 필드의 소형 몬스터에게 먹이를 던져 유혹(테이밍)을 시도합니다.
        /// </summary>
        public bool TryTameMonster(MonsterBase targetMonster, int playerLevel, bool ignoreBaitRequirementForPrototype = false)
        {
            if (targetMonster == null || targetMonster.IsDead || targetMonster.Data == null)
                return false;

            MonsterDataSO data = targetMonster.Data;
            if (!data.canBeTamed || data.size != MonsterSize.Small)
            {
                Debug.LogWarning($"[펫 유혹] '{data.monsterName}'은(는) 길들일 수 없는 몬스터입니다.");
                return false;
            }

            // 선호 먹이 보유 확인 및 소모
            if (!ignoreBaitRequirementForPrototype && data.favoriteBaitItem != null && InventoryAndCraftingManager.Instance != null)
            {
                if (!InventoryAndCraftingManager.Instance.RemoveItem(data.favoriteBaitItem, 1))
                {
                    Debug.LogWarning($"[펫 유혹] '{data.favoriteBaitItem.itemName}' 먹이가 필요합니다!");
                    return false;
                }
            }

            // 체력이 낮을수록 유혹 확률 상승 (최대 +45% 보너스)
            float hpRatio = Mathf.Clamp01((float)targetMonster.CurrentHp / data.maxHp);
            float finalChance = data.baseTameChance + (1f - hpRatio) * 0.45f;

            if (Random.value <= finalChance)
            {
                // 원작 고증: 처음 유혹해 데려올 때는 주인공보다 낮은 레벨로 시작
                int startLevel = Mathf.Max(1, playerLevel - Random.Range(1, 3));
                activePet = new TamedPetInstance
                {
                    petOriginData = data,
                    customNickname = data.monsterName,
                    currentLevel = startLevel,
                    currentExp = 0,
                    isSpecialSkillAwakened = false
                };

                SpawnFollowerPetVisual(targetMonster);
                if (BestiaryManager.Instance != null)
                    BestiaryManager.Instance.RecordTame(data);

                Debug.Log($"[펫 유혹 성공!] '{data.monsterName}'(Lv.{startLevel})이(가) 당신의 동료가 되었습니다!");
                Destroy(targetMonster.gameObject);
                return true;
            }

            Debug.Log($"[펫 유혹 실패] '{data.monsterName}'이(가) 먹이만 먹고 경계하고 있습니다.");
            return false;
        }

        private void SpawnFollowerPetVisual(MonsterBase tamedMonster)
        {
            if (_spawnedPetVisual != null)
                Destroy(_spawnedPetVisual.gameObject);

            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player != null)
                _followTarget = player.transform;

            SpriteRenderer origSr = tamedMonster.GetComponent<SpriteRenderer>();
            GameObject petGo = new GameObject($"CompanionPet_{tamedMonster.Data.monsterName}");
            petGo.transform.position = tamedMonster.transform.position;
            petGo.transform.localScale = Vector3.one * 0.85f;

            SpriteRenderer sr = petGo.AddComponent<SpriteRenderer>();
            if (origSr != null)
            {
                sr.sprite = origSr.sprite;
                sr.sortingOrder = 9;
            }
            _spawnedPetVisual = petGo.transform;
        }

        /// <summary>
        /// 주인공과 함께 사냥하여 펫이 레벨업할 때 호출됩니다.
        /// 주인공과 동등한 레벨에 도달하면 특수 능력이 개방됩니다.
        /// </summary>
        public void AddPetExp(int exp, int playerLevel)
        {
            if (activePet == null || activePet.petOriginData == null) return;

            activePet.currentExp += exp;
            int requiredExp = activePet.currentLevel * 50;

            if (activePet.currentExp >= requiredExp && activePet.currentLevel < playerLevel)
            {
                activePet.currentExp -= requiredExp;
                activePet.currentLevel++;

                Debug.Log($"[펫 성장] '{activePet.customNickname}'의 레벨이 {activePet.currentLevel}(으)로 올랐습니다!");

                if (!activePet.isSpecialSkillAwakened && activePet.currentLevel >= playerLevel)
                {
                    activePet.isSpecialSkillAwakened = true;
                    Debug.Log($"★[펫 잠재력 개방] '{activePet.customNickname}'이(가) 주인공과 동등한 경지에 올라 특수 능력 [{activePet.petOriginData.awakenedPetSkillDescription}]을(를) 개방했습니다!");
                }
            }
        }
    }
}
