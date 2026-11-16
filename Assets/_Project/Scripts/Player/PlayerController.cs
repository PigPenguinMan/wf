using UnityEngine;

namespace WildFrontier.Player
{
    /// <summary>
    /// 32x32 픽셀아트 환경에 맞춘 탑다운 8방향 이동 및 회피/대시 컨트롤러.
    /// 모바일 가상 조이스틱 입력과 키보드(WASD) 테스트 입력을 모두 지원합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동 및 회피 설정")]
        [SerializeField] private float moveSpeed = 3.8f;
        [SerializeField] private float dodgeSpeed = 8.0f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.8f;
        [SerializeField] private SpriteRenderer characterSpriteRenderer;
        [SerializeField] private GuardianTalesCharacterRig characterRig;

        private Rigidbody2D _rb;
        private PlayerCombat _cachedCombat;
        private Vector2 _moveInput;
        private Vector2 _lastFacingDir = Vector2.down;
        private Vector2 _virtualJoystickInput;

        private bool _isDodging;
        private float _dodgeTimer;
        private float _dodgeCooldownTimer;

        private bool _isAttackLunging;
        private float _attackLungeTimer;
        private float _attackLungeSpeed;
        private Vector2 _attackLungeDir;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        public void ConfigureRenderer(SpriteRenderer sr)
        {
            characterSpriteRenderer = sr;
        }

        public void ConfigureRig(GuardianTalesCharacterRig rig)
        {
            characterRig = rig;
        }

        public void TriggerAttackLunge(Vector2 dir, float speed = 4.6f, float duration = 0.11f)
        {
            if (_isDodging) return;
            Vector2 normalized = dir.sqrMagnitude > 0.001f ? dir.normalized : _lastFacingDir;
            if (normalized.sqrMagnitude < 0.001f) normalized = Vector2.right;
            _attackLungeDir = normalized;
            _lastFacingDir = normalized;
            _attackLungeSpeed = speed;
            _attackLungeTimer = duration;
            _isAttackLunging = true;
        }

        private void Update()
        {
            if (_dodgeCooldownTimer > 0f)
                _dodgeCooldownTimer -= Time.deltaTime;

            if (_isAttackLunging)
            {
                _attackLungeTimer -= Time.deltaTime;
                if (_attackLungeTimer <= 0f)
                    _isAttackLunging = false;
            }

            if (_isDodging)
            {
                _dodgeTimer -= Time.deltaTime;
                if (_dodgeTimer <= 0f)
                    _isDodging = false;
                UpdateCharacterVisualAnimation();
                return;
            }

            // 에디터 테스트 편의: Left Shift 키로 회피(Dodge) 발동 가능
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                TriggerDodge();
                return;
            }

            // 에디터 테스트용 키보드 입력 + 모바일 가상 조이스틱 입력 결합
            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _moveInput = _virtualJoystickInput.sqrMagnitude > 0.01f ? _virtualJoystickInput : keyboardInput;

            if (_moveInput.sqrMagnitude > 1f)
                _moveInput.Normalize();

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastFacingDir = _moveInput.normalized;
                if (characterSpriteRenderer != null && Mathf.Abs(_moveInput.x) > 0.05f)
                {
                    characterSpriteRenderer.flipX = _moveInput.x < 0f;
                }
            }

            UpdateCharacterVisualAnimation();
        }

        private void UpdateCharacterVisualAnimation()
        {
            if (characterRig != null)
            {
                if (_cachedCombat == null)
                    _cachedCombat = GetComponent<PlayerCombat>();

                bool isCharging = _cachedCombat != null && _cachedCombat.IsHoldingCharge;
                bool isCarving = _cachedCombat != null && _cachedCombat.IsHoldingCarve;
                float chargeProgress = _cachedCombat != null ? _cachedCombat.HoldNormalizedProgress : 0f;
                Vector2 vel = _rb != null ? _rb.linearVelocity : (_moveInput * moveSpeed);

                characterRig.UpdateLocomotion(vel, _isDodging, isCharging, isCarving, chargeProgress);
                return;
            }

            UpdateSDChibiAnimation();
        }

        /// <summary>
        /// 가디언 테일즈(ref/img) 특유의 2등신 SD 캐릭터가 달릴 때 통통 튀는 바운스 & 숨쉬기 모션을 연출합니다.
        /// </summary>
        private void UpdateSDChibiAnimation()
        {
            if (characterSpriteRenderer == null) return;
            Transform visualTr = characterSpriteRenderer.transform;

            if (_moveInput.sqrMagnitude > 0.01f || _isDodging)
            {
                float bounce = Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.11f;
                float tilt = Mathf.Sin(Time.time * 18f) * 6.5f;
                float stretchY = 1.0f + Mathf.Sin(Time.time * 36f) * 0.06f;
                float squashX = 1.0f - Mathf.Sin(Time.time * 36f) * 0.04f;

                visualTr.localPosition = new Vector3(0f, bounce, 0f);
                visualTr.localRotation = Quaternion.Euler(0f, 0f, tilt);
                visualTr.localScale = new Vector3(squashX, stretchY, 1f);
            }
            else
            {
                float breath = Mathf.Sin(Time.time * 4.0f) * 0.03f;
                visualTr.localPosition = Vector3.Lerp(visualTr.localPosition, Vector3.zero, Time.deltaTime * 12f);
                visualTr.localRotation = Quaternion.Lerp(visualTr.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                visualTr.localScale = new Vector3(1.0f - breath * 0.5f, 1.0f + breath, 1f);
            }
        }

        private void FixedUpdate()
        {
            if (_isDodging)
            {
                _rb.linearVelocity = _lastFacingDir * dodgeSpeed;
            }
            else if (_isAttackLunging)
            {
                _rb.linearVelocity = _attackLungeDir * _attackLungeSpeed;
            }
            else
            {
                _rb.linearVelocity = _moveInput * moveSpeed;
            }
        }

        /// <summary>
        /// 모바일 가상 플로팅 조이스틱 UI에서 매 프레임 호출합니다.
        /// </summary>
        public void SetVirtualJoystickInput(Vector2 joystickVector)
        {
            _virtualJoystickInput = Vector2.ClampMagnitude(joystickVector, 1f);
        }

        /// <summary>
        /// 모바일 회피/스텝 버튼 터치 시 호출합니다.
        /// </summary>
        public void TriggerDodge()
        {
            if (_isDodging || _dodgeCooldownTimer > 0f) return;

            _isDodging = true;
            _dodgeTimer = dodgeDuration;
            _dodgeCooldownTimer = dodgeCooldown;
        }
    }
}
