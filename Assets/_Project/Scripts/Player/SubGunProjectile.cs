using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Monster;
using WildFrontier.UI;

namespace WildFrontier.Player
{
    /// <summary>
    /// 와일드 프론티어 공통 보조 무기 '견제 사격(Sub-Gun)' 투사체 컴포넌트.
    /// 고속 비행하며 몬스터와 충돌 시 피해·원거리 어그로 유인과 함께 장착된 룬 상태이상을 부여합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class SubGunProjectile : MonoBehaviour
    {
        private static Sprite _cachedBulletSprite;

        [SerializeField] private float speed = 11f;
        [SerializeField] private float maxLifetime = 1.2f;

        private Vector2 _direction = Vector2.right;
        private int _damage = 20;
        private ElementType _element = ElementType.Physical;
        private PlayerCombat _ownerCombat;
        private float _lifeTimer;
        private bool _hasHit;

        public void Initialize(Vector2 direction, int damage, ElementType element, PlayerCombat owner)
        {
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            _damage = Mathf.Max(1, damage);
            _element = element;
            _ownerCombat = owner;
            _lifeTimer = maxLifetime;
            _hasHit = false;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            SetupVisualAndPhysics();
        }

        private void SetupVisualAndPhysics()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = gameObject.AddComponent<SpriteRenderer>();

            sr.sprite = GetOrCreateBulletSprite();
            sr.sortingOrder = 14;
            sr.color = GetElementTint(_element);
        }

        private void Update()
        {
            if (_hasHit) return;

            transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit) return;

            MonsterBase monster = other.GetComponent<MonsterBase>();
            if (monster == null || monster.IsDead) return;

            _hasHit = true;

            Transform ownerTr = _ownerCombat != null ? _ownerCombat.transform : null;
            monster.TakeDamage(_damage, _element, false, ownerTr);

            if (_ownerCombat != null)
            {
                _ownerCombat.ApplyEquippedRunesToTarget(monster, _damage);
            }

            FloatingPopupManager.SpawnPopup(
                monster.transform.position + Vector3.up * 0.95f,
                "🔫사격",
                new Color(1.0f, 0.85f, 0.35f),
                18,
                0.8f);

            Destroy(gameObject);
        }

        private static Color GetElementTint(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return new Color(1.0f, 0.55f, 0.25f, 1f);
                case ElementType.Lightning: return new Color(0.55f, 0.90f, 1.0f, 1f);
                case ElementType.Ice: return new Color(0.65f, 0.95f, 1.0f, 1f);
                default: return new Color(1.0f, 0.92f, 0.45f, 1f);
            }
        }

        private static Sprite GetOrCreateBulletSprite()
        {
            if (_cachedBulletSprite != null)
                return _cachedBulletSprite;

            const int w = 12;
            const int h = 8;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color32(55, 28, 16, 255);
            Color body = new Color32(255, 210, 65, 255);
            Color core = new Color32(255, 252, 215, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool insideOuter = x >= 1 && x <= w - 2 && y >= 1 && y <= h - 2;
                    bool insideBody = x >= 2 && x <= w - 3 && y >= 2 && y <= h - 3;
                    bool insideCore = x >= 4 && x <= w - 4 && y >= 3 && y <= h - 4;

                    if (insideCore) tex.SetPixel(x, y, core);
                    else if (insideBody) tex.SetPixel(x, y, body);
                    else if (insideOuter) tex.SetPixel(x, y, outline);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _cachedBulletSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
            return _cachedBulletSprite;
        }
    }
}
