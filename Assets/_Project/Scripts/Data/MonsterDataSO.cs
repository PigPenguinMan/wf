using System;
using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Data
{
    [Serializable]
    public struct LootDropEntry
    {
        public ItemDataSO item;
        [Range(0f, 1f)] public float baseDropChance;   // 기본 드랍 확률 (0.0 ~ 1.0)
        public int minAmount;
        public int maxAmount;
        [Tooltip("오버킬 또는 약점 속성으로 처치 시 추가 드랍 확률 보너스")]
        [Range(0f, 0.5f)] public float overkillBonusChance;
    }

    /// <summary>
    /// 몬스터 생태, 전투 스탯, 시체 갈무리(Carving) 테이블, 펫 테이밍 정보를 담는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Monster_", menuName = "WildFrontier/Data/Monster Data")]
    public class MonsterDataSO : ScriptableObject
    {
        [Header("생태계 기본 정보")]
        public string monsterId;
        public string monsterName;
        public MonsterSpecies species;
        public MonsterSize size;
        public int level = 1;
        public ElementType weaknessElement = ElementType.Fire;

        [Header("전투 능력치")]
        public int maxHp = 100;
        public int attackPower = 12;
        public int defense = 4;
        public float moveSpeed = 2.5f;
        public float chaseRange = 5.0f;
        public float attackRange = 1.2f;
        public float attackCooldown = 1.5f;
        public int expReward = 25;

        [Header("낮/밤 생태 특성")]
        [Tooltip("야행성 여부: 체크 시 밤(Night) 시간대에 공격력과 이동속도가 25% 상승합니다.")]
        public bool isNocturnal;

        [Header("시체 갈무리 (Carving) 데이터 - 와일드 프론티어 핵심")]
        [Tooltip("시체가 필드에 유지되는 시간 (초)")]
        public float corpseDuration = 20f;
        [Tooltip("갈무리 게이지를 채우는 데 걸리는 기본 시간 (초)")]
        public float carveHoldTime = 1.2f;
        [Tooltip("갈무리 성공 시 획득 가능한 재료, 제작법(레시피북), 룬 목록")]
        public List<LootDropEntry> carvingLootTable = new List<LootDropEntry>();

        [Header("펫 테이밍 (소형 S 몬스터 & 특수 네임드 '덕구' 등)")]
        public bool canBeTamed;
        [Tooltip("유혹에 필요한 먹이 아이템")]
        public ItemDataSO favoriteBaitItem;
        [Range(0f, 1f)] public float baseTameChance = 0.25f;
        [Tooltip("주인공과 동등한 레벨까지 성장했을 때 개방되는 특수 패시브 설명")]
        public string awakenedPetSkillDescription;
    }
}
