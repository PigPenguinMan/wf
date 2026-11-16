using UnityEngine;

namespace WildFrontier.Core
{
    /// <summary>
    /// 모바일 '스마트 컨텍스트 액션 버튼'과 연동되는 상호작용 인터페이스.
    /// (몬스터 시체 갈무리, 채집물, 모닥불 캠프 등이 구현)
    /// </summary>
    public interface IHoldInteractable
    {
        /// <summary>
        /// 스마트 액션 버튼에 표시할 컨텍스트 종류 (갈무리, 채집, 캠프 등)
        /// </summary>
        ActionContextType ContextType { get; }

        /// <summary>
        /// 상호작용이 현재 가능한 상태인지 여부
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// 홀드 완료까지 걸리는 시간 (초). 예: 일반 시체 갈무리 1.2초, 대형 보스 갈무리 2.0초
        /// </summary>
        float RequiredHoldDuration { get; }

        /// <summary>
        /// 상호작용 대상의 월드 위치
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// 버튼을 누르기 시작할 때 호출
        /// </summary>
        void OnInteractStart(GameObject interactor);

        /// <summary>
        /// 버튼을 누르고 있는 동안 매 프레임 호출 (진행률 0.0 ~ 1.0)
        /// </summary>
        void OnInteractProgress(float normalizedProgress);

        /// <summary>
        /// 게이지가 100% 차올라 갈무리/채집에 성공했을 때 호출
        /// </summary>
        void OnInteractComplete(GameObject interactor);

        /// <summary>
        /// 도중에 버튼을 떼거나 피격당해 취소되었을 때 호출
        /// </summary>
        void OnInteractCancel();
    }
}
