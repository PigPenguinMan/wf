using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;
using WildFrontier.Systems;

namespace WildFrontier.Monster
{
    /// <summary>
    /// [와일드 프론티어의 상징적 시스템] 몬스터 시체 갈무리 (Carving) 컴포넌트.
    /// 몬스터 처치 시 생성/활성화되며, 시체 위에서 공격(스마트 액션) 버튼을 길게 누르면
    /// 게이지가 차오르며 전리품, 장비 제작법(레시피북), 룬을 획득합니다.
    /// </summary>
    public class MonsterCorpse : MonoBehaviour, IHoldInteractable
    {
        [Header("갈무리 상태")]
        [SerializeField] private MonsterDataSO monsterData;
        [SerializeField] private bool isOverkillBonus; // 약점 속성 또는 차지 스킬로 처치 시 희귀 드랍률 상승
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ParticleSystem sparkleEffect; // 오버킬/희귀 시체 반짝임 도트 파티클

        private bool _isCarved;
        private float _despawnTimer;

        public ActionContextType ContextType => ActionContextType.Carve;
        public bool CanInteract => !_isCarved && monsterData != null;
        public float RequiredHoldDuration => monsterData != null ? monsterData.carveHoldTime : 1.2f;
        public Vector3 Position => transform.position;

        /// <summary>
        /// 몬스터가 사망할 때 MonsterBase에서 호출하여 시체 상태를 초기화합니다.
        /// </summary>
        public void InitializeCorpse(MonsterDataSO data, bool killedByWeaknessOrOverkill, SpriteRenderer sr = null)
        {
            monsterData = data;
            isOverkillBonus = killedByWeaknessOrOverkill;
            if (sr != null) spriteRenderer = sr;
            _isCarved = false;
            _despawnTimer = data != null ? data.corpseDuration : 20f;

            if (sparkleEffect != null)
            {
                if (isOverkillBonus) sparkleEffect.Play();
                else sparkleEffect.Stop();
            }
        }

        private void Update()
        {
            if (_isCarved) return;

            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void OnInteractStart(GameObject interactor)
        {
            Debug.Log($"[갈무리 시작] '{monsterData.monsterName}' 시체 갈무리 중... (소요 시간: {RequiredHoldDuration:F1}초)");
        }

        public void OnInteractProgress(float normalizedProgress)
        {
            // 게이지가 차오를 때 픽셀 스프라이트가 살짝 흔들리는 연출
            if (spriteRenderer != null)
            {
                float shakeOffset = Mathf.Sin(normalizedProgress * 40f) * 0.03f;
                spriteRenderer.transform.localPosition = new Vector3(shakeOffset, 0f, 0f);
            }
        }

        public void OnInteractComplete(GameObject interactor)
        {
            if (_isCarved) return;
            _isCarved = true;

            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = Vector3.zero;

            if (BestiaryManager.Instance != null)
                BestiaryManager.Instance.RecordCarve(monsterData);

            ExecuteCarvingLoot();

            // 주변 시체 연속 갈무리(Quick Carve) 특성 지원 가능 지점
            FadeOutAndDestroy();
        }

        public void OnInteractCancel()
        {
            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = Vector3.zero;

            Debug.Log("[갈무리 취소] 버튼에서 손을 떼거나 피격당해 갈무리가 중단되었습니다.");
        }

        private void ExecuteCarvingLoot()
        {
            if (monsterData == null || InventoryAndCraftingManager.Instance == null) return;

            List<LootDropEntry> lootTable = monsterData.carvingLootTable;
            int acquiredCount = 0;

            foreach (var entry in lootTable)
            {
                if (entry.item == null) continue;

                float finalChance = entry.baseDropChance;
                if (isOverkillBonus)
                {
                    finalChance += entry.overkillBonusChance;
                }
                if (WorldEnvironmentManager.Instance != null)
                {
                    finalChance += WorldEnvironmentManager.Instance.ActiveCarveLuckBonus;
                }
                if (BestiaryManager.Instance != null)
                {
                    finalChance += BestiaryManager.Instance.TotalBonusCarveLuck;
                }

                if (Random.value <= finalChance)
                {
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    if (amount > 0)
                    {
                        InventoryAndCraftingManager.Instance.AddItem(entry.item, amount);
                        acquiredCount++;
                    }
                }
            }

            if (acquiredCount == 0)
            {
                Debug.Log($"[갈무리 완료] '{monsterData.monsterName}'에게서 온전한 재료를 건지지 못했습니다.");
            }
        }

        private void FadeOutAndDestroy()
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                spriteRenderer.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.45f);
            }
            Destroy(gameObject, 0.6f);
        }
    }
}
