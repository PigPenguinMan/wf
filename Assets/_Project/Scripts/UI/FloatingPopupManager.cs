using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WildFrontier.Data;
using WildFrontier.Systems;

namespace WildFrontier.UI
{
    /// <summary>
    /// 사냥 데미지, 룬 발동, 시체 갈무리 전리품 획득, 재료 압축/제작 성공 시
    /// 캐릭터와 몬스터 머리 위에 픽셀 감성의 팝업 텍스트를 띄워주는 매니저입니다.
    /// </summary>
    public class FloatingPopupManager : MonoBehaviour
    {
        public static FloatingPopupManager Instance { get; private set; }

        private class ActivePopup
        {
            public GameObject rootGo;
            public Text textComp;
            public Vector3 worldPos;
            public float elapsed;
            public float duration;
            public Color baseColor;
        }

        private Canvas _worldCanvas;
        private Font _defaultFont;
        private Transform _playerTransform;
        private readonly List<ActivePopup> _popups = new List<ActivePopup>();
        private int _recentPopupStagger;
        private float _lastPopupTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateWorldPopupCanvas();
        }

        private void Start()
        {
            var player = FindFirstObjectByType<Player.PlayerCombat>();
            if (player != null)
                _playerTransform = player.transform;

            if (InventoryAndCraftingManager.Instance != null)
            {
                InventoryAndCraftingManager.Instance.OnItemAdded += HandleItemAdded;
                InventoryAndCraftingManager.Instance.OnRecipeUnlocked += HandleRecipeUnlocked;
                InventoryAndCraftingManager.Instance.OnCraftCompleted += HandleCraftCompleted;
            }
        }

        private void OnDestroy()
        {
            if (InventoryAndCraftingManager.Instance != null)
            {
                InventoryAndCraftingManager.Instance.OnItemAdded -= HandleItemAdded;
                InventoryAndCraftingManager.Instance.OnRecipeUnlocked -= HandleRecipeUnlocked;
                InventoryAndCraftingManager.Instance.OnCraftCompleted -= HandleCraftCompleted;
            }
        }

        private void CreateWorldPopupCanvas()
        {
            GameObject canvasGo = new GameObject("WorldPopupCanvas");
            canvasGo.transform.SetParent(transform, false);
            _worldCanvas = canvasGo.AddComponent<Canvas>();
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            _worldCanvas.sortingOrder = 50;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.localScale = Vector3.one * 0.015f;
        }

        private void HandleItemAdded(ItemDataSO item, int amount)
        {
            if (item == null) return;
            Vector3 pos = _playerTransform != null ? _playerTransform.position : Vector3.zero;

            // 동시에 여러 갈무리 재료가 나올 때 겹치지 않게 높이 오프셋 부여
            if (Time.time - _lastPopupTime < 0.25f)
                _recentPopupStagger++;
            else
                _recentPopupStagger = 0;

            _lastPopupTime = Time.time;

            Color col = new Color(0.45f, 1.0f, 0.55f);
            if (item.grade == Core.ItemGrade.Rare) col = new Color(0.35f, 0.85f, 1.0f);
            else if (item.grade >= Core.ItemGrade.Epic) col = new Color(1.0f, 0.80f, 0.25f);

            SpawnPopup(pos + new Vector3(0f, 0.7f + _recentPopupStagger * 0.38f, 0f),
                $"+ {item.itemName} x{amount}", col, 22, 1.6f);
        }

        private void HandleRecipeUnlocked(CraftingRecipeSO recipe)
        {
            if (recipe == null) return;
            Vector3 pos = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            SpawnPopup(pos + new Vector3(0f, 1.3f, 0f),
                $"📜 [제작법 습득] {recipe.recipeName}!", new Color(1.0f, 0.90f, 0.30f), 24, 2.2f);
        }

        private void HandleCraftCompleted(ItemDataSO resultItem, int count, bool isCrit)
        {
            if (resultItem == null) return;
            Vector3 pos = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            string prefix = isCrit ? "★대성공 제작★ " : "🔨 제작 완료: ";
            SpawnPopup(pos + new Vector3(0f, 1.2f, 0f),
                $"{prefix}{resultItem.itemName} x{count}", new Color(1.0f, 0.75f, 0.25f), 25, 2.0f);
        }

        public static void SpawnPopup(Vector3 worldPos, string message, Color color, int fontSize = 20, float duration = 1.1f)
        {
            if (Instance == null) return;
            Instance.InternalSpawn(worldPos, message, color, fontSize, duration);
        }

        private void InternalSpawn(Vector3 worldPos, string message, Color color, int fontSize, float duration)
        {
            if (_worldCanvas == null) return;

            GameObject go = new GameObject("PopupText");
            go.transform.SetParent(_worldCanvas.transform, false);
            go.transform.position = worldPos;

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450f, 60f);

            Text txt = go.AddComponent<Text>();
            txt.font = _defaultFont;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.color = color;
            txt.text = message;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            _popups.Add(new ActivePopup
            {
                rootGo = go,
                textComp = txt,
                worldPos = worldPos,
                elapsed = 0f,
                duration = duration,
                baseColor = color
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                p.elapsed += dt;
                if (p.elapsed >= p.duration || p.rootGo == null)
                {
                    if (p.rootGo != null) Destroy(p.rootGo);
                    _popups.RemoveAt(i);
                    continue;
                }

                float t = p.elapsed / p.duration;
                p.rootGo.transform.position = p.worldPos + new Vector3(0f, t * 0.65f, 0f);

                if (t > 0.6f)
                {
                    float alpha = Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
                    p.textComp.color = new Color(p.baseColor.r, p.baseColor.g, p.baseColor.b, alpha);
                }
            }
        }
    }
}
