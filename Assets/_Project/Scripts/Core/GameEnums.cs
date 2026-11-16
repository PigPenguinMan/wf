using System;

namespace WildFrontier.Core
{
    /// <summary>
    /// 와일드 프론티어 3대 클래스 정의
    /// </summary>
    public enum CharacterClass
    {
        None = 0,       // 프롤로그 (초보 헌터)
        Warrior = 1,    // 워리어: 대검/양손무기, 차지(Charge) 및 돌파 한방 딜러
        Scouter = 2,    // 스카우터: 쌍단검, 고속 연타·치명타·회피 및 총기 하이브리드
        Guardian = 3    // 가디언(탱커): 한손무기+방패, 저스트 가드·가시 반사(반탱)·실드 스핀
    }

    /// <summary>
    /// 몬스터 생태계 분류
    /// </summary>
    public enum MonsterSpecies
    {
        Plant,      // 식물계
        Beast,      // 야수계
        Insect,     // 갑충계
        Avian,      // 맹금류
        Spirit,     // 정령계
        Dragon      // 고대종/드래곤
    }

    /// <summary>
    /// 몬스터 크기 등급 (소형 S는 펫 테이밍 가능, 대형 L/보스 B는 부위 파괴 가능)
    /// </summary>
    public enum MonsterSize
    {
        Small,      // S (32x32) - 펫 유혹 가능
        Medium,     // M (48x48 ~ 64x64)
        Large,      // L (96x96 ~ 128x128) - 대형 몬스터
        Boss        // B (128x128 ~ 256x256) - 필드/지역 우두머리
    }

    /// <summary>
    /// 아이템 및 룬 5단계 등급 (일반 -> 고급 -> 희귀 -> 영웅 -> 전설)
    /// </summary>
    public enum ItemGrade
    {
        Common = 0,     // 일반 (하얀색)
        Uncommon = 1,   // 고급 (초록색)
        Rare = 2,       // 희귀 (파란색)
        Epic = 3,       // 영웅 (보라색)
        Legendary = 4   // 전설 (주황/황금색)
    }

    /// <summary>
    /// 아이템 카테고리
    /// </summary>
    public enum ItemCategory
    {
        Material,       // 갈무리/채집 재료 (압축 및 변이 가능)
        Equipment,      // 무기, 방어구, 장신구
        Rune,           // 장비 장착용 룬
        RecipeBook,     // 장비/요리 제작법 도안 (갈무리로 획득)
        Consumable,     // 요리, 물약, 테이밍 먹이
        KeyItem         // 퀘스트 및 핵심 아이템
    }

    /// <summary>
    /// 룬 및 공격 속성 타입
    /// </summary>
    public enum ElementType
    {
        Physical,   // 물리 (무속성)
        Fire,       // 화염: 입힌 피해 비례 화상 도트 대미지
        Lightning,  // 전격: 감전 및 적 공격력/방어력 디버프 (보스 공략 필수)
        Ice,        // 냉기: 이동속도 및 공격속도 둔화
        Vampire,    // 흡혈/흡기: 타격 시 HP/MP 흡수
        Thorns      // 가시/심판: 피격 또는 회피 시 반사 피해 (반사 탱커 핵심)
    }

    /// <summary>
    /// 스마트 액션 버튼의 컨텍스트 상태
    /// </summary>
    public enum ActionContextType
    {
        Attack,     // 기본 공격 및 차지 공격 (⚔️)
        Carve,      // 몬스터 시체 갈무리 (🔪)
        Gather,     // 약초/광석 채집 (🌿)
        Tame,       // 소형 몬스터 유혹 (🐾)
        Campfire    // 모닥불 캠프 요리/제작 및 NPC 대화 (🔥)
    }

    /// <summary>
    /// 시간대 및 기후 (몬스터 스탯 및 속성 위력에 영향)
    /// </summary>
    public enum TimeOfDay
    {
        Dawn,
        Day,
        Dusk,
        Night       // 야간: 야행성 몬스터 공격력/이속 증가, 희귀 채집물 등장
    }

    public enum WeatherType
    {
        Clear,
        Rain,       // 우천: 화염 위력 감소, 전격 범위 증가
        Sandstorm,  // 모래폭풍: 원거리 명중 감소
        Snow        // 폭설: 캠프 요리로 한기 저항 필요
    }
}
