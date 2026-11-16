using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;

namespace WildFrontier.Systems
{
    /// <summary>
    /// [모닥불 캠프 (Campfire) 상호작용 컴포넌트]
    /// 플레이어가 모닥불 근처에 다가가면 스마트 액션 버튼이 🔥 캠프 제작으로 전환되며,
    /// 홀드 완료 시 체력 회복 + 모닥불 특선 요리 버프(갈무리 희귀 재료 확률 +15%, 공격력 +10%) 및 재료 압축을 수행합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CampfireInteractable : MonoBehaviour, IHoldInteractable
    {
        [Header("모닥불 캠프 설정")]
        [SerializeField] private float interactHoldSeconds = 0.8f;
        [SerializeField] private float mealBuffDuration = 300f; // 5분 버프
        [SerializeField] private CraftingRecipeSO defaultCampfireMealRecipe;
        [SerializeField] private SpriteRenderer flameRenderer;

        private float _flickerSeed;

        public ActionContextType ContextType => ActionContextType.Campfire;
        public bool CanInteract => true;
        public float RequiredHoldDuration => interactHoldSeconds;
        public Vector3 Position => transform.position;

        public void ConfigureCampfire(SpriteRenderer flame, CraftingRecipeSO mealRecipe = null)
        {
            flameRenderer = flame;
            defaultCampfireMealRecipe = mealRecipe;
        }

        private void Start()
        {
            _flickerSeed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // 모닥불 도트 불꽃이 타오르는 미세 스케일/밝기 애니메이션
            if (flameRenderer != null)
            {
                float pulse = 1.0f + Mathf.Sin((Time.time + _flickerSeed) * 8f) * 0.06f;
                flameRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        public void OnInteractStart(GameObject interactor)
        {
            Debug.Log("[모닥불 캠프] 모닥불 앞에서 휴식 및 야생 요리를 준비합니다...");
        }

        public void OnInteractProgress(float normalizedProgress)
        {
        }

        public void OnInteractComplete(GameObject interactor)
        {
            // 1. 모닥불 요리 레시피가 등록되어 있고 재료가 있다면 요리 제작 실행
            if (defaultCampfireMealRecipe != null && InventoryAndCraftingManager.Instance != null)
            {
                InventoryAndCraftingManager.Instance.TryExecuteRecipe(defaultCampfireMealRecipe);
            }

            // 2. 캠프 휴식 및 수렵 버프(갈무리 행운 +15%, 공격력 +10%, 기후 저항) 부여
            if (WorldEnvironmentManager.Instance != null)
            {
                WorldEnvironmentManager.Instance.ApplyCampfireMealBuff(
                    mealBuffDuration,
                    grantCarveLuck: true,
                    grantAttackBoost: true,
                    grantClimateResist: true);
            }

            Debug.Log("🔥 [모닥불 캠프 완료] 모닥불 요리를 섭취하여 5분간 [시체 갈무리 행운 +15% & 공격력 +10%] 버프를 획득했습니다!");
        }

        public void OnInteractCancel()
        {
            Debug.Log("[모닥불 캠프] 캠프 준비를 중단했습니다.");
        }
    }
}
