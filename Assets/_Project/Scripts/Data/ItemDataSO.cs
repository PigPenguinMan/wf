using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Data
{
    /// <summary>
    /// 아이템 데이터 ScriptableObject.
    /// 원작 와일드 프론티어의 핵심인 '재료 압축(Compression)'과 '장비 페이퍼돌(Paper-doll) 도트 스프라이트' 정보를 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "WildFrontier/Data/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)]
        public string description;
        public ItemCategory category;
        public ItemGrade grade;
        public Sprite icon32x32;        // 32x32 픽셀아트 아이콘
        public int maxStack = 99;
        public int sellPrice = 10;

        [Header("재료 압축 (Material Compression) 설정")]
        [Tooltip("체크 시 하위 재료 여러 개를 압축하여 상위 등급 재료로 변환할 수 있습니다.")]
        public bool canCompress;
        [Tooltip("압축에 필요한 동일 재료 개수 (예: 거친 가죽 5개 -> 질긴 가죽 1개)")]
        public int compressRequiredCount = 5;
        public int compressGoldCost = 50;
        public ItemDataSO compressedResultItem;

        [Header("장비 전용 스탯 (Equipment인 경우)")]
        public CharacterClass requiredClass = CharacterClass.None;
        public int bonusAttack;
        public int bonusDefense;
        public int bonusMaxHp;
        public float bonusCritRate;     // 0.05 = 5%
        public float bonusEvasionRate;  // 회피율 (반사 탱커 및 스카우터 핵심)
        public int runeSlotCount = 2;   // 룬 장착 가능 슬롯 수
        public Sprite paperDollSpriteSheet; // 장착 시 캐릭터 외형에 반영될 32x32 도트 시트

        [Header("레시피북 전용 (RecipeBook인 경우)")]
        [Tooltip("갈무리로 획득한 제작법 책을 사용했을 때 해금되는 레시피")]
        public CraftingRecipeSO unlockedRecipe;
    }
}
