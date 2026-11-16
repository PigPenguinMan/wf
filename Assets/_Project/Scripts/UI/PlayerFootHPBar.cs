using UnityEngine;
using UnityEngine.UI;
using WildFrontier.Player;

namespace WildFrontier.UI
{
    /// <summary>
    /// [가디언 테일즈 스타일 플레이어 발밑 초록색 HP 바 + 레벨 뱃지]
    /// RigVisualRoot의 좌우 반전에 영향받지 않도록 Player_Hunter 루트에 부착됩니다.
    /// </summary>
    public class PlayerFootHPBar : MonoBehaviour
    {
        [SerializeField] private PlayerCombat targetPlayer;
        [SerializeField] private float footOffsetY = -0.50f;

        private Image _hpFillImage;
        private Text _levelText;

        public void Configure(PlayerCombat player)
        {
            targetPlayer = player;
        }

        private void Start()
        {
            if (targetPlayer == null)
                targetPlayer = GetComponentInParent<PlayerCombat>();

            BuildWorldBar();
        }

        private void BuildWorldBar()
        {
            GameObject canvasGo = new GameObject("PlayerFootHPCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, footOffsetY, 0f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 32;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 30f);
            rt.localScale = Vector3.one * 0.010f;

            // 외곽 어두운 테두리 배경
            GameObject bgGo = new GameObject("PlayerHP_Bg");
            bgGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchoredPosition = new Vector2(10f, 0f);
            bgRt.sizeDelta = new Vector2(78f, 9f);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.12f, 0.14f, 0.90f);

            // 초록색 HP 채움 바
            GameObject fillGo = new GameObject("PlayerHP_Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(1f, 0f);
            fillRt.sizeDelta = new Vector2(76f, -2f);
            _hpFillImage = fillGo.AddComponent<Image>();
            _hpFillImage.color = new Color(0.28f, 0.92f, 0.36f, 1f);

            // 좌측 Lv.1 뱃지
            GameObject lvlGo = new GameObject("LevelBadge");
            lvlGo.transform.SetParent(canvasGo.transform, false);
            RectTransform lvlRt = lvlGo.AddComponent<RectTransform>();
            lvlRt.anchoredPosition = new Vector2(-43f, 0f);
            lvlRt.sizeDelta = new Vector2(36f, 18f);
            _levelText = lvlGo.AddComponent<Text>();
            _levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _levelText.fontSize = 11;
            _levelText.fontStyle = FontStyle.Bold;
            _levelText.alignment = TextAnchor.MiddleRight;
            _levelText.color = Color.white;
            _levelText.text = "Lv.1";
            Outline outline = lvlGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void LateUpdate()
        {
            if (targetPlayer == null || _hpFillImage == null) return;

            float ratio = targetPlayer.MaxHp > 0
                ? Mathf.Clamp01((float)targetPlayer.CurrentHp / targetPlayer.MaxHp)
                : 0f;
            _hpFillImage.rectTransform.sizeDelta = new Vector2(76f * ratio, -2f);
        }
    }
}
