using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;

namespace WildFrontier.Monster
{
    /// <summary>
    /// 몬스터 AI, 상태이상(화염 도트, 전격 공/방 50% 감소, 냉기 둔화), 사망 시 시체(MonsterCorpse) 전환을 담당하는 클래스
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MonsterBase : MonoBehaviour
    {
        public enum AIState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            DeadCorpse
        }

        [Header("몬스터 데이터")]
        [SerializeField] private MonsterDataSO monsterData;
        [SerializeField] private MonsterCorpse corpseComponent;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("런타임 상태")]
        [SerializeField] private AIState currentState = AIState.Idle;
        [SerializeField] private int currentHp;

        // 룬 디버프 상태
        private float _burnTimer;
        private float _burnTickTimer;
        private int _burnDamagePerTick;

        private float _lightningDebuffTimer;
        private float _statReductionRatio; // 전격 룬: 공격력/방어력 감소율

        private float _iceSlowTimer;
        private float _slowRatio;          // 냉기 룬: 이속/공속 감소율

        private Transform _targetPlayer;
        private float _attackCooldownTimer;

        // 보스/준보스 패턴 전조(바닥 레드존) & 부위 파괴 그로기 상태
        private static Sprite _cachedTelegraphCircleSprite;
        private bool _isPreparingBossSkill;
        private float _bossSkillChargeTimer;
        private float _bossSkillCooldownTimer = 3.0f;
        private float _groggyTimer;
        private bool _hasBrokenPart;
        private SpriteRenderer _telegraphBorderRenderer;
        private SpriteRenderer _telegraphFillRenderer;

        private float _hopPhase;
        private float _hitFlashTimer;
        private float _attackLungeTimer;
        private Vector2 _attackLungeDir = Vector2.right;
        private Vector3 _baseSpriteScale = Vector3.one;
        private SpriteRenderer _footShadowRenderer;
        private static Sprite _cachedMonsterShadowSprite;

        private const float BossSkillTelegraphDuration = 1.6f;
        private const float BossSkillCooldown = 8.0f;

        public MonsterDataSO Data => monsterData;
        public bool IsDead => currentState == AIState.DeadCorpse;
        public int CurrentHp => currentHp;
        public bool IsGroggy => _groggyTimer > 0f;

        public void ConfigureMonster(MonsterDataSO data, SpriteRenderer sr, MonsterCorpse corpse)
        {
            monsterData = data;
            spriteRenderer = sr;
            corpseComponent = corpse;
            if (data != null) currentHp = data.maxHp;
        }

        private void Start()
        {
            if (monsterData != null)
            {
                currentHp = monsterData.maxHp;
            }
            if (corpseComponent == null)
            {
                corpseComponent = GetComponent<MonsterCorpse>();
            }
            if (corpseComponent != null)
            {
                corpseComponent.enabled = false;
            }

            if (spriteRenderer != null)
                _baseSpriteScale = spriteRenderer.transform.localScale;
            _hopPhase = Random.Range(0f, Mathf.PI * 2f);
            EnsureMonsterFootShadow();
        }

        private void EnsureMonsterFootShadow()
        {
            if (_footShadowRenderer != null) return;
            if (_cachedMonsterShadowSprite == null)
            {
                const int w = 32;
                const int h = 16;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Vector2 center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float nx = (x - center.x) / (w * 0.45f);
                        float ny = (y - center.y) / (h * 0.42f);
                        float d = nx * nx + ny * ny;
                        float a = d <= 1f ? Mathf.Lerp(0.45f, 0.05f, d) : 0f;
                        tex.SetPixel(x, y, new Color(0.08f, 0.14f, 0.12f, a));
                    }
                }
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                _cachedMonsterShadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            }

            GameObject shadowGo = new GameObject("MonsterFootShadow");
            shadowGo.transform.SetParent(transform, false);
            bool isBoss = monsterData != null && monsterData.size == MonsterSize.Boss;
            shadowGo.transform.localPosition = new Vector3(0f, isBoss ? -0.52f : -0.30f, 0f);
            shadowGo.transform.localScale = Vector3.one * (isBoss ? 1.85f : 0.95f);

            _footShadowRenderer = shadowGo.AddComponent<SpriteRenderer>();
            _footShadowRenderer.sprite = _cachedMonsterShadowSprite;
            _footShadowRenderer.sortingOrder = 1;
        }

        private void Update()
        {
            if (IsDead) return;

            float dt = Time.deltaTime;
            UpdateElementalDebuffs(dt);
            UpdateHitFlashAndMotion(dt);

            if (_groggyTimer > 0f)
            {
                _groggyTimer -= dt;
                if (spriteRenderer != null)
                {
                    float shake = Mathf.Sin(Time.time * 28f) * 0.04f;
                    spriteRenderer.transform.localPosition = new Vector3(shake, 0f, 0f);
                    if (_groggyTimer <= 0f)
                        spriteRenderer.transform.localPosition = Vector3.zero;
                }
                return;
            }

            UpdateAI(dt);
        }

        private void UpdateHitFlashAndMotion(float dt)
        {
            if (spriteRenderer == null) return;

            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= dt;
                bool redPhase = Mathf.FloorToInt(_hitFlashTimer * 30f) % 2 == 0;
                spriteRenderer.color = redPhase ? new Color(1f, 0.42f, 0.42f, 1f) : Color.white;
            }
            else if (spriteRenderer.color != Color.white)
            {
                spriteRenderer.color = Color.white;
            }

            if (_attackLungeTimer > 0f)
                _attackLungeTimer -= dt;

            if (_groggyTimer > 0f || spriteRenderer.transform == transform)
                return;

            Vector3 localPos = Vector3.zero;
            Vector3 localScale = _baseSpriteScale;
            float tiltZ = 0f;

            if (_attackLungeTimer > 0f)
            {
                float t = 1f - Mathf.Clamp01(_attackLungeTimer / 0.20f);
                float wave = Mathf.Sin(t * Mathf.PI);
                localPos += (Vector3)(_attackLungeDir * (0.24f * wave));
                localScale = new Vector3(_baseSpriteScale.x * (1f + 0.14f * wave), _baseSpriteScale.y * (1f - 0.10f * wave), _baseSpriteScale.z);
            }
            else if (currentState == AIState.Chase)
            {
                _hopPhase += dt * 11f;
                float hop = Mathf.Abs(Mathf.Sin(_hopPhase));
                float cos = Mathf.Cos(_hopPhase);
                localPos.y = hop * 0.11f;
                tiltZ = cos * 6f;
                localScale = new Vector3(_baseSpriteScale.x * (1f - hop * 0.06f), _baseSpriteScale.y * (1f + hop * 0.06f), _baseSpriteScale.z);
            }
            else
            {
                _hopPhase += dt * 2.8f;
                float breathe = Mathf.Sin(_hopPhase);
                localPos.y = breathe * 0.02f;
            }

            if (_hitFlashTimer > 0f)
            {
                float hitT = Mathf.Clamp01(_hitFlashTimer / 0.18f);
                localScale = new Vector3(_baseSpriteScale.x * (1f + 0.18f * hitT), _baseSpriteScale.y * (1f - 0.15f * hitT), _baseSpriteScale.z);
            }

            spriteRenderer.transform.localPosition = localPos;
            spriteRenderer.transform.localScale = localScale;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
        }

        private void UpdateElementalDebuffs(float dt)
        {
            // 1. 화염 룬 화상 도트 대미지
            if (_burnTimer > 0f)
            {
                _burnTimer -= dt;
                _burnTickTimer -= dt;
                if (_burnTickTimer <= 0f)
                {
                    _burnTickTimer = 0.8f;
                    TakeDamage(_burnDamagePerTick, ElementType.Fire, false);
                }
            }

            // 2. 전격 룬 공/방 저하 타이머
            if (_lightningDebuffTimer > 0f)
            {
                _lightningDebuffTimer -= dt;
                if (_lightningDebuffTimer <= 0f) _statReductionRatio = 0f;
            }

            // 3. 냉기 룬 둔화 타이머
            if (_iceSlowTimer > 0f)
            {
                _iceSlowTimer -= dt;
                if (_iceSlowTimer <= 0f) _slowRatio = 0f;
            }
        }

        private void UpdateAI(float dt)
        {
            if (monsterData == null) return;

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= dt;

            if (_targetPlayer == null) return;

            // 준보스(Lv.8 이상) / 보스 전용 바닥 레드존 패턴 전조 처리
            if (IsBossOrMiniBoss())
            {
                if (_isPreparingBossSkill)
                {
                    UpdateBossSkillTelegraph(dt);
                    return;
                }

                _bossSkillCooldownTimer -= dt;
                float skillRadius = GetBossSkillRadius();
                if (_bossSkillCooldownTimer <= 0f && Vector2.Distance(transform.position, _targetPlayer.position) <= skillRadius + 1.5f)
                {
                    StartBossSkillTelegraph();
                    return;
                }
            }

            float dist = Vector2.Distance(transform.position, _targetPlayer.position);
            if (dist <= monsterData.attackRange)
            {
                currentState = AIState.Attack;
                if (_attackCooldownTimer <= 0f)
                {
                    _attackCooldownTimer = monsterData.attackCooldown;
                    _attackLungeTimer = 0.20f;
                    Vector2 atkDir = _targetPlayer.position - transform.position;
                    _attackLungeDir = atkDir.sqrMagnitude > 0.001f ? atkDir.normalized : Vector2.right;

                    var playerCombat = _targetPlayer.GetComponent<Player.PlayerCombat>();
                    if (playerCombat != null)
                    {
                        TimeOfDay tod = Systems.WorldEnvironmentManager.Instance != null
                            ? Systems.WorldEnvironmentManager.Instance.CurrentTimeOfDay
                            : TimeOfDay.Day;
                        playerCombat.ReceiveMonsterAttack(this, GetEffectiveAttackPower(tod));
                    }
                }
            }
            else if (dist <= monsterData.chaseRange)
            {
                currentState = AIState.Chase;
                float effectiveSpeed = monsterData.moveSpeed * (1f - _slowRatio);
                Vector2 dir = (_targetPlayer.position - transform.position).normalized;
                transform.position += (Vector3)(dir * effectiveSpeed * dt);

                if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.05f)
                    spriteRenderer.flipX = dir.x < 0f;
            }
            else
            {
                currentState = AIState.Idle;
            }
        }

        public int GetEffectiveAttackPower(TimeOfDay currentTimeOfDay)
        {
            if (monsterData == null) return 1;
            float atk = monsterData.attackPower;

            // 야행성 몬스터는 밤에 공격력 25% 상승
            if (monsterData.isNocturnal && currentTimeOfDay == TimeOfDay.Night)
                atk *= 1.25f;

            // 전격 룬 디버프 적용
            atk *= (1f - _statReductionRatio);
            return Mathf.Max(1, Mathf.RoundToInt(atk));
        }

        public int GetEffectiveDefense()
        {
            if (monsterData == null || _groggyTimer > 0f) return 0;
            float def = monsterData.defense * (1f - _statReductionRatio);
            return Mathf.Max(0, Mathf.RoundToInt(def));
        }

        private bool IsBossOrMiniBoss()
        {
            return monsterData != null && (monsterData.size == MonsterSize.Boss || monsterData.level >= 8);
        }

        private float GetBossSkillRadius()
        {
            if (monsterData == null) return 1.8f;
            return monsterData.size == MonsterSize.Boss ? 2.8f : 1.9f;
        }

        private void StartBossSkillTelegraph()
        {
            _isPreparingBossSkill = true;
            _bossSkillChargeTimer = 0f;
            EnsureTelegraphRenderers();
            UpdateTelegraphVisual(0f);

            UI.FloatingPopupManager.SpawnPopup(
                transform.position + Vector3.up * 1.25f,
                "⚠️ [강공격 예고! 회피 또는 차지/스킬로 차단!]",
                new Color(1.0f, 0.35f, 0.25f),
                20,
                1.5f);
        }

        private void UpdateBossSkillTelegraph(float dt)
        {
            _bossSkillChargeTimer += dt;
            float progress = Mathf.Clamp01(_bossSkillChargeTimer / BossSkillTelegraphDuration);
            UpdateTelegraphVisual(progress);

            if (_bossSkillChargeTimer < BossSkillTelegraphDuration) return;

            // 보스 광역 패턴 폭발
            _isPreparingBossSkill = false;
            _bossSkillCooldownTimer = BossSkillCooldown;
            HideTelegraphVisual();

            float skillRadius = GetBossSkillRadius();
            if (_targetPlayer != null && Vector2.Distance(transform.position, _targetPlayer.position) <= skillRadius)
            {
                var playerCombat = _targetPlayer.GetComponent<Player.PlayerCombat>();
                if (playerCombat != null)
                {
                    TimeOfDay tod = Systems.WorldEnvironmentManager.Instance != null
                        ? Systems.WorldEnvironmentManager.Instance.CurrentTimeOfDay
                        : TimeOfDay.Day;
                    int bossSkillDamage = Mathf.Max(1, Mathf.RoundToInt(GetEffectiveAttackPower(tod) * 1.8f));
                    playerCombat.ReceiveMonsterAttack(this, bossSkillDamage);
                }
            }

            UI.FloatingPopupManager.SpawnPopup(
                transform.position + Vector3.up * 1.2f,
                "💢 [보스 강공격 폭발!]",
                new Color(1.0f, 0.25f, 0.20f),
                22,
                1.2f);
        }

        private void TriggerPartBreakGroggy()
        {
            _isPreparingBossSkill = false;
            _bossSkillCooldownTimer = BossSkillCooldown;
            _groggyTimer = 4.0f;
            _hasBrokenPart = true;
            HideTelegraphVisual();

            UI.FloatingPopupManager.SpawnPopup(
                transform.position + Vector3.up * 1.35f,
                "💥 [부위 파괴 & 그로기!] (4초간 방어력 0)",
                new Color(1.0f, 0.85f, 0.20f),
                23,
                2.0f);
        }

        private void EnsureTelegraphRenderers()
        {
            if (_telegraphBorderRenderer != null && _telegraphFillRenderer != null) return;

            Sprite circleSprite = GetOrCreateTelegraphCircleSprite();

            GameObject borderGo = new GameObject("BossTelegraph_Border");
            borderGo.transform.SetParent(transform, false);
            _telegraphBorderRenderer = borderGo.AddComponent<SpriteRenderer>();
            _telegraphBorderRenderer.sprite = circleSprite;
            _telegraphBorderRenderer.color = new Color(1.0f, 0.20f, 0.20f, 0.30f);
            _telegraphBorderRenderer.sortingOrder = 1;

            GameObject fillGo = new GameObject("BossTelegraph_Fill");
            fillGo.transform.SetParent(transform, false);
            _telegraphFillRenderer = fillGo.AddComponent<SpriteRenderer>();
            _telegraphFillRenderer.sprite = circleSprite;
            _telegraphFillRenderer.color = new Color(1.0f, 0.12f, 0.12f, 0.45f);
            _telegraphFillRenderer.sortingOrder = 2;
        }

        private void UpdateTelegraphVisual(float progress)
        {
            float diameter = GetBossSkillRadius() * 2f;
            if (_telegraphBorderRenderer != null)
            {
                _telegraphBorderRenderer.enabled = true;
                _telegraphBorderRenderer.transform.localScale = Vector3.one * diameter;
            }
            if (_telegraphFillRenderer != null)
            {
                _telegraphFillRenderer.enabled = true;
                _telegraphFillRenderer.transform.localScale = Vector3.one * (diameter * Mathf.Max(0.05f, progress));
            }
        }

        private void HideTelegraphVisual()
        {
            if (_telegraphBorderRenderer != null) _telegraphBorderRenderer.enabled = false;
            if (_telegraphFillRenderer != null) _telegraphFillRenderer.enabled = false;
        }

        private static Sprite GetOrCreateTelegraphCircleSprite()
        {
            if (_cachedTelegraphCircleSprite != null) return _cachedTelegraphCircleSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, d <= 0.98f ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _cachedTelegraphCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _cachedTelegraphCircleSprite;
        }

        /// <summary>
        /// 플레이어의 공격 또는 반사 대미지를 받을 때 호출됩니다.
        /// </summary>
        public void TakeDamage(int rawDamage, ElementType attackElement, bool isHeavyChargeHit, Transform attacker = null)
        {
            if (IsDead) return;
            if (attacker != null) _targetPlayer = attacker;

            bool isWeakness = monsterData != null && monsterData.weaknessElement == attackElement && attackElement != ElementType.Physical;
            float weaknessMultiplier = isWeakness ? 1.35f : 1.0f;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * weaknessMultiplier) - GetEffectiveDefense());
            currentHp -= finalDamage;
            _hitFlashTimer = 0.18f;

            // 가디언 테일즈식 피격 넉백 (일반 몬스터 또는 그로기 보스)
            if (attacker != null && currentHp > 0 && (!IsBossOrMiniBoss() || _groggyTimer > 0f))
            {
                Vector2 pushDir = (Vector2)(transform.position - attacker.position);
                if (pushDir.sqrMagnitude > 0.001f)
                {
                    transform.position += (Vector3)(pushDir.normalized * (isHeavyChargeHit ? 0.26f : 0.12f));
                }
            }

            // 데미지 팝업 텍스트 표시
            Color dmgColor = isHeavyChargeHit
                ? new Color(1.0f, 0.55f, 0.15f)
                : (isWeakness ? new Color(1.0f, 0.90f, 0.25f) : Color.white);
            string prefix = isHeavyChargeHit ? "💥차지 " : (isWeakness ? "★약점 " : "");
            UI.FloatingPopupManager.SpawnPopup(
                transform.position + new Vector3(Random.Range(-0.25f, 0.25f), 0.65f, 0f),
                $"{prefix}-{finalDamage}", dmgColor, isHeavyChargeHit ? 24 : 20, 0.95f);

            // 보스 패턴 전조 중 강공격/스킬 또는 전격 디버프로 타격 시 부위 파괴 & 그로기 유발
            if (currentHp > 0 && _isPreparingBossSkill && (isHeavyChargeHit || _lightningDebuffTimer > 0f))
            {
                TriggerPartBreakGroggy();
            }

            if ( currentHp <= 0 )
            {
                _hitFlashTimer = 0f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                    if (spriteRenderer.transform != transform)
                    {
                        spriteRenderer.transform.localPosition = Vector3.zero;
                        spriteRenderer.transform.localScale = _baseSpriteScale;
                        spriteRenderer.transform.localRotation = Quaternion.identity;
                    }
                }
                if (_footShadowRenderer != null)
                    _footShadowRenderer.enabled = false;

                bool overkillBonus = _hasBrokenPart || isWeakness || isHeavyChargeHit || Mathf.Abs(currentHp) >= (monsterData.maxHp * 0.25f);
                if (Systems.PetTamingSystem.Instance != null)
                {
                    Systems.PetTamingSystem.Instance.AddPetExp(monsterData.expReward, 5);
                }
                DieAndBecomeCorpse(overkillBonus);
            }
        }

        /// <summary>
        /// 장착된 룬 효과(화상, 전격 디버프, 냉기 둔화)를 몬스터에게 적용합니다.
        /// </summary>
        public void ApplyRuneEffect(RuneDataSO rune, int dealtDamage)
        {
            if (rune == null || IsDead) return;
            if (Random.value > rune.procChance) return;

            switch (rune.runeElement)
            {
                case ElementType.Fire:
                    _burnTimer = rune.effectDuration;
                    _burnTickTimer = 0.8f;
                    _burnDamagePerTick = Mathf.Max(1, Mathf.RoundToInt(dealtDamage * rune.burnDamageRatio));
                    break;

                case ElementType.Lightning:
                    _lightningDebuffTimer = rune.effectDuration;
                    _statReductionRatio = Mathf.Max(_statReductionRatio, rune.lightningStatReductionRatio);
                    if (_isPreparingBossSkill)
                        TriggerPartBreakGroggy();
                    Debug.Log($"[전격 룬 발동] '{monsterData.monsterName}'의 공격력/방어력이 {_statReductionRatio * 100f:F0}% 감소했습니다!");
                    break;

                case ElementType.Ice:
                    _iceSlowTimer = rune.effectDuration;
                    _slowRatio = Mathf.Max(_slowRatio, rune.iceSlowRatio);
                    break;
            }
        }

        private void DieAndBecomeCorpse(bool overkillBonus)
        {
            currentState = AIState.DeadCorpse;
            currentHp = 0;
            _isPreparingBossSkill = false;
            _groggyTimer = 0f;
            HideTelegraphVisual();
            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = Vector3.zero;

            if (monsterData != null)
            {
                if (Systems.BestiaryManager.Instance != null)
                    Systems.BestiaryManager.Instance.RecordKill(monsterData);

                if (Systems.InventoryAndCraftingManager.Instance != null)
                    Systems.InventoryAndCraftingManager.Instance.AddGold(monsterData.level * 15);
            }

            Debug.Log($"[몬스터 토벌] '{monsterData.monsterName}' 처치 완료! 시체 위에서 공격 버튼을 길게 눌러 갈무리하세요.");

            if (corpseComponent == null)
                corpseComponent = gameObject.AddComponent<MonsterCorpse>();

            corpseComponent.enabled = true;
            corpseComponent.InitializeCorpse(monsterData, overkillBonus, spriteRenderer);

            // 사망 시 몬스터 도트 색상을 어둡게 변경하여 시체 상태임을 직관적으로 표시
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                spriteRenderer.color = new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, 0.9f);
            }
        }
    }
}
