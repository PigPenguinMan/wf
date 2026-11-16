using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;
using WildFrontier.Monster;

namespace WildFrontier.Player
{
    /// <summary>
    /// 플레이어의 스탯, 클래스(워리어/스카우터/가디언), 룬 장착 효과,
    /// 그리고 '스마트 컨텍스트 액션 버튼(공격/차지 vs 시체 갈무리/채집)'을 총괄하는 전투 컨트롤러입니다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("직업 및 기본 스탯")]
        [SerializeField] private CharacterClass characterClass = CharacterClass.Warrior;
        [SerializeField] private int maxHp = 250;
        [SerializeField] private int currentHp = 250;
        [SerializeField] private int baseAttack = 25;
        [SerializeField] private int baseDefense = 8;
        [SerializeField] [Range(0f, 0.8f)] private float evasionRate = 0.10f; // 회피율 (반사 탱커·스카우터 핵심)
        [SerializeField] [Range(0f, 1f)] private float critRate = 0.15f;

        [Header("장착 룬 목록 & 5대 룬 프리셋")]
        [SerializeField] private List<RuneDataSO> equippedRunes = new List<RuneDataSO>();
        [SerializeField] private RuneDataSO fireRune;
        [SerializeField] private RuneDataSO lightningRune;
        [SerializeField] private RuneDataSO iceRune;
        [SerializeField] private RuneDataSO vampireRune;
        [SerializeField] private RuneDataSO thornsRune;
        private int _currentRunePresetIndex;

        [Header("2등신 SD 클래스별 도트 스프라이트 & 파츠 분리형 오버사이즈 무기")]
        [SerializeField] private SpriteRenderer characterSpriteRenderer;
        [SerializeField] private Sprite warriorSDSprite;
        [SerializeField] private Sprite scouterSDSprite;
        [SerializeField] private Sprite guardianSDSprite;
        [SerializeField] private Transform weaponPivotTransform;
        [SerializeField] private SpriteRenderer slashArcRenderer;
        [SerializeField] private GuardianTalesCharacterRig characterRig;

        private PlayerController _playerController;
        private float _swingAnimTimer;
        private MonsterBase _lastTargetedMonster;
        private int _comboStep;
        private float _comboResetTimer;

        [Header("공격 및 차지(Charge) 설정")]
        [SerializeField] private float attackRadius = 1.3f;
        [SerializeField] private float maxChargeTime = 1.2f; // 워리어 차지 스매시 완충 시간
        [SerializeField] private float interactDetectRadius = 1.1f;
        [SerializeField] private float subGunCooldown = 4.0f;
        [SerializeField] private LayerMask monsterAndInteractLayer;

        // 런타임 홀드(갈무리 or 차지 공격) 상태
        private bool _isHoldingAction;
        private float _holdTimer;
        private IHoldInteractable _activeInteractable;
        private ActionContextType _currentContext = ActionContextType.Attack;

        // 클래스 스킬 & 보조 총기 쿨타임 / 가디언 실드 스핀 상태
        private float _subGunCooldownTimer;
        private float _classSkillCooldownTimer;
        private float _guardianSpinTimer;
        private float _guardianSpinTickTimer;

        public ActionContextType CurrentContext => _currentContext;
        public CharacterClass ActiveClass => characterClass;
        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public MonsterBase LastTargetedMonster => _lastTargetedMonster;
        public string CurrentRunePresetName { get; private set; } = "🔥화염 + ⚡전격 (극딜·부위파괴)";
        public float SubGunCooldownNormalized => Mathf.Clamp01(_subGunCooldownTimer / Mathf.Max(0.1f, subGunCooldown));
        public float ClassSkillCooldownNormalized => Mathf.Clamp01(_classSkillCooldownTimer / Mathf.Max(0.1f, GetClassSkillMaxCooldown()));

        public void ConfigureWeaponRig(Transform weaponPivot, SpriteRenderer slashArc)
        {
            weaponPivotTransform = weaponPivot;
            slashArcRenderer = slashArc;
            if (slashArcRenderer != null)
                slashArcRenderer.enabled = false;
        }

        public void ConfigureCombat(CharacterClass cls, LayerMask interactLayer, params RuneDataSO[] starterRunes)
        {
            characterClass = cls;
            monsterAndInteractLayer = interactLayer;
            equippedRunes = new List<RuneDataSO>(starterRunes);
        }

        public void ConfigureRunePresets(RuneDataSO fire, RuneDataSO lightning, RuneDataSO ice, RuneDataSO vampire, RuneDataSO thorns)
        {
            fireRune = fire;
            lightningRune = lightning;
            iceRune = ice;
            vampireRune = vampire;
            thornsRune = thorns;
            ApplyRunePreset(0);
        }

        public void CycleRunePreset()
        {
            _currentRunePresetIndex = (_currentRunePresetIndex + 1) % 3;
            ApplyRunePreset(_currentRunePresetIndex);

            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.15f,
                $"💎 룬 빌드 변경: {CurrentRunePresetName}", new Color(0.55f, 0.85f, 1.0f), 21, 1.6f);
        }

        private void ApplyRunePreset(int index)
        {
            equippedRunes.Clear();
            switch (index)
            {
                case 0:
                    CurrentRunePresetName = "🔥화염 + ⚡전격 (극딜·부위파괴)";
                    if (fireRune != null) equippedRunes.Add(fireRune);
                    if (lightningRune != null) equippedRunes.Add(lightningRune);
                    break;
                case 1:
                    CurrentRunePresetName = "❄️냉기 + 🩸흡혈 (둔화·무한유지력)";
                    if (iceRune != null) equippedRunes.Add(iceRune);
                    if (vampireRune != null) equippedRunes.Add(vampireRune);
                    break;
                default:
                    CurrentRunePresetName = "🛡️가시 + 🩸흡혈 (회피 반사탱커)";
                    if (thornsRune != null) equippedRunes.Add(thornsRune);
                    if (vampireRune != null) equippedRunes.Add(vampireRune);
                    break;
            }
        }

        public void ConfigureClassSprites(SpriteRenderer sr, Sprite warriorSpr, Sprite scouterSpr, Sprite guardianSpr)
        {
            characterSpriteRenderer = sr;
            warriorSDSprite = warriorSpr;
            scouterSDSprite = scouterSpr;
            guardianSDSprite = guardianSpr;
            RefreshClassSprite();
        }

        public void ConfigureCharacterRig(GuardianTalesCharacterRig rig)
        {
            characterRig = rig;
            if (characterRig != null)
                characterRig.ApplyClassVisuals(characterClass);
        }

        public void CycleCharacterClass()
        {
            if (characterClass == CharacterClass.Warrior)
                characterClass = CharacterClass.Scouter;
            else if (characterClass == CharacterClass.Scouter)
                characterClass = CharacterClass.Guardian;
            else
                characterClass = CharacterClass.Warrior;

            RefreshClassSprite();

            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.0f,
                $"⚔️ 클래스 전환: [{characterClass}]", new Color(0.55f, 0.90f, 1.0f), 22, 1.5f);
        }

        private void RefreshClassSprite()
        {
            if (characterRig != null)
                characterRig.ApplyClassVisuals(characterClass);

            if (characterSpriteRenderer == null) return;
            if (characterClass == CharacterClass.Warrior && warriorSDSprite != null)
                characterSpriteRenderer.sprite = warriorSDSprite;
            else if (characterClass == CharacterClass.Scouter && scouterSDSprite != null)
                characterSpriteRenderer.sprite = scouterSDSprite;
            else if (characterClass == CharacterClass.Guardian && guardianSDSprite != null)
                characterSpriteRenderer.sprite = guardianSDSprite;
        }

        public void EquipItemBonus(ItemDataSO equipment)
        {
            if (equipment == null) return;
            baseAttack += equipment.bonusAttack;
            baseDefense += equipment.bonusDefense;
            maxHp += equipment.bonusMaxHp;
            currentHp = maxHp;
            evasionRate = Mathf.Clamp(evasionRate + equipment.bonusEvasionRate, 0f, 0.8f);

            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.3f,
                $"🛡️ [{equipment.itemName}] 자동 장착! (방어+{equipment.bonusDefense}, HP+{equipment.bonusMaxHp})",
                new Color(0.4f, 1f, 0.6f), 22, 2.0f);
        }

        public bool IsHoldingCharge => _isHoldingAction && _activeInteractable == null;
        public bool IsHoldingCarve => _isHoldingAction && _activeInteractable != null;

        public float HoldNormalizedProgress
        {
            get
            {
                if (!_isHoldingAction) return 0f;
                if (_activeInteractable != null)
                    return Mathf.Clamp01(_holdTimer / Mathf.Max(0.1f, _activeInteractable.RequiredHoldDuration));
                return Mathf.Clamp01(_holdTimer / maxChargeTime);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateSkillTimers(dt);

            // 에디터 테스트 편의: 스페이스바(스마트 액션), Q(클래스 스킬), E(보조 총기 사격)
            if (Input.GetKeyDown(KeyCode.Space))
                OnActionButtonDown();
            if (Input.GetKeyUp(KeyCode.Space))
                OnActionButtonUp();

            if (Input.GetKeyDown(KeyCode.Q))
                TriggerClassActiveSkill();
            if (Input.GetKeyDown(KeyCode.E))
                TriggerSubGunShot();

            if (!_isHoldingAction)
            {
                DetectNearestContext();
            }
            else
            {
                UpdateActionHold(dt);
            }

            UpdateWeaponSwingVisual(dt);
        }

        private void UpdateSkillTimers(float dt)
        {
            if (_comboResetTimer > 0f)
            {
                _comboResetTimer -= dt;
                if (_comboResetTimer <= 0f)
                    _comboStep = 0;
            }

            if (_subGunCooldownTimer > 0f)
                _subGunCooldownTimer = Mathf.Max(0f, _subGunCooldownTimer - dt);

            if (_classSkillCooldownTimer > 0f)
                _classSkillCooldownTimer = Mathf.Max(0f, _classSkillCooldownTimer - dt);

            if (_guardianSpinTimer > 0f)
            {
                _guardianSpinTimer -= dt;
                _guardianSpinTickTimer -= dt;
                if (_guardianSpinTickTimer <= 0f)
                {
                    _guardianSpinTickTimer = 0.3f;
                    ExecuteGuardianSpinTick();
                }
            }
        }

        private float GetClassSkillMaxCooldown()
        {
            switch (characterClass)
            {
                case CharacterClass.Warrior: return 7.0f;
                case CharacterClass.Scouter: return 5.0f;
                case CharacterClass.Guardian: return 6.5f;
                default: return 6.0f;
            }
        }

        private void UpdateWeaponSwingVisual(float dt)
        {
            if (_guardianSpinTimer > 0f)
            {
                if (weaponPivotTransform != null)
                    weaponPivotTransform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 900f);
                return;
            }

            if (_swingAnimTimer > 0f)
            {
                _swingAnimTimer -= dt;
                float t = 1f - Mathf.Clamp01(_swingAnimTimer / 0.22f);
                if (characterRig == null && weaponPivotTransform != null)
                {
                    float zAngle = Mathf.Lerp(65f, -95f, t);
                    weaponPivotTransform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
                }

                if (slashArcRenderer != null)
                {
                    slashArcRenderer.enabled = true;
                    Color c = slashArcRenderer.color;
                    slashArcRenderer.color = new Color(c.r, c.g, c.b, 1f - t);
                }
            }
            else
            {
                if (slashArcRenderer != null && slashArcRenderer.enabled)
                    slashArcRenderer.enabled = false;

                if (characterRig != null || weaponPivotTransform == null)
                    return;

                // 갈무리 홀드 중에는 빠르게 칼질하는 모션, 평소에는 대기 각도
                if (_isHoldingAction && _activeInteractable != null)
                {
                    float carveSwing = Mathf.Sin(Time.time * 28f) * 35f;
                    weaponPivotTransform.localRotation = Quaternion.Euler(0f, 0f, carveSwing);
                }
                else if (_isHoldingAction && _activeInteractable == null)
                {
                    // 워리어 차지 중에는 대검을 뒤로 젖히며 파르르 떨림
                    float tremble = Mathf.Sin(Time.time * 45f) * 4f;
                    weaponPivotTransform.localRotation = Quaternion.Euler(0f, 0f, 75f + tremble);
                }
                else
                {
                    weaponPivotTransform.localRotation = Quaternion.Lerp(
                        weaponPivotTransform.localRotation,
                        Quaternion.Euler(0f, 0f, 18f),
                        dt * 12f);
                }
            }
        }

        /// <summary>
        /// 주변에 시체(MonsterCorpse)나 채집물이 있는지 탐색하여
        /// 모바일 우측 메인 버튼의 아이콘(⚔️ 공격 <-> 🔪 갈무리 <-> 🌿 채집)을 자동 전환합니다.
        /// </summary>
        private void DetectNearestContext()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactDetectRadius, monsterAndInteractLayer);
            IHoldInteractable nearest = null;
            float minDist = float.MaxValue;

            foreach (var col in hits)
            {
                var interactable = col.GetComponent<IHoldInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    float d = Vector2.Distance(transform.position, interactable.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = interactable;
                    }
                }
            }

            _activeInteractable = nearest;
            _currentContext = nearest != null ? nearest.ContextType : ActionContextType.Attack;
        }

        /// <summary>
        /// 모바일 우측 메인 액션 버튼을 누르기 시작했을 때 (PointerDown)
        /// </summary>
        public void OnActionButtonDown()
        {
            _isHoldingAction = true;
            _holdTimer = 0f;

            if (_activeInteractable != null && _activeInteractable.CanInteract)
            {
                _activeInteractable.OnInteractStart(gameObject);
            }
        }

        /// <summary>
        /// 모바일 우측 메인 액션 버튼에서 손을 뗐을 때 (PointerUp)
        /// </summary>
        public void OnActionButtonUp()
        {
            if (!_isHoldingAction) return;
            _isHoldingAction = false;

            // 1. 시체 갈무리 중이었는데 완료 전에 손을 뗀 경우 -> 갈무리 취소
            if (_activeInteractable != null)
            {
                if (_activeInteractable.CanInteract)
                {
                    _activeInteractable.OnInteractCancel();
                }
                _activeInteractable = null;
                return;
            }

            // 2. 공격 모드에서 손을 뗀 경우 -> 일반 타격 또는 워리어 차지 스매시 발동
            bool isFullCharge = characterClass == CharacterClass.Warrior && _holdTimer >= maxChargeTime;
            ExecuteMeleeAttack(isFullCharge, _holdTimer);
            _holdTimer = 0f;
        }

        private void UpdateActionHold(float dt)
        {
            _holdTimer += dt;

            // 시체 갈무리 또는 채집 홀드 진행
            if (_activeInteractable != null)
            {
                if (!_activeInteractable.CanInteract)
                {
                    _isHoldingAction = false;
                    _activeInteractable = null;
                    return;
                }

                float progress = Mathf.Clamp01(_holdTimer / Mathf.Max(0.1f, _activeInteractable.RequiredHoldDuration));
                _activeInteractable.OnInteractProgress(progress);

                if (progress >= 1.0f)
                {
                    _activeInteractable.OnInteractComplete(gameObject);
                    _isHoldingAction = false;
                    _activeInteractable = null;
                    _holdTimer = 0f;
                }
            }
        }

        private int GetEffectiveBaseAttack()
        {
            int bestiaryBonus = Systems.BestiaryManager.Instance != null
                ? Systems.BestiaryManager.Instance.TotalBonusAttack
                : 0;
            return baseAttack + bestiaryBonus;
        }

        private void ExecuteMeleeAttack(bool isFullCharge, float chargeSeconds)
        {
            int activeCombo = _comboStep;
            float comboMultiplier = activeCombo == 2 ? 1.3f : 1.0f;
            _comboStep = (_comboStep + 1) % 3;
            _comboResetTimer = 1.25f;

            float classMultiplier = 1.0f;
            if (characterClass == CharacterClass.Warrior)
            {
                // 워리어: 차지 시간에 비례해 최대 2.5배 강력한 범위 스매시
                classMultiplier = isFullCharge ? 2.5f : Mathf.Lerp(1.0f, 1.6f, chargeSeconds / maxChargeTime);
            }
            else if (characterClass == CharacterClass.Scouter)
            {
                // 스카우터: 빠른 쌍단검 치명타 보정
                classMultiplier = 1.15f;
            }

            int effectiveAtk = GetEffectiveBaseAttack();
            bool isCrit = Random.value <= (critRate + (characterClass == CharacterClass.Scouter ? 0.15f : 0f));
            int rawDamage = Mathf.RoundToInt(effectiveAtk * classMultiplier * comboMultiplier * (isCrit ? 1.6f : 1.0f));

            // 공격 전진 스텝(가디언 테일즈식 타격 전진) + 멀티 파츠 3타 콤보 모션
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_playerController != null)
            {
                Vector2 lungeDir = GetAimDirection(3.2f);
                _playerController.TriggerAttackLunge(lungeDir, isFullCharge ? 6.0f : 4.5f, isFullCharge ? 0.14f : 0.10f);
            }
            if (characterRig != null)
                characterRig.PlayAttackComboMotion(activeCombo, isFullCharge);

            // 파츠 분리형 오버사이즈 무기 스윙 및 슬래시 검기 이펙트 트리거
            _swingAnimTimer = 0.22f;
            if (slashArcRenderer != null)
            {
                slashArcRenderer.transform.localScale = Vector3.one * (isFullCharge ? 1.45f : (activeCombo == 2 ? 1.22f : 1.0f));
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius, monsterAndInteractLayer);
            foreach (var hit in hits)
            {
                var monster = hit.GetComponent<MonsterBase>();
                if (monster != null && !monster.IsDead)
                {
                    _lastTargetedMonster = monster;
                    monster.TakeDamage(rawDamage, GetPrimaryRuneElement(), isFullCharge || activeCombo == 2, transform);
                    TriggerOffensiveRunes(monster, rawDamage);
                }
            }
        }

        /// <summary>
        /// [공통 보조 무기] 견제 사격(Sub-Gun): 가장 가까운 몬스터 방향으로 투사체를 발사해 원거리 어그로 유인 및 룬 상태를 부여합니다.
        /// </summary>
        public void TriggerSubGunShot()
        {
            if (_subGunCooldownTimer > 0f)
            {
                UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 0.9f,
                    $"🔫 재장전 중 ({_subGunCooldownTimer:F1}s)", new Color(0.85f, 0.85f, 0.85f), 17, 0.7f);
                return;
            }

            _subGunCooldownTimer = subGunCooldown;

            Vector2 dir = GetAimDirection(10f);
            int shotDamage = Mathf.Max(1, Mathf.RoundToInt(GetEffectiveBaseAttack() * 0.9f));

            GameObject projGo = new GameObject("SubGunProjectile");
            projGo.transform.position = transform.position + (Vector3)(dir * 0.35f);
            SubGunProjectile proj = projGo.AddComponent<SubGunProjectile>();
            proj.Initialize(dir, shotDamage, GetPrimaryRuneElement(), this);
        }

        /// <summary>
        /// [3대 클래스 액티브 스킬] 워리어(대지 돌파), 스카우터(그림자 기습 & 질풍 난무), 가디언(실드 스핀)
        /// </summary>
        public void TriggerClassActiveSkill()
        {
            if (_classSkillCooldownTimer > 0f)
            {
                UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 0.9f,
                    $"⚡ 스킬 대기 ({_classSkillCooldownTimer:F1}s)", new Color(0.85f, 0.85f, 0.85f), 17, 0.7f);
                return;
            }

            if (characterClass == CharacterClass.Scouter)
            {
                MonsterBase target = FindNearestAliveMonster(3.8f);
                if (target == null)
                {
                    UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 0.9f,
                        "⚡ 사거리(3.8m) 내 대상이 없습니다!", new Color(1.0f, 0.65f, 0.35f), 18, 0.9f);
                    return;
                }

                _classSkillCooldownTimer = GetClassSkillMaxCooldown();
                ExecuteScouterShadowFlurry(target);
                return;
            }

            _classSkillCooldownTimer = GetClassSkillMaxCooldown();

            if (characterClass == CharacterClass.Warrior)
            {
                ExecuteWarriorBreakthrough();
            }
            else if (characterClass == CharacterClass.Guardian)
            {
                _guardianSpinTimer = 1.5f;
                _guardianSpinTickTimer = 0f;
                UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.1f,
                    "🛡️ 실드 스핀!", new Color(0.75f, 0.85f, 1.0f), 22, 1.1f);
            }
        }

        private void ExecuteWarriorBreakthrough()
        {
            Vector2 dir = GetAimDirection(6.0f);
            Vector3 startPos = transform.position;
            Vector3 dashOffset = (Vector3)(dir * 1.6f);
            Vector3 midPos = startPos + dashOffset * 0.5f;
            transform.position = startPos + dashOffset;

            _swingAnimTimer = 0.22f;
            if (slashArcRenderer != null)
                slashArcRenderer.transform.localScale = Vector3.one * 1.45f;

            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.1f,
                "💥 대지 돌파!", new Color(1.0f, 0.60f, 0.20f), 23, 1.1f);

            int hitDmg = Mathf.RoundToInt(GetEffectiveBaseAttack() * 1.8f);
            ElementType elem = GetPrimaryRuneElement();

            Collider2D[] hits = Physics2D.OverlapCircleAll(midPos, 1.6f, monsterAndInteractLayer);
            foreach (var hit in hits)
            {
                var monster = hit.GetComponent<MonsterBase>();
                if (monster != null && !monster.IsDead)
                {
                    _lastTargetedMonster = monster;
                    // 2연타 + 강한 경직/부위 파괴 누적(isHeavyChargeHit: true)
                    monster.TakeDamage(hitDmg, elem, true, transform);
                    TriggerOffensiveRunes(monster, hitDmg);
                    if (!monster.IsDead)
                    {
                        monster.TakeDamage(hitDmg, elem, true, transform);
                        TriggerOffensiveRunes(monster, hitDmg);
                    }
                }
            }
        }

        private void ExecuteScouterShadowFlurry(MonsterBase target)
        {
            Vector2 dirToTarget = ((Vector2)(target.transform.position - transform.position));
            if (dirToTarget.sqrMagnitude < 0.001f) dirToTarget = Vector2.right;
            else dirToTarget.Normalize();

            // 대상 등 뒤(+0.6f 오프셋)로 순간 이동
            transform.position = target.transform.position + (Vector3)(dirToTarget * 0.6f);
            if (characterSpriteRenderer != null)
                characterSpriteRenderer.flipX = dirToTarget.x > 0f;

            _lastTargetedMonster = target;
            _swingAnimTimer = 0.22f;
            if (slashArcRenderer != null)
                slashArcRenderer.transform.localScale = Vector3.one * 1.15f;

            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 1.1f,
                "⚡ 그림자 난무!", new Color(0.45f, 1.0f, 0.80f), 22, 1.1f);

            ElementType elem = GetPrimaryRuneElement();
            int effectiveAtk = GetEffectiveBaseAttack();
            int backstabDmg = Mathf.RoundToInt(effectiveAtk * 2.1f);
            target.TakeDamage(backstabDmg, elem, true, transform);
            TriggerOffensiveRunes(target, backstabDmg);

            int flurryDmg = Mathf.RoundToInt(effectiveAtk * 0.55f);
            for (int i = 0; i < 3; i++)
            {
                if (target.IsDead) break;
                target.TakeDamage(flurryDmg, elem, false, transform);
                TriggerOffensiveRunes(target, flurryDmg);
            }
        }

        private void ExecuteGuardianSpinTick()
        {
            int spinDmg = Mathf.Max(1, Mathf.RoundToInt(GetEffectiveBaseAttack() * 0.75f));
            ElementType elem = GetPrimaryRuneElement();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.6f, monsterAndInteractLayer);
            foreach (var hit in hits)
            {
                var monster = hit.GetComponent<MonsterBase>();
                if (monster != null && !monster.IsDead)
                {
                    _lastTargetedMonster = monster;
                    monster.TakeDamage(spinDmg, elem, false, transform);
                    TriggerOffensiveRunes(monster, spinDmg);
                }
            }
        }

        private MonsterBase FindNearestAliveMonster(float maxRange)
        {
            MonsterBase[] all = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
            MonsterBase best = null;
            float minDist = maxRange;
            foreach (var m in all)
            {
                if (m != null && !m.IsDead)
                {
                    float d = Vector2.Distance(transform.position, m.transform.position);
                    if (d <= minDist)
                    {
                        minDist = d;
                        best = m;
                    }
                }
            }
            return best;
        }

        private Vector2 GetAimDirection(float searchRange)
        {
            MonsterBase nearest = FindNearestAliveMonster(searchRange);
            if (nearest != null)
            {
                Vector2 diff = nearest.transform.position - transform.position;
                if (diff.sqrMagnitude > 0.001f)
                    return diff.normalized;
            }

            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (moveInput.sqrMagnitude > 0.01f)
                return moveInput.normalized;

            if (characterRig != null)
                return characterRig.FacingSign < 0 ? Vector2.left : Vector2.right;

            return (characterSpriteRenderer != null && characterSpriteRenderer.flipX) ? Vector2.left : Vector2.right;
        }

        public void ApplyEquippedRunesToTarget(MonsterBase target, int dealtDamage)
        {
            if (target == null || target.IsDead) return;
            TriggerOffensiveRunes(target, dealtDamage);
        }

        /// <summary>
        /// 몬스터에게 피격당할 때 호출됩니다.
        /// ★와일드 프론티어 명물 '반사 탱커(반탱)' 메커니즘 완벽 구현:
        /// 공격을 회피(Evasion)하더라도 가시 스킬/룬의 반사 대미지가 적에게 그대로 들어갑니다!
        /// </summary>
        public void ReceiveMonsterAttack(MonsterBase attacker, int incomingDamage)
        {
            bool isEvaded = Random.value <= evasionRate;

            // 반사 대미지 계산 (가디언 클래스 패시브 + 가시/심판 룬, 실드 스핀 중 1.5배 증폭)
            float reflectRatio = characterClass == CharacterClass.Guardian ? 0.45f : 0f;
            foreach (var rune in equippedRunes)
            {
                if (rune != null && rune.runeElement == ElementType.Thorns)
                {
                    reflectRatio += rune.thornsReflectMultiplier;
                }
            }
            if (_guardianSpinTimer > 0f)
            {
                reflectRatio *= 1.5f;
            }

            if (reflectRatio > 0f && attacker != null && !attacker.IsDead)
            {
                int reflectedDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * reflectRatio));
                attacker.TakeDamage(reflectedDamage, ElementType.Physical, false, transform);
                UI.FloatingPopupManager.SpawnPopup(attacker.transform.position + Vector3.up * 0.8f,
                    $"🛡️가시 반사 -{reflectedDamage}!", new Color(0.85f, 0.55f, 1.0f), 20, 1.1f);
                Debug.Log($"[가시 반사 발동] {(isEvaded ? "회피 성공과 동시에" : "방패 방어로")} {reflectedDamage}의 반사 피해를 입혔습니다!");
            }

            if (isEvaded)
            {
                UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 0.7f,
                    "💨 회피(MISS)!", new Color(0.5f, 0.95f, 1.0f), 19, 1.0f);
                Debug.Log("[회피!] 몬스터의 공격을 날렵하게 회피했습니다.");
                return;
            }

            int finalTaken = Mathf.Max(1, incomingDamage - baseDefense);
            if (_guardianSpinTimer > 0f)
            {
                finalTaken = Mathf.Max(1, Mathf.RoundToInt(finalTaken * 0.5f));
            }
            currentHp = Mathf.Max(0, currentHp - finalTaken);
            if (characterRig != null)
                characterRig.PlayHurtReaction();
            UI.FloatingPopupManager.SpawnPopup(transform.position + Vector3.up * 0.6f,
                $"-{finalTaken}", new Color(1.0f, 0.35f, 0.35f), 19, 0.9f);
        }

        private ElementType GetPrimaryRuneElement()
        {
            foreach (var rune in equippedRunes)
            {
                if (rune != null && (rune.runeElement == ElementType.Fire ||
                                     rune.runeElement == ElementType.Lightning ||
                                     rune.runeElement == ElementType.Ice))
                {
                    return rune.runeElement;
                }
            }
            return ElementType.Physical;
        }

        private void TriggerOffensiveRunes(MonsterBase target, int dealtDamage)
        {
            foreach (var rune in equippedRunes)
            {
                if (rune == null) continue;

                // 흡혈 룬 처리
                if (rune.runeElement == ElementType.Vampire && Random.value <= rune.procChance)
                {
                    int heal = Mathf.Max(1, Mathf.RoundToInt(dealtDamage * rune.lifeStealRatio));
                    currentHp = Mathf.Min(maxHp, currentHp + heal);
                }
                else
                {
                    target.ApplyRuneEffect(rune, dealtDamage);
                }
            }
        }
    }
}
