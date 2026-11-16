using UnityEngine;
using UnityEngine.UI;
using WildFrontier.Core;
using WildFrontier.Player;

namespace WildFrontier.UI
{
    /// <summary>
    /// 모바일 터치 화면의 우측 '스마트 컨텍스트 액션 버튼' 및 갈무리/차지 원형 게이지를 제어하는 HUD 스크립트입니다.
    /// 평소에는 ⚔️ 공격 아이콘이다가, 몬스터 시체 위에 서면 🔪 갈무리 아이콘으로 자동 전환됩니다.
    /// </summary>
    public class MobileHUDController : MonoBehaviour
    {
        [Header("플레이어 참조")]
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerController playerController;

        [Header("스마트 액션 버튼 UI")]
        [SerializeField] private Image actionButtonIcon;
        [SerializeField] private Image holdProgressRing; // 갈무리 & 워리어 차지 진행도 원형 링 (Fill Amount 0~1)
        [SerializeField] private Text actionButtonLabel;

        [Header("컨텍스트별 32x32 도트 아이콘")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite carveKnifeIcon;
        [SerializeField] private Sprite gatherHerbIcon;
        [SerializeField] private Sprite campfireIcon;

        [Header("클래스 스킬 & 보조 총기 버튼 UI")]
        [SerializeField] private Text classSkillButtonLabel;
        [SerializeField] private Image classSkillCooldownFill;
        [SerializeField] private Image subGunCooldownFill;

        private ActionContextType _lastDisplayedContext = (ActionContextType)(-1);
        private CharacterClass _lastDisplayedClass = (CharacterClass)(-1);

        public void ConfigureSkillButtons(Text classSkillLabel, Image classSkillCdFill, Image subGunCdFill)
        {
            classSkillButtonLabel = classSkillLabel;
            classSkillCooldownFill = classSkillCdFill;
            subGunCooldownFill = subGunCdFill;
        }

        public void ConfigureHUD(PlayerCombat combat, PlayerController controller, Image icon, Image ring, Text label,
            Sprite atkSprite, Sprite carveSprite, Sprite gatherSprite, Sprite campSprite)
        {
            playerCombat = combat;
            playerController = controller;
            actionButtonIcon = icon;
            holdProgressRing = ring;
            actionButtonLabel = label;
            attackIcon = atkSprite;
            carveKnifeIcon = carveSprite;
            gatherHerbIcon = gatherSprite;
            campfireIcon = campSprite;
        }

        private void Update()
        {
            if (playerCombat == null) return;

            // 1. 컨텍스트 변화 감지 및 아이콘/라벨 자동 교체
            ActionContextType current = playerCombat.CurrentContext;
            if (current != _lastDisplayedContext)
            {
                _lastDisplayedContext = current;
                RefreshSmartButtonVisual(current);
            }

            // 2. 갈무리 또는 워리어 차지 홀드 게이지 업데이트
            if (holdProgressRing != null)
            {
                holdProgressRing.fillAmount = playerCombat.HoldNormalizedProgress;
            }

            // 3. 클래스별 스킬명 자동 갱신 & 쿨타임 오버레이 표시
            CharacterClass activeCls = playerCombat.ActiveClass;
            if (activeCls != _lastDisplayedClass)
            {
                _lastDisplayedClass = activeCls;
                RefreshClassSkillLabel(activeCls);
            }

            if (classSkillCooldownFill != null)
            {
                classSkillCooldownFill.fillAmount = playerCombat.ClassSkillCooldownNormalized;
            }

            if (subGunCooldownFill != null)
            {
                subGunCooldownFill.fillAmount = playerCombat.SubGunCooldownNormalized;
            }
        }

        private void RefreshClassSkillLabel(CharacterClass cls)
        {
            if (classSkillButtonLabel == null) return;

            switch (cls)
            {
                case CharacterClass.Warrior:
                    classSkillButtonLabel.text = "⚡ 대지 돌파\n(Q)";
                    break;
                case CharacterClass.Scouter:
                    classSkillButtonLabel.text = "⚡ 그림자 난무\n(Q)";
                    break;
                case CharacterClass.Guardian:
                    classSkillButtonLabel.text = "⚡ 실드 스핀\n(Q)";
                    break;
                default:
                    classSkillButtonLabel.text = "⚡ 클래스 스킬\n(Q)";
                    break;
            }
        }

        private void RefreshSmartButtonVisual(ActionContextType context)
        {
            if (actionButtonLabel != null)
            {
                switch (context)
                {
                    case ActionContextType.Carve:
                        actionButtonLabel.text = "갈무리 (Hold)";
                        if (actionButtonIcon != null && carveKnifeIcon != null)
                            actionButtonIcon.sprite = carveKnifeIcon;
                        break;

                    case ActionContextType.Gather:
                        actionButtonLabel.text = "채집 (Hold)";
                        if (actionButtonIcon != null && gatherHerbIcon != null)
                            actionButtonIcon.sprite = gatherHerbIcon;
                        break;

                    case ActionContextType.Campfire:
                        actionButtonLabel.text = "캠프 제작";
                        if (actionButtonIcon != null && campfireIcon != null)
                            actionButtonIcon.sprite = campfireIcon;
                        break;

                    default:
                        actionButtonLabel.text = "공격 / 차지";
                        if (actionButtonIcon != null && attackIcon != null)
                            actionButtonIcon.sprite = attackIcon;
                        break;
                }
            }
        }

        // EventTrigger 컴포넌트(PointerDown / PointerUp)에 연결할 메서드
        public void OnSmartActionPointerDown()
        {
            if (playerCombat != null)
                playerCombat.OnActionButtonDown();
        }

        public void OnSmartActionPointerUp()
        {
            if (playerCombat != null)
                playerCombat.OnActionButtonUp();
        }

        public void OnDodgeButtonClicked()
        {
            if (playerController != null)
                playerController.TriggerDodge();
        }

        public void OnClassSkillButtonClicked()
        {
            if (playerCombat != null)
                playerCombat.TriggerClassActiveSkill();
        }

        public void OnSubGunButtonClicked()
        {
            if (playerCombat != null)
                playerCombat.TriggerSubGunShot();
        }
    }
}
