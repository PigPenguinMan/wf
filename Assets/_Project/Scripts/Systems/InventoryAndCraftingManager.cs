using System;
using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;

namespace WildFrontier.Systems
{
    /// <summary>
    /// 인벤토리, 재료 압축(Compression), 재료 변이(Mutation), 장비/요리 제작(Crafting)을 담당하는 핵심 매니저
    /// </summary>
    public class InventoryAndCraftingManager : MonoBehaviour
    {
        public static InventoryAndCraftingManager Instance { get; private set; }

        [Header("보유 재화 및 인벤토리")]
        [SerializeField] private int currentGold = 500;
        private readonly Dictionary<ItemDataSO, int> _itemCounts = new Dictionary<ItemDataSO, int>();
        private readonly HashSet<CraftingRecipeSO> _unlockedRecipes = new HashSet<CraftingRecipeSO>();

        public event Action<ItemDataSO, int> OnItemAdded;
        public event Action<CraftingRecipeSO> OnRecipeUnlocked;
        public event Action<ItemDataSO, int, bool> OnCraftCompleted; // (결과 아이템, 수량, 대성공 여부)

        public int CurrentGold => currentGold;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            currentGold += amount;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public int GetItemCount(ItemDataSO item)
        {
            if (item == null) return 0;
            return _itemCounts.TryGetValue(item, out int count) ? count : 0;
        }

        public void AddItem(ItemDataSO item, int amount = 1)
        {
            if (item == null || amount <= 0) return;

            // 갈무리로 얻은 제작법(레시피북)인 경우 즉시 해금 처리 가능
            if (item.category == ItemCategory.RecipeBook && item.unlockedRecipe != null)
            {
                UnlockRecipe(item.unlockedRecipe);
            }

            if (_itemCounts.ContainsKey(item))
                _itemCounts[item] += amount;
            else
                _itemCounts[item] = amount;

            Debug.Log($"[인벤토리] '{item.itemName}' x{amount} 획득! (현재 보유: {_itemCounts[item]})");
            OnItemAdded?.Invoke(item, amount);
        }

        public bool RemoveItem(ItemDataSO item, int amount = 1)
        {
            if (GetItemCount(item) < amount) return false;

            _itemCounts[item] -= amount;
            if (_itemCounts[item] <= 0)
                _itemCounts.Remove(item);

            return true;
        }

        public void UnlockRecipe(CraftingRecipeSO recipe)
        {
            if (recipe == null || _unlockedRecipes.Contains(recipe)) return;
            _unlockedRecipes.Add(recipe);
            Debug.Log($"[제작법 해금] 새로운 제작법 '{recipe.recipeName}'을(를) 습득했습니다!");
            OnRecipeUnlocked?.Invoke(recipe);
        }

        /// <summary>
        /// [와일드 프론티어 핵심 1] 재료 압축 (Material Compression)
        /// 하위 등급 재료 N개를 압축하여 상위 등급 재료 1개로 업그레이드합니다.
        /// </summary>
        public bool TryCompressMaterial(ItemDataSO sourceMaterial, int batchCount = 1)
        {
            if (sourceMaterial == null || !sourceMaterial.canCompress || sourceMaterial.compressedResultItem == null)
            {
                Debug.LogWarning("[재료 압축] 압축할 수 없는 아이템입니다.");
                return false;
            }

            int totalRequired = sourceMaterial.compressRequiredCount * batchCount;
            int totalGoldCost = sourceMaterial.compressGoldCost * batchCount;

            if (GetItemCount(sourceMaterial) < totalRequired)
            {
                Debug.LogWarning($"[재료 압축] 재료가 부족합니다. ({GetItemCount(sourceMaterial)}/{totalRequired})");
                return false;
            }

            if (currentGold < totalGoldCost)
            {
                Debug.LogWarning("[재료 압축] 골드가 부족합니다.");
                return false;
            }

            currentGold -= totalGoldCost;
            RemoveItem(sourceMaterial, totalRequired);
            AddItem(sourceMaterial.compressedResultItem, batchCount);

            Debug.Log($"[재료 압축 성공] {sourceMaterial.itemName} x{totalRequired} ➔ {sourceMaterial.compressedResultItem.itemName} x{batchCount}");
            return true;
        }

        /// <summary>
        /// [와일드 프론티어 핵심 2] 장비 제작, 모닥불 요리, 재료 변이(Mutation), 룬 조합 실행
        /// </summary>
        public bool TryExecuteRecipe(CraftingRecipeSO recipe)
        {
            if (recipe == null) return false;

            // 재료 및 골드 검증
            if (currentGold < recipe.goldCost)
            {
                Debug.LogWarning("[제작] 골드가 부족합니다.");
                return false;
            }

            foreach (var ing in recipe.ingredients)
            {
                if (GetItemCount(ing.materialItem) < ing.count)
                {
                    Debug.LogWarning($"[제작] '{ing.materialItem.itemName}' 재료가 부족합니다.");
                    return false;
                }
            }

            // 비용 및 재료 차감
            currentGold -= recipe.goldCost;
            foreach (var ing in recipe.ingredients)
            {
                RemoveItem(ing.materialItem, ing.count);
            }

            // 성공 / 대성공(변이 크리티컬) 판정
            float roll = UnityEngine.Random.value;
            if (roll > recipe.baseSuccessRate)
            {
                Debug.Log("[제작 실패] 재료 변이/조합에 실패했습니다.");
                return false;
            }

            bool isCritical = recipe.bonusCriticalResultItem != null &&
                              UnityEngine.Random.value <= recipe.criticalSuccessRate;

            ItemDataSO finalItem = isCritical ? recipe.bonusCriticalResultItem : recipe.resultItem;
            AddItem(finalItem, recipe.resultCount);

            OnCraftCompleted?.Invoke(finalItem, recipe.resultCount, isCritical);
            Debug.Log($"[제작 완료] {(isCritical ? "★대성공★ " : "")}'{finalItem.itemName}' x{recipe.resultCount} 제작!");
            return true;
        }
    }
}
