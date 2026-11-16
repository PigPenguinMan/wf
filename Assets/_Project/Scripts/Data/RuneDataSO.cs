using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Data
{
    /// <summary>
    /// 와일드 프론티어 특유의 룬(Rune) 시스템 데이터.
    /// 화염(도트 딜), 전격(공/방 50% 디버프), 냉기(둔화), 흡혈, 가시(회피 시에도 반사 대미지 발동)를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Rune_", menuName = "WildFrontier/Data/Rune Data")]
    public class RuneDataSO : ItemDataSO
    {
        [Header("룬 고유 속성 및 메커니즘")]
        public ElementType runeElement;

        [Tooltip("발동 확률 (0.0 ~ 1.0)")]
        [Range(0f, 1f)] public float procChance = 0.25f;

        [Header("속성별 수치 설정")]
        [Tooltip("화염: 입힌 대미지 대비 틱당 화상 비율 (예: 0.1 = 10%씩 5회)")]
        public float burnDamageRatio = 0.10f;

        [Tooltip("전격: 보스/몬스터 공격력 및 방어력 감소 비율 (원작 전격 룬: 최대 0.5 = 50% 감소)")]
        public float lightningStatReductionRatio = 0.30f;

        [Tooltip("냉기: 이동속도 및 공격속도 둔화율 (예: 0.4 = 40% 둔화)")]
        public float iceSlowRatio = 0.35f;

        [Tooltip("흡혈/흡기: 타격 대미지 대비 HP/MP 회복 비율")]
        public float lifeStealRatio = 0.08f;

        [Tooltip("가시/심판(반사탱커 핵심): 피격 또는 회피 성공 시 적에게 되돌려주는 반사 계수")]
        public float thornsReflectMultiplier = 1.2f;

        public float effectDuration = 4.0f;
    }
}
