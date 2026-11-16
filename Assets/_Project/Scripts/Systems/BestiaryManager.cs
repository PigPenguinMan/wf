using System;
using System.Collections.Generic;
using UnityEngine;
using WildFrontier.Data;
using WildFrontier.Player;
using WildFrontier.UI;

namespace WildFrontier.Systems
{
    /// <summary>
    /// [생태 도감 (Bestiary) & 영구 컬렉션 보너스 매니저]
    /// 몬스터별 토벌(Kill), 갈무리(Carve), 펫 유혹(Tame) 달성도를 기록하고
    /// 연구 등급(Lv.1~3)에 따라 공격력·최대 HP·갈무리 행운 영구 보너스를 제공합니다.
    /// </summary>
    public class BestiaryManager : MonoBehaviour
    {
        public static BestiaryManager Instance { get; private set; }

        [Serializable]
        public class BestiaryEntry
        {
            public string monsterId;
            public string monsterName;
            public int kills;
            public int carves;
            public bool tamed;
            public int masteryLevel; // 0 ~ 3
        }

        [SerializeField] private List<BestiaryEntry> entries = new List<BestiaryEntry>();
        private readonly Dictionary<string, BestiaryEntry> _entryMap = new Dictionary<string, BestiaryEntry>();
        private Transform _playerTransform;

        public int TotalMasteryLevels
        {
            get
            {
                int sum = 0;
                foreach (var e in entries)
                    sum += e.masteryLevel;
                return sum;
            }
        }

        public int TotalBonusAttack => TotalMasteryLevels * 3;
        public int TotalBonusMaxHp => TotalMasteryLevels * 15;
        public float TotalBonusCarveLuck => TotalMasteryLevels * 0.02f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _entryMap.Clear();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.monsterId))
                    _entryMap[e.monsterId] = e;
            }
        }

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerCombat>();
            if (player != null)
                _playerTransform = player.transform;
        }

        public void RecordKill(MonsterDataSO data)
        {
            if (data == null || string.IsNullOrEmpty(data.monsterId)) return;
            var entry = GetOrCreateEntry(data);
            entry.kills++;
            EvaluateMastery(data, entry);
        }

        public void RecordCarve(MonsterDataSO data)
        {
            if (data == null || string.IsNullOrEmpty(data.monsterId)) return;
            var entry = GetOrCreateEntry(data);
            entry.carves++;
            EvaluateMastery(data, entry);
        }

        public void RecordTame(MonsterDataSO data)
        {
            if (data == null || string.IsNullOrEmpty(data.monsterId)) return;
            var entry = GetOrCreateEntry(data);
            entry.tamed = true;
            EvaluateMastery(data, entry);
        }

        private BestiaryEntry GetOrCreateEntry(MonsterDataSO data)
        {
            if (!_entryMap.TryGetValue(data.monsterId, out var entry))
            {
                entry = new BestiaryEntry
                {
                    monsterId = data.monsterId,
                    monsterName = data.monsterName
                };
                _entryMap[data.monsterId] = entry;
                entries.Add(entry);
            }
            return entry;
        }

        private void EvaluateMastery(MonsterDataSO data, BestiaryEntry entry)
        {
            int targetLevel = 0;
            if (entry.carves >= 5 || entry.tamed)
                targetLevel = 3;
            else if (entry.carves >= 2)
                targetLevel = 2;
            else if (entry.kills >= 1)
                targetLevel = 1;

            if (targetLevel > entry.masteryLevel)
            {
                entry.masteryLevel = targetLevel;
                Vector3 popupPos = _playerTransform != null
                    ? _playerTransform.position + Vector3.up * 1.45f
                    : new Vector3(0f, 1.45f, 0f);

                FloatingPopupManager.SpawnPopup(
                    popupPos,
                    $"📖 [생태 도감 Lv.{entry.masteryLevel} 달성!] {data.monsterName} (공격력+3, 갈무리 행운+2%)",
                    new Color(0.45f, 0.90f, 1.0f),
                    22,
                    2.2f);
            }
        }
    }
}
