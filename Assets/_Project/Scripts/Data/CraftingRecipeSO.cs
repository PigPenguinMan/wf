using System;
using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Data
{
    [Serializable]
    public struct RecipeIngredient
    {
        public ItemDataSO materialItem;
        public int count;
    }

    public enum RecipeType
    {
        EquipmentCraft,     // 무기/방어구 제작
        CampfireCooking,    // 모닥불 캠프 요리 (기후 저항, 갈무리 행운 버프 등)
        MaterialMutation,   // 재료 변이 (서로 다른 계열 재료 조합 -> 특수 변이 재료)
        RuneCombine         // 하위 등급 룬 -> 상위 등급 룬 조합
    }

    /// <summary>
    /// 장비 제작, 모닥불 요리, 재료 변이, 룬 조합에 사용되는 레시피 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "WildFrontier/Data/Crafting Recipe")]
    public class CraftingRecipeSO : ScriptableObject
    {
        public string recipeId;
        public string recipeName;
        public RecipeType recipeType;

        [Header("요구 재료 목록")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public int goldCost = 100;

        [Header("제작 결과물")]
        public ItemDataSO resultItem;
        public int resultCount = 1;

        [Header("변이/특수 제작 대성공 확률")]
        [Range(0f, 1f)] public float baseSuccessRate = 1.0f;
        [Tooltip("대성공(크리티컬 제작) 시 한 단계 높은 등급 또는 보너스 결과물 획득")]
        public ItemDataSO bonusCriticalResultItem;
        [Range(0f, 0.5f)] public float criticalSuccessRate = 0.1f;
    }
}
