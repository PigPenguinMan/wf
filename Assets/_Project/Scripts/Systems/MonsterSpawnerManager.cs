using System;
using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Data;
using WildFrontier.Monster;
using WildFrontier.UI;

namespace WildFrontier.Systems
{
    /// <summary>
    /// [몬스터 서식지 자동 리스폰 매니저]
    /// 필드의 몬스터가 사냥·갈무리되거나 펫으로 길들여진 뒤,
    /// 서식지별 대기 시간(일반 10초, 준보스 18초, 보스 30초)이 지나면 자동으로 다시 스폰시킵니다.
    /// </summary>
    public class MonsterSpawnerManager : MonoBehaviour
    {
        public static MonsterSpawnerManager Instance { get; private set; }

        [Serializable]
        public class SpawnSlot
        {
            public string slotName;
            public Vector3 spawnPosition;
            public Sprite monsterSprite;
            public MonsterDataSO monsterData;
            public float colliderRadius = 0.45f;
            public float uiHeightOffset = 0.8f;
            public float respawnDelaySeconds = 10f;
            public MonsterBase activeInstance;
            public float timer;
        }

        [SerializeField] private List<SpawnSlot> spawnSlots = new List<SpawnSlot>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterSpawnSlot(
            string slotName,
            Vector3 pos,
            Sprite sprite,
            MonsterDataSO data,
            float radius,
            float uiHeightOffset,
            float respawnDelay,
            MonsterBase existingInstance)
        {
            spawnSlots.Add(new SpawnSlot
            {
                slotName = slotName,
                spawnPosition = pos,
                monsterSprite = sprite,
                monsterData = data,
                colliderRadius = radius,
                uiHeightOffset = uiHeightOffset,
                respawnDelaySeconds = respawnDelay,
                activeInstance = existingInstance,
                timer = respawnDelay
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < spawnSlots.Count; i++)
            {
                SpawnSlot slot = spawnSlots[i];
                if (slot.activeInstance != null || slot.monsterData == null) continue;

                slot.timer -= dt;
                if (slot.timer <= 0f)
                {
                    slot.timer = slot.respawnDelaySeconds;
                    SpawnSlotMonster(slot);
                }
            }
        }

        private void SpawnSlotMonster(SpawnSlot slot)
        {
            GameObject go = new GameObject(slot.slotName);
            go.transform.position = slot.spawnPosition;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = slot.monsterSprite;
            sr.sortingOrder = 8;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = slot.colliderRadius;

            MonsterCorpse corpse = go.AddComponent<MonsterCorpse>();
            MonsterBase monster = go.AddComponent<MonsterBase>();
            monster.ConfigureMonster(slot.monsterData, sr, corpse);

            MonsterWorldUI worldUi = go.AddComponent<MonsterWorldUI>();
            worldUi.InitializeWorldUI(monster, slot.uiHeightOffset);

            slot.activeInstance = monster;

            FloatingPopupManager.SpawnPopup(
                slot.spawnPosition + Vector3.up * 0.9f,
                $"🌿 [{slot.monsterData.monsterName}] 서식지 출현!",
                new Color(0.60f, 1.0f, 0.55f),
                19,
                1.4f);
        }
    }
}
