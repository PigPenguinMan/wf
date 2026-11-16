using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Player
{
    /// <summary>
    /// [가디언 테일즈 스타일 멀티 파츠 퍼펫 리그]
    /// 발(좌/우 교차), 그림자, 갑옷 몸통, 얼굴 표정(기본/눈깜빡임/공격/피격), 클래스 투구/후드,
    /// 보조손/방패, 그리고 회전 피벗 무기를 절차적 스프링/바운스 애니메이션으로 구동한다.
    /// </summary>
    public class GuardianTalesCharacterRig : MonoBehaviour
    {
        [Header("파츠 계층 참조")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer footShadowRenderer;
        [SerializeField] private Transform leftFootTransform;
        [SerializeField] private Transform rightFootTransform;
        [SerializeField] private Transform bodyTransform;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform headTransform;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private SpriteRenderer headgearRenderer;
        [SerializeField] private Transform offHandTransform;
        [SerializeField] private SpriteRenderer offHandRenderer;
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private SpriteRenderer weaponRenderer;

        [Header("표정 / 클래스 / 이펙트 스프라이트")]
        [SerializeField] private Sprite headNormalSprite;
        [SerializeField] private Sprite headBlinkSprite;
        [SerializeField] private Sprite headAttackSprite;
        [SerializeField] private Sprite headHurtSprite;

        [SerializeField] private Sprite warriorHelmSprite;
        [SerializeField] private Sprite scouterHoodSprite;
        [SerializeField] private Sprite guardianKnightHelmSprite;

        [SerializeField] private Sprite warriorWeaponSprite;
        [SerializeField] private Sprite scouterWeaponSprite;
        [SerializeField] private Sprite guardianWeaponSprite;

        [SerializeField] private Sprite handSprite;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite dustPuffSprite;

        private Vector3 _baseLeftFootPos = new Vector3(-0.11f, -0.34f, 0f);
        private Vector3 _baseRightFootPos = new Vector3(0.11f, -0.34f, 0f);
        private Vector3 _baseBodyPos = new Vector3(0f, -0.13f, 0f);
        private Vector3 _baseHeadPos = new Vector3(0f, 0.21f, 0f);
        private Vector3 _baseOffHandPos = new Vector3(-0.23f, -0.14f, 0f);
        private Vector3 _baseWeaponPos = new Vector3(0.22f, -0.15f, 0f);
        private Vector3 _baseShadowScale = Vector3.one;

        private float _walkCycle;
        private float _idleCycle;
        private float _dustTimer;
        private float _blinkCooldown = 2.5f;
        private float _blinkDurationTimer;
        private float _attackExprTimer;
        private float _hurtTimer;

        private float _attackMotionTimer;
        private float _attackMotionDuration = 0.24f;
        private int _currentComboStep;
        private bool _isSpinAttack;
        private int _facingSign = 1;
        public int FacingSign => _facingSign;

        public void ConfigureRig(
            Transform root,
            SpriteRenderer shadowSr,
            Transform leftFoot,
            Transform rightFoot,
            Transform body,
            SpriteRenderer bodySr,
            Transform head,
            SpriteRenderer headSr,
            SpriteRenderer headgearSr,
            Transform offHand,
            SpriteRenderer offHandSr,
            Transform weaponPivotTf,
            SpriteRenderer weaponSr,
            Sprite normalFace,
            Sprite blinkFace,
            Sprite attackFace,
            Sprite hurtFace,
            Sprite warriorHelm,
            Sprite scouterHood,
            Sprite guardianHelm,
            Sprite warriorWeapon,
            Sprite scouterWeapon,
            Sprite guardianWeapon,
            Sprite handSpr,
            Sprite shieldSpr,
            Sprite dustSpr)
        {
            visualRoot = root;
            footShadowRenderer = shadowSr;
            leftFootTransform = leftFoot;
            rightFootTransform = rightFoot;
            bodyTransform = body;
            bodyRenderer = bodySr;
            headTransform = head;
            headRenderer = headSr;
            headgearRenderer = headgearSr;
            offHandTransform = offHand;
            offHandRenderer = offHandSr;
            weaponPivot = weaponPivotTf;
            weaponRenderer = weaponSr;

            headNormalSprite = normalFace;
            headBlinkSprite = blinkFace;
            headAttackSprite = attackFace;
            headHurtSprite = hurtFace;

            warriorHelmSprite = warriorHelm;
            scouterHoodSprite = scouterHood;
            guardianKnightHelmSprite = guardianHelm;

            warriorWeaponSprite = warriorWeapon;
            scouterWeaponSprite = scouterWeapon;
            guardianWeaponSprite = guardianWeapon;

            handSprite = handSpr;
            shieldSprite = shieldSpr;
            dustPuffSprite = dustSpr;

            CacheBaseTransforms();
            ApplyClassVisuals(CharacterClass.Warrior);
        }

        private void Awake()
        {
            CacheBaseTransforms();
        }

        private void CacheBaseTransforms()
        {
            if (leftFootTransform != null) _baseLeftFootPos = leftFootTransform.localPosition;
            if (rightFootTransform != null) _baseRightFootPos = rightFootTransform.localPosition;
            if (bodyTransform != null) _baseBodyPos = bodyTransform.localPosition;
            if (headTransform != null) _baseHeadPos = headTransform.localPosition;
            if (offHandTransform != null) _baseOffHandPos = offHandTransform.localPosition;
            if (weaponPivot != null) _baseWeaponPos = weaponPivot.localPosition;
            if (footShadowRenderer != null) _baseShadowScale = footShadowRenderer.transform.localScale;
        }

        public void ApplyClassVisuals(CharacterClass cls)
        {
            if (headgearRenderer != null)
            {
                Sprite helm = warriorHelmSprite;
                if (cls == CharacterClass.Scouter) helm = scouterHoodSprite;
                else if (cls == CharacterClass.Guardian) helm = guardianKnightHelmSprite;

                headgearRenderer.sprite = helm;
                headgearRenderer.enabled = helm != null;
            }

            if (weaponRenderer != null)
            {
                Sprite wpn = warriorWeaponSprite;
                if (cls == CharacterClass.Scouter) wpn = scouterWeaponSprite;
                else if (cls == CharacterClass.Guardian) wpn = guardianWeaponSprite;

                weaponRenderer.sprite = wpn;
            }

            if (offHandRenderer != null)
            {
                bool useShield = (cls == CharacterClass.Guardian || cls == CharacterClass.Warrior) && shieldSprite != null;
                offHandRenderer.sprite = useShield ? shieldSprite : handSprite;
                if (offHandTransform != null)
                {
                    offHandTransform.localScale = useShield ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);
                }
            }

            if (bodyRenderer != null)
            {
                if (cls == CharacterClass.Scouter) bodyRenderer.color = new Color(0.84f, 0.98f, 0.86f, 1f);
                else if (cls == CharacterClass.Guardian) bodyRenderer.color = new Color(0.92f, 0.96f, 1f, 1f);
                else bodyRenderer.color = Color.white;
            }
        }

        public void PlayAttackComboMotion(int comboStep, bool isFullCharge)
        {
            _currentComboStep = Mathf.Clamp(comboStep, 0, 2);
            _isSpinAttack = isFullCharge || _currentComboStep == 2;
            _attackMotionDuration = _isSpinAttack ? 0.30f : 0.21f;
            _attackMotionTimer = _attackMotionDuration;
            _attackExprTimer = 0.34f;
        }

        public void PlayHurtReaction()
        {
            _hurtTimer = 0.36f;
        }

        public void UpdateLocomotion(Vector2 moveVel, bool isDodging, bool isCharging, bool isCarving, float chargeProgress)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (Mathf.Abs(moveVel.x) > 0.08f)
            {
                _facingSign = moveVel.x >= 0f ? 1 : -1;
                if (visualRoot != null)
                {
                    Vector3 s = visualRoot.localScale;
                    s.x = Mathf.Abs(s.x) * _facingSign;
                    visualRoot.localScale = s;
                }
            }

            float speed = moveVel.magnitude;
            bool isMoving = speed > 0.12f;
            float bounceOffset = 0f;
            float leanAngle = 0f;

            if (isDodging)
            {
                leanAngle = -18f;
                bounceOffset = 0.06f;
                if (bodyTransform != null)
                    bodyTransform.localScale = new Vector3(1.12f, 0.90f, 1f);

                _dustTimer -= dt;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = 0.055f;
                    SpawnDustPuff(0.95f);
                }
            }
            else if (isMoving)
            {
                _walkCycle += dt * 15f;
                float sin = Mathf.Sin(_walkCycle);
                float cos = Mathf.Cos(_walkCycle);
                bounceOffset = Mathf.Abs(sin) * 0.085f;
                leanAngle = -7f + sin * 4f;

                if (leftFootTransform != null)
                    leftFootTransform.localPosition = _baseLeftFootPos + new Vector3(sin * 0.14f, Mathf.Max(0f, cos) * 0.10f, 0f);
                if (rightFootTransform != null)
                    rightFootTransform.localPosition = _baseRightFootPos + new Vector3(-sin * 0.14f, Mathf.Max(0f, -cos) * 0.10f, 0f);
                if (bodyTransform != null)
                    bodyTransform.localScale = new Vector3(1f + Mathf.Abs(cos) * 0.04f, 1f - Mathf.Abs(cos) * 0.04f, 1f);

                _dustTimer -= dt;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = 0.13f;
                    SpawnDustPuff(0.75f);
                }
            }
            else
            {
                _idleCycle += dt * 3.4f;
                float breathe = Mathf.Sin(_idleCycle);
                bounceOffset = breathe * 0.02f;

                if (leftFootTransform != null)
                    leftFootTransform.localPosition = Vector3.Lerp(leftFootTransform.localPosition, _baseLeftFootPos, dt * 14f);
                if (rightFootTransform != null)
                    rightFootTransform.localPosition = Vector3.Lerp(rightFootTransform.localPosition, _baseRightFootPos, dt * 14f);
                if (bodyTransform != null)
                    bodyTransform.localScale = Vector3.Lerp(bodyTransform.localScale, Vector3.one, dt * 12f);
            }

            if (isCharging)
            {
                float shake = Mathf.Sin(Time.time * 48f) * (0.015f + 0.025f * chargeProgress);
                bounceOffset -= 0.045f;
                if (bodyTransform != null)
                {
                    bodyTransform.localPosition = _baseBodyPos + new Vector3(shake, bounceOffset, 0f);
                    bodyTransform.localScale = new Vector3(1.07f, 0.92f, 1f);
                }
            }
            else if (bodyTransform != null)
            {
                bodyTransform.localPosition = _baseBodyPos + new Vector3(0f, bounceOffset, 0f);
                bodyTransform.localRotation = Quaternion.Euler(0f, 0f, leanAngle);
            }

            if (headTransform != null)
            {
                float headNod = isMoving ? Mathf.Sin(_walkCycle) * 4.5f : Mathf.Sin(_idleCycle) * 1.8f;
                headTransform.localPosition = _baseHeadPos + new Vector3(0f, bounceOffset * 1.15f, 0f);
                headTransform.localRotation = Quaternion.Euler(0f, 0f, headNod);
            }

            if (offHandTransform != null)
            {
                float armSwing = isMoving ? -Mathf.Sin(_walkCycle) * 0.055f : 0f;
                offHandTransform.localPosition = _baseOffHandPos + new Vector3(armSwing, bounceOffset * 0.9f, 0f);
            }

            if (footShadowRenderer != null)
            {
                float shrink = Mathf.Clamp(1f - bounceOffset * 1.5f, 0.72f, 1.05f);
                footShadowRenderer.transform.localScale = _baseShadowScale * shrink;
            }

            UpdateWeaponMotion(dt, bounceOffset, isCharging, isCarving, chargeProgress, isMoving);
            UpdateFaceAndFlash(dt, isCharging);
        }

        private void UpdateWeaponMotion(float dt, float bounceOffset, bool isCharging, bool isCarving, float chargeProgress, bool isMoving)
        {
            if (weaponPivot == null) return;

            weaponPivot.localPosition = _baseWeaponPos + new Vector3(0f, bounceOffset * 0.95f, 0f);

            if (_attackMotionTimer > 0f)
            {
                _attackMotionTimer -= dt;
                float t = 1f - Mathf.Clamp01(_attackMotionTimer / Mathf.Max(0.01f, _attackMotionDuration));
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                float angle;
                if (_isSpinAttack)
                {
                    angle = Mathf.Lerp(40f, -340f, eased);
                    weaponPivot.localScale = Vector3.one * (1.22f - 0.14f * t);
                }
                else if (_currentComboStep == 1)
                {
                    // 2타: 아래(-95도) -> 위(+80도) 올려베기
                    angle = Mathf.Lerp(-95f, 80f, eased);
                    weaponPivot.localScale = Vector3.one * 1.10f;
                }
                else
                {
                    // 1타: 위(+85도) -> 아래(-95도) 내려찍기
                    angle = Mathf.Lerp(85f, -95f, eased);
                    weaponPivot.localScale = Vector3.one * 1.10f;
                }

                weaponPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else if (isCharging)
            {
                weaponPivot.localScale = Vector3.one * (1f + chargeProgress * 0.18f);
                float windup = 58f + chargeProgress * 38f + Mathf.Sin(Time.time * 42f) * 3f;
                weaponPivot.localRotation = Quaternion.Euler(0f, 0f, windup);
            }
            else if (isCarving)
            {
                weaponPivot.localScale = Vector3.one;
                float chop = -45f + Mathf.Sin(Time.time * 18f) * 35f;
                weaponPivot.localRotation = Quaternion.Euler(0f, 0f, chop);
            }
            else
            {
                weaponPivot.localScale = Vector3.Lerp(weaponPivot.localScale, Vector3.one, dt * 14f);
                float idleWpn = isMoving ? (-32f + Mathf.Sin(_walkCycle) * 14f) : (-38f + Mathf.Sin(_idleCycle) * 4f);
                Quaternion targetRot = Quaternion.Euler(0f, 0f, idleWpn);
                weaponPivot.localRotation = Quaternion.Lerp(weaponPivot.localRotation, targetRot, dt * 14f);
            }
        }

        private void UpdateFaceAndFlash(float dt, bool isCharging)
        {
            if (_hurtTimer > 0f)
                _hurtTimer -= dt;

            if (_attackExprTimer > 0f)
                _attackExprTimer -= dt;

            if (_blinkDurationTimer > 0f)
            {
                _blinkDurationTimer -= dt;
            }
            else
            {
                _blinkCooldown -= dt;
                if (_blinkCooldown <= 0f)
                {
                    _blinkDurationTimer = 0.11f;
                    _blinkCooldown = Random.Range(2.4f, 4.2f);
                }
            }

            if (headRenderer != null)
            {
                Sprite targetFace = headNormalSprite;
                if (_hurtTimer > 0f && headHurtSprite != null)
                    targetFace = headHurtSprite;
                else if ((_attackExprTimer > 0f || isCharging) && headAttackSprite != null)
                    targetFace = headAttackSprite;
                else if (_blinkDurationTimer > 0f && headBlinkSprite != null)
                    targetFace = headBlinkSprite;

                if (targetFace != null && headRenderer.sprite != targetFace)
                    headRenderer.sprite = targetFace;
            }

            // 피격 시 선명한 붉은-흰 깜빡임 피드백
            if (_hurtTimer > 0f)
            {
                bool whitePhase = Mathf.FloorToInt(_hurtTimer * 24f) % 2 == 0;
                Color flash = whitePhase ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
                if (headRenderer != null) headRenderer.color = flash;
                if (headgearRenderer != null) headgearRenderer.color = flash;
            }
            else
            {
                if (headRenderer != null) headRenderer.color = Color.white;
                if (headgearRenderer != null) headgearRenderer.color = Color.white;
            }
        }

        private void SpawnDustPuff(float scaleMul)
        {
            if (dustPuffSprite == null) return;

            GameObject go = new GameObject("GT_DustPuff");
            go.transform.position = transform.position + new Vector3(-_facingSign * 0.20f, -0.34f, 0f);
            go.transform.localScale = Vector3.one * (0.55f * scaleMul);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dustPuffSprite;
            sr.sortingOrder = 6;

            DustPuffMotion puff = go.AddComponent<DustPuffMotion>();
            puff.Init(new Vector3(-_facingSign * 0.45f, 0.35f, 0f), 0.30f);
        }

        private sealed class DustPuffMotion : MonoBehaviour
        {
            private Vector3 _vel;
            private float _life;
            private float _maxLife;
            private SpriteRenderer _sr;

            public void Init(Vector3 velocity, float duration)
            {
                _vel = velocity;
                _life = duration;
                _maxLife = duration;
                _sr = GetComponent<SpriteRenderer>();
            }

            private void Update()
            {
                float dt = Time.deltaTime;
                _life -= dt;
                if (_life <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }

                transform.position += _vel * dt;
                transform.localScale += Vector3.one * (dt * 1.4f);

                if (_sr != null)
                {
                    Color c = _sr.color;
                    c.a = Mathf.Clamp01(_life / _maxLife) * 0.85f;
                    _sr.color = c;
                }
            }
        }
    }
}
