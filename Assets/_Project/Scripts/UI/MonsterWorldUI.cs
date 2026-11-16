using UnityEngine;
using UnityEngine.UI;
using WildFrontier.Monster;

namespace WildFrontier.UI
{
    /// <summary>
    /// 몬스터 머리 위에 이름표(레벨+이름), 실시간 HP 바,
    /// 그리고 사망 시 '🔪 갈무리 가능 (Hold)' 안내 표시를 렌더링하는 월드 스페이스 UI 컴포넌트입니다.
    /// </summary>
    public class MonsterWorldUI : MonoBehaviour
    {
        [SerializeField] private MonsterBase targetMonster;
        [SerializeField] private float heightOffset = -0.48f;

        private Canvas _canvas;
        private Text _nameText;
        private Image _hpFillImage;
        private Image _hpBgImage;
        private bool _wasDead;

        public void InitializeWorldUI(MonsterBase monster, float customYOffset)
        {
            targetMonster = monster;
            heightOffset = customYOffset;
        }

        private void Start()
        {
            if (targetMonster == null)
                targetMonster = GetComponent<MonsterBase>();

            BuildOverheadCanvas();
            RefreshUI();
        }

        private void BuildOverheadCanvas()
        {
            float yOffset = heightOffset;
            if (targetMonster != null && targetMonster.Data != null && targetMonster.Data.size == Core.MonsterSize.Boss)
                yOffset = -0.72f;

            GameObject canvasGo = new GameObject("OverheadUI");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, yOffset, 0f);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 30;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 40f);
            rt.localScale = Vector3.one * 0.010f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. 몬스터 이름표
            GameObject nameGo = new GameObject("NameLabel");
            nameGo.transform.SetParent(canvasGo.transform, false);
            RectTransform nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchoredPosition = new Vector2(0f, -15f);
            nameRt.sizeDelta = new Vector2(220f, 22f);

            _nameText = nameGo.AddComponent<Text>();
            _nameText.font = font;
            _nameText.fontSize = 18;
            _nameText.fontStyle = FontStyle.Bold;
            _nameText.alignment = TextAnchor.MiddleCenter;
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _nameText.color = Color.white;

            Outline outline = nameGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // 2. 체력바 배경
            GameObject bgGo = new GameObject("HPBar_Bg");
            bgGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchoredPosition = new Vector2(0f, -2f);
            bgRt.sizeDelta = new Vector2(96f, 9f);
            _hpBgImage = bgGo.AddComponent<Image>();
            _hpBgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

            // 3. 체력바 Fill
            GameObject fillGo = new GameObject("HPBar_Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(1f, 0f);
            fillRt.sizeDelta = new Vector2(94f, -2f);
            _hpFillImage = fillGo.AddComponent<Image>();
            _hpFillImage.color = new Color(0.92f, 0.25f, 0.25f, 1f);
        }

        private void LateUpdate()
        {
            if (targetMonster == null || targetMonster.Data == null) return;
            RefreshUI();
        }

        private void RefreshUI()
        {
            var data = targetMonster.Data;

            if (!targetMonster.IsDead)
            {
                if (_nameText != null)
                {
                    string prefix = data.size == Core.MonsterSize.Boss ? "👑 " : (data.level >= 8 ? "★ " : "");
                    _nameText.text = $"{prefix}Lv.{data.level} {data.monsterName}";
                    _nameText.color = data.size == Core.MonsterSize.Boss
                        ? new Color(1.0f, 0.65f, 0.2f)
                        : (data.level >= 8 ? new Color(1.0f, 0.9f, 0.3f) : Color.white);
                }

                if (_hpFillImage != null)
                {
                    float ratio = Mathf.Clamp01((float)targetMonster.CurrentHp / Mathf.Max(1, data.maxHp));
                    _hpFillImage.rectTransform.sizeDelta = new Vector2(94f * ratio, -2f);
                }
            }
            else if (!_wasDead)
            {
                _wasDead = true;
                if (_nameText != null)
                {
                    _nameText.text = $"🔪 [{data.monsterName} 시체] 갈무리 가능";
                    _nameText.color = new Color(1.0f, 0.92f, 0.35f);
                }
                if (_hpBgImage != null)
                {
                    _hpBgImage.gameObject.SetActive(false);
                }
            }
        }
    }
}
