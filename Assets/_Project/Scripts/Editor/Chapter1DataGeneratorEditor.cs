#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WildFrontier.Core;
using WildFrontier.Data;

namespace WildFrontier.Editor
{
    /// <summary>
    /// Unity 상단 메뉴 [WildFrontier > Generate Chapter 1 Starter Data] 클릭 한 번으로
    /// 1챕터 아이템(재료 압축 연결 포함), 룬, 제작/변이 레시피, 몬스터(뿔토끼, 덕구, 고대 수호목 등)
    /// ScriptableObject 에셋들을 자동으로 생성하고 참조를 연결해 주는 에디터 툴입니다.
    /// </summary>
    public static class Chapter1DataGeneratorEditor
    {
        private const string BasePath = "Assets/_Project/ScriptableObjects/Chapter1";

        [MenuItem("WildFrontier/Generate Chapter 1 Starter Data (1챕터 데이터 자동 생성)")]
        public static void GenerateAllChapter1Data()
        {
            EnsureDirectory($"{BasePath}/Items");
            EnsureDirectory($"{BasePath}/Runes");
            EnsureDirectory($"{BasePath}/Recipes");
            EnsureDirectory($"{BasePath}/Monsters");

            // 1. 가죽 및 등껍질 재료 (압축 체인 역순 생성 후 연결)
            var leather03 = CreateItem("MAT_LEATHER_03", "고급 무두 가죽", "두껍게 압축된 최고급 가죽.", ItemCategory.Material, ItemGrade.Rare, 120);
            var leather02 = CreateItem("MAT_LEATHER_02", "질긴 가죽", "거친 가죽 5개를 압축해 만든 질긴 가죽.", ItemCategory.Material, ItemGrade.Uncommon, 30, true, 5, 200, leather03);
            var leather01 = CreateItem("MAT_LEATHER_01", "거친 가죽", "초원 야수에게서 갈무리한 거친 가죽. 5개를 모아 질긴 가죽으로 압축할 수 있다.", ItemCategory.Material, ItemGrade.Common, 5, true, 5, 50, leather02);

            var shell03 = CreateItem("MAT_SHELL_03", "강철빛 중갑각", "강철처럼 단단하게 압축된 갑충 외피.", ItemCategory.Material, ItemGrade.Rare, 140);
            var shell02 = CreateItem("MAT_SHELL_02", "압축 갑충 판금", "단단한 등껍질을 압축 가공한 판금.", ItemCategory.Material, ItemGrade.Uncommon, 35, true, 5, 200, shell03);
            var shell01 = CreateItem("MAT_SHELL_01", "단단한 등껍질", "가시 딱정벌레에게서 갈무리한 등껍질.", ItemCategory.Material, ItemGrade.Common, 6, true, 5, 50, shell02);

            var meat01 = CreateItem("MAT_MEAT_01", "질긴 고기", "모닥불 요리와 테이밍 육포에 쓰이는 야생 고기.", ItemCategory.Material, ItemGrade.Common, 8);
            var spore02 = CreateItem("MAT_SPORE_02", "농축 마력 포자", "재료 변이의 촉매로 사용되는 고농축 포자.", ItemCategory.Material, ItemGrade.Uncommon, 40);
            var spore01 = CreateItem("MAT_SPORE_01", "형광 포자 가루", "밤에 빛나는 포자 가루. 4개를 압축해 농축 마력 포자를 만든다.", ItemCategory.Material, ItemGrade.Common, 8, true, 4, 40, spore02);
            var deokguFur = CreateItem("MAT_DEOKGU_FUR", "덕구의 황금 털가죽", "광폭한 덕구를 토벌하고 갈무리한 전설적인 털가죽.", ItemCategory.Material, ItemGrade.Rare, 250);
            var bossCore = CreateItem("MAT_ENT_HEART", "고대목의 심장", "1챕터 보스 고대 수호목 엔트의 맥동하는 핵.", ItemCategory.Material, ItemGrade.Epic, 800);
            var mutScale = CreateItem("MAT_MUT_SCALE", "변이된 비늘 가죽", "질긴 가죽과 형광 포자를 변이 연성하여 얻은 희귀 비늘 가죽.", ItemCategory.Material, ItemGrade.Rare, 220);
            var mutDragonScale = CreateItem("MAT_MUT_DRAGONSCALE", "용린화 가죽", "재료 변이 대성공으로 탄생한 영웅 등급의 용비늘 가죽.", ItemCategory.Material, ItemGrade.Epic, 500);

            // 테이밍 먹이 & 모닥불 캠프 요리
            var baitGrass = CreateItem("ITEM_BAIT_GRASS", "향긋한 풀잎", "초원 뿔토끼가 좋아하는 신선한 풀잎.", ItemCategory.Consumable, ItemGrade.Common, 15);
            var baitJerky = CreateItem("ITEM_BAIT_JERKY", "덕구가 좋아하는 특제 육포", "광폭한 덕구를 유혹할 수 있는 특제 육포.", ItemCategory.Consumable, ItemGrade.Rare, 150);
            var mealLuckSoup = CreateItem("ITEM_MEAL_LUCK_SOUP", "사냥꾼의 행운 포자 스프", "모닥불에서 끓여낸 특제 스프. 시체 갈무리 행운과 야간 시야를 높여준다.", ItemCategory.Consumable, ItemGrade.Uncommon, 85);

            // 2. 5대 핵심 룬 생성 (화염, 전격, 냉기, 흡혈, 가시/심판)
            var runeFire = CreateRune("RUNE_FIRE_01", "하급 화염의 룬", ElementType.Fire, ItemGrade.Common, 0.15f, burnRatio: 0.06f);
            var runeThorns = CreateRune("RUNE_THORNS_02", "중급 심판(가시)의 룬", ElementType.Thorns, ItemGrade.Uncommon, 1.0f, thornsMult: 0.70f);
            var runeLightning = CreateRune("RUNE_LIGHTNING_03", "희귀 전격의 룬", ElementType.Lightning, ItemGrade.Rare, 0.22f, lightningReduction: 0.30f);
            var runeIce = CreateRune("RUNE_ICE_02", "중급 냉기의 룬", ElementType.Ice, ItemGrade.Uncommon, 0.25f, iceSlow: 0.30f);
            var runeVampire = CreateRune("RUNE_VAMPIRE_02", "중급 흡혈의 룬", ElementType.Vampire, ItemGrade.Uncommon, 0.22f, lifeSteal: 0.10f);

            // 3. 대표 장비, 도안, 재료 변이, 모닥불 요리 레시피 생성
            var equipDeokguArmor = CreateEquipment("EQ_ARMOR_DEOKGU", "덕구 학살자의 견갑", CharacterClass.None, ItemGrade.Rare, atk: 5, def: 22, hp: 120, evasion: 0.05f, slots: 3);
            var recipeDeokguArmor = CreateRecipe("REC_EQ_DEOKGU", "덕구 학살자의 견갑 제작법", RecipeType.EquipmentCraft, equipDeokguArmor, 300,
                null, 0.1f, 1.0f,
                new RecipeIngredient { materialItem = deokguFur, count = 1 },
                new RecipeIngredient { materialItem = leather02, count = 3 });
            var bookDeokguArmor = CreateRecipeBook("BOOK_EQ_DEOKGU", "[도안] 덕구 학살자의 견갑", recipeDeokguArmor, ItemGrade.Rare);

            CreateRecipe("REC_MUT_01", "변이 비늘 가죽 연성", RecipeType.MaterialMutation, mutScale, 120,
                mutDragonScale, 0.20f, 1.0f,
                new RecipeIngredient { materialItem = leather02, count = 1 },
                new RecipeIngredient { materialItem = spore01, count = 2 });

            CreateRecipe("REC_COOK_02", "사냥꾼의 행운 포자 스프", RecipeType.CampfireCooking, mealLuckSoup, 30,
                null, 0.1f, 1.0f,
                new RecipeIngredient { materialItem = meat01, count = 1 },
                new RecipeIngredient { materialItem = spore01, count = 1 });

            // 4. 1챕터 6종 몬스터 생성 (초원 뿔토끼, 포자 독버섯, 가시 딱정벌레, 바람갈기 늑대, 광폭한 덕구, 고대 수호목 엔트)
            CreateMonster("MON_101_RABBIT", "초원 뿔토끼", MonsterSpecies.Beast, MonsterSize.Small, 1, 60, 8, 2, ElementType.Fire, false, 1.0f,
                canTame: true, bait: baitGrass, tameSkill: "갈무리 속도 +15% 증가",
                new LootDropEntry { item = leather01, baseDropChance = 0.80f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.15f },
                new LootDropEntry { item = meat01, baseDropChance = 0.60f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.15f });

            CreateMonster("MON_102_MUSHROOM", "포자 독버섯", MonsterSpecies.Plant, MonsterSize.Small, 2, 85, 10, 4, ElementType.Fire, true, 1.0f,
                canTame: true, bait: baitGrass, tameSkill: "초당 HP 1% 자연 회복",
                new LootDropEntry { item = spore01, baseDropChance = 0.85f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.15f },
                new LootDropEntry { item = runeFire, baseDropChance = 0.10f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.15f });

            CreateMonster("MON_103_BEETLE", "가시 딱정벌레", MonsterSpecies.Insect, MonsterSize.Medium, 4, 150, 15, 9, ElementType.Lightning, false, 1.2f,
                canTame: false, bait: null, tameSkill: "",
                new LootDropEntry { item = shell01, baseDropChance = 0.80f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.15f },
                new LootDropEntry { item = runeThorns, baseDropChance = 0.12f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.15f });

            CreateMonster("MON_104_WOLF", "바람갈기 늑대", MonsterSpecies.Beast, MonsterSize.Medium, 6, 220, 24, 7, ElementType.Ice, true, 1.3f,
                canTame: false, bait: null, tameSkill: "",
                new LootDropEntry { item = leather01, baseDropChance = 0.90f, minAmount = 2, maxAmount = 3, overkillBonusChance = 0.10f },
                new LootDropEntry { item = meat01, baseDropChance = 0.70f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.15f },
                new LootDropEntry { item = runeVampire, baseDropChance = 0.12f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.15f });

            CreateMonster("MON_105_DEOKGU", "광폭한 덕구", MonsterSpecies.Beast, MonsterSize.Small, 8, 550, 35, 14, ElementType.Lightning, false, 1.8f,
                canTame: true, bait: baitJerky, tameSkill: "플레이어 치명타율 +10% 및 공격력 +8%",
                new LootDropEntry { item = deokguFur, baseDropChance = 1.00f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0f },
                new LootDropEntry { item = leather02, baseDropChance = 0.70f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.20f },
                new LootDropEntry { item = bookDeokguArmor, baseDropChance = 0.25f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.25f },
                new LootDropEntry { item = runeThorns, baseDropChance = 0.12f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.15f });

            CreateMonster("MON_106_ENT_BOSS", "고대 수호목 엔트", MonsterSpecies.Plant, MonsterSize.Boss, 12, 2400, 58, 24, ElementType.Fire, false, 2.5f,
                canTame: false, bait: null, tameSkill: "",
                new LootDropEntry { item = bossCore, baseDropChance = 1.00f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0f },
                new LootDropEntry { item = runeLightning, baseDropChance = 0.15f, minAmount = 1, maxAmount = 1, overkillBonusChance = 0.20f },
                new LootDropEntry { item = runeFire, baseDropChance = 0.35f, minAmount = 1, maxAmount = 2, overkillBonusChance = 0.15f });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("★ [WildFrontier] 1챕터 아이템(압축 체인), 룬, 제작 도안, 몬스터(뿔토끼/덕구/수호목 엔트) 데이터 에셋 생성이 완료되었습니다!");
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static ItemDataSO CreateItem(string id, string name, string desc, ItemCategory cat, ItemGrade grade, int price,
            bool canCompress = false, int compressCount = 5, int compressGold = 50, ItemDataSO compressedResult = null)
        {
            string assetPath = $"{BasePath}/Items/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ItemDataSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDataSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.itemId = id;
            asset.itemName = name;
            asset.description = desc;
            asset.category = cat;
            asset.grade = grade;
            asset.sellPrice = price;
            asset.canCompress = canCompress;
            asset.compressRequiredCount = compressCount;
            asset.compressGoldCost = compressGold;
            asset.compressedResultItem = compressedResult;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ItemDataSO CreateEquipment(string id, string name, CharacterClass reqClass, ItemGrade grade, int atk, int def, int hp, float evasion, int slots)
        {
            var asset = CreateItem(id, name, $"{name} 장비", ItemCategory.Equipment, grade, 300);
            asset.requiredClass = reqClass;
            asset.bonusAttack = atk;
            asset.bonusDefense = def;
            asset.bonusMaxHp = hp;
            asset.bonusEvasionRate = evasion;
            asset.runeSlotCount = slots;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ItemDataSO CreateRecipeBook(string id, string name, CraftingRecipeSO recipe, ItemGrade grade)
        {
            var asset = CreateItem(id, name, $"몬스터 갈무리로 획득하는 비전 도안. 획득 시 [{recipe.recipeName}]을 해금한다.", ItemCategory.RecipeBook, grade, 100);
            asset.unlockedRecipe = recipe;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RuneDataSO CreateRune(string id, string name, ElementType element, ItemGrade grade, float procChance,
            float burnRatio = 0.1f, float lightningReduction = 0.3f, float thornsMult = 1.0f, float iceSlow = 0.35f, float lifeSteal = 0.08f)
        {
            string assetPath = $"{BasePath}/Runes/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<RuneDataSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RuneDataSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.itemId = id;
            asset.itemName = name;
            asset.category = ItemCategory.Rune;
            asset.grade = grade;
            asset.runeElement = element;
            asset.procChance = procChance;
            asset.burnDamageRatio = burnRatio;
            asset.lightningStatReductionRatio = lightningReduction;
            asset.thornsReflectMultiplier = thornsMult;
            asset.iceSlowRatio = iceSlow;
            asset.lifeStealRatio = lifeSteal;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CraftingRecipeSO CreateRecipe(string id, string name, RecipeType type, ItemDataSO result, int goldCost,
            ItemDataSO bonusCritResult = null, float critRate = 0.1f, float successRate = 1.0f, params RecipeIngredient[] ingredients)
        {
            string assetPath = $"{BasePath}/Recipes/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CraftingRecipeSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CraftingRecipeSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.recipeId = id;
            asset.recipeName = name;
            asset.recipeType = type;
            asset.resultItem = result;
            asset.resultCount = 1;
            asset.goldCost = goldCost;
            asset.baseSuccessRate = successRate;
            asset.bonusCriticalResultItem = bonusCritResult;
            asset.criticalSuccessRate = critRate;
            asset.ingredients = new List<RecipeIngredient>(ingredients);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static MonsterDataSO CreateMonster(string id, string name, MonsterSpecies species, MonsterSize size, int lv, int hp, int atk, int def,
            ElementType weakness, bool nocturnal, float carveTime, bool canTame, ItemDataSO bait, string tameSkill, params LootDropEntry[] loots)
        {
            string assetPath = $"{BasePath}/Monsters/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MonsterDataSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.monsterId = id;
            asset.monsterName = name;
            asset.species = species;
            asset.size = size;
            asset.level = lv;
            asset.maxHp = hp;
            asset.attackPower = atk;
            asset.defense = def;
            asset.weaknessElement = weakness;
            asset.isNocturnal = nocturnal;
            asset.carveHoldTime = carveTime;
            asset.canBeTamed = canTame;
            asset.favoriteBaitItem = bait;
            asset.awakenedPetSkillDescription = tameSkill;
            asset.carvingLootTable = new List<LootDropEntry>(loots);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
#endif
