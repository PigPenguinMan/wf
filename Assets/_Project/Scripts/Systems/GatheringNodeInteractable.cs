using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;

namespace WildFrontier.Systems
{
    /// <summary>
    /// 필드의 약초, 형광 포자 버섯, 광맥 등 자연 채집물 상호작용 컴포넌트.
    /// 밤(Night)이나 우천(Rain) 시 채집 수량이 증가하는 환경 상호작용을 지원합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GatheringNodeInteractable : MonoBehaviour, IHoldInteractable
    {
        [Header("채집 노드 설정")]
        [SerializeField] private string nodeName = "야생 형광 버섯 군락";
        [SerializeField] private ItemDataSO gatheredItem;
        [SerializeField] private int minAmount = 1;
        [SerializeField] private int maxAmount = 3;
        [SerializeField] private float gatherHoldTime = 1.0f;
        [SerializeField] private float respawnSeconds = 25f;
        [Tooltip("체크 시 밤(Night)이나 비(Rain) 시간대에 채집 수량이 +1 추가됩니다.")]
        [SerializeField] private bool bonusInNightOrRain = true;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool _isDepleted;
        private float _respawnTimer;

        public ActionContextType ContextType => ActionContextType.Gather;
        public bool CanInteract => !_isDepleted && gatheredItem != null;
        public float RequiredHoldDuration => gatherHoldTime;
        public Vector3 Position => transform.position;

        public void ConfigureNode(string customName, ItemDataSO item, SpriteRenderer sr)
        {
            nodeName = customName;
            gatheredItem = item;
            spriteRenderer = sr;
        }

        private void Update()
        {
            if (!_isDepleted) return;

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                _isDepleted = false;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }
        }

        public void OnInteractStart(GameObject interactor)
        {
            Debug.Log($"[채집 시작] '{nodeName}' 채집 중...");
        }

        public void OnInteractProgress(float normalizedProgress)
        {
            if (spriteRenderer != null)
            {
                float shake = Mathf.Sin(normalizedProgress * 35f) * 0.025f;
                spriteRenderer.transform.localPosition = new Vector3(shake, 0f, 0f);
            }
        }

        public void OnInteractComplete(GameObject interactor)
        {
            if (_isDepleted) return;
            _isDepleted = true;
            _respawnTimer = respawnSeconds;

            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localPosition = Vector3.zero;
                spriteRenderer.enabled = false;
            }

            int amount = Random.Range(minAmount, maxAmount + 1);
            if (bonusInNightOrRain && WorldEnvironmentManager.Instance != null)
            {
                var env = WorldEnvironmentManager.Instance;
                if (env.CurrentTimeOfDay == TimeOfDay.Night || env.CurrentWeather == WeatherType.Rain)
                {
                    amount += 1;
                    Debug.Log("[환경 채집 보너스!] 밤/우천 생태계 영향으로 채집물을 추가 획득했습니다.");
                }
            }

            if (InventoryAndCraftingManager.Instance != null && gatheredItem != null)
            {
                InventoryAndCraftingManager.Instance.AddItem(gatheredItem, amount);
            }
        }

        public void OnInteractCancel()
        {
            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = Vector3.zero;
        }
    }
}
