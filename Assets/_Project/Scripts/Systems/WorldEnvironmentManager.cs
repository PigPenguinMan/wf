using System;
using UnityEngine;
using WildFrontier.Core;

namespace WildFrontier.Systems
{
    /// <summary>
    /// [와일드 프론티어 핵심 환경 시스템]
    /// 실시간 낮/밤(TimeOfDay) 및 기후(WeatherType) 변화를 관리하며,
    /// 야행성 몬스터 스탯 강화, 속성(화염/전격/냉기) 위력 보정, 모닥불 요리 버프 지속시간을 총괄합니다.
    /// </summary>
    public class WorldEnvironmentManager : MonoBehaviour
    {
        public static WorldEnvironmentManager Instance { get; private set; }

        [Header("시간대 및 기후 설정")]
        [SerializeField] private TimeOfDay currentTimeOfDay = TimeOfDay.Day;
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [Tooltip("현실 시간 기준 한 시간대(낮/황혼/밤/새벽)가 유지되는 시간(초)")]
        [SerializeField] private float phaseDurationSeconds = 90f;
        [SerializeField] private float weatherChangeInterval = 120f;

        [Header("화면/환경 색조 연출 (카메라 또는 글로벌 라이트 연동)")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Color dawnBgColor = new Color(0.38f, 0.62f, 0.36f);
        [SerializeField] private Color dayBgColor = new Color(0.48f, 0.84f, 0.29f);
        [SerializeField] private Color duskBgColor = new Color(0.42f, 0.56f, 0.30f);
        [SerializeField] private Color nightBgColor = new Color(0.16f, 0.24f, 0.32f);

        [Header("모닥불 요리 버프 상태")]
        [SerializeField] private float carveLuckBuffTimer;
        [SerializeField] private float bonusCarveDropRate = 0.15f; // 행운 포자 스프 섭취 시 갈무리 드랍률 +15%
        [SerializeField] private float attackBuffTimer;
        [SerializeField] private float climateResistanceTimer;

        private float _timePhaseTimer;
        private float _weatherTimer;

        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;
        public WeatherType CurrentWeather => currentWeather;
        public bool HasCarveLuckBuff => carveLuckBuffTimer > 0f;
        public float ActiveCarveLuckBonus => carveLuckBuffTimer > 0f ? bonusCarveDropRate : 0f;
        public float ActiveAttackBuffMultiplier => attackBuffTimer > 0f ? 1.10f : 1.0f;

        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<WeatherType> OnWeatherChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            ApplyEnvironmentVisuals();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. 낮/밤 사이클 진행
            _timePhaseTimer += dt;
            if (_timePhaseTimer >= phaseDurationSeconds)
            {
                _timePhaseTimer = 0f;
                AdvanceTimeOfDay();
            }

            // 2. 날씨 변화 진행
            _weatherTimer += dt;
            if (_weatherTimer >= weatherChangeInterval)
            {
                _weatherTimer = 0f;
                RollNextWeather();
            }

            // 3. 모닥불 요리 버프 타이머 갱신
            if (carveLuckBuffTimer > 0f) carveLuckBuffTimer -= dt;
            if (attackBuffTimer > 0f) attackBuffTimer -= dt;
            if (climateResistanceTimer > 0f) climateResistanceTimer -= dt;
        }

        public void AdvanceTimeOfDay()
        {
            currentTimeOfDay = (TimeOfDay)(((int)currentTimeOfDay + 1) % 4);
            ApplyEnvironmentVisuals();
            Debug.Log($"[월드 시간 변화] 현재 시간대: {currentTimeOfDay} {(currentTimeOfDay == TimeOfDay.Night ? "(⚠️ 야행성 몬스터의 공격력과 이동속도가 상승합니다!)" : "")}");
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        public void SetWeather(WeatherType nextWeather)
        {
            currentWeather = nextWeather;
            Debug.Log($"[월드 기후 변화] 현재 날씨: {currentWeather}");
            OnWeatherChanged?.Invoke(currentWeather);
        }

        private void RollNextWeather()
        {
            float r = UnityEngine.Random.value;
            if (r < 0.55f) SetWeather(WeatherType.Clear);
            else if (r < 0.80f) SetWeather(WeatherType.Rain);
            else if (r < 0.90f) SetWeather(WeatherType.Sandstorm);
            else SetWeather(WeatherType.Snow);
        }

        /// <summary>
        /// 현재 날씨에 따른 속성 대미지/효과 배율을 반환합니다.
        /// (예: 우천 시 전격 위력 +25%, 화염 위력 -20%)
        /// </summary>
        public float GetWeatherElementMultiplier(ElementType element)
        {
            if (currentWeather == WeatherType.Rain)
            {
                if (element == ElementType.Lightning) return 1.25f;
                if (element == ElementType.Fire) return 0.80f;
            }
            else if (currentWeather == WeatherType.Snow)
            {
                if (element == ElementType.Ice) return 1.20f;
            }
            return 1.0f;
        }

        /// <summary>
        /// 모닥불 캠프에서 요리를 섭취했을 때 버프를 부여합니다.
        /// </summary>
        public void ApplyCampfireMealBuff(float durationSeconds, bool grantCarveLuck, bool grantAttackBoost, bool grantClimateResist)
        {
            if (grantCarveLuck) carveLuckBuffTimer = Mathf.Max(carveLuckBuffTimer, durationSeconds);
            if (grantAttackBoost) attackBuffTimer = Mathf.Max(attackBuffTimer, durationSeconds);
            if (grantClimateResist) climateResistanceTimer = Mathf.Max(climateResistanceTimer, durationSeconds);

            Debug.Log($"[모닥불 요리 섭취] {durationSeconds:F0}초 동안 수렵 버프가 활성화되었습니다! (갈무리 행운+: {grantCarveLuck}, 공격력+: {grantAttackBoost})");
        }

        private void ApplyEnvironmentVisuals()
        {
            if (targetCamera == null) return;

            switch (currentTimeOfDay)
            {
                case TimeOfDay.Dawn:
                    targetCamera.backgroundColor = dawnBgColor;
                    break;
                case TimeOfDay.Day:
                    targetCamera.backgroundColor = dayBgColor;
                    break;
                case TimeOfDay.Dusk:
                    targetCamera.backgroundColor = duskBgColor;
                    break;
                case TimeOfDay.Night:
                    targetCamera.backgroundColor = nightBgColor;
                    break;
            }
        }
    }
}
