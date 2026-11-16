using UnityEngine;
using UnityEngine.EventSystems;
using WildFrontier.Player;

namespace WildFrontier.UI
{
    /// <summary>
    /// 모바일 화면 좌측 하단의 가상 조이스틱(Virtual Joystick) 컨트롤러입니다.
    /// 마우스 드래그 또는 모바일 터치로 조이스틱 핸들을 움직여 8방향 이동을 수행합니다.
    /// </summary>
    public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("참조 설정")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private float maxRadius = 55f;

        public void ConfigureJoystick(PlayerController controller, RectTransform bg, RectTransform handle, float radius = 55f)
        {
            playerController = controller;
            backgroundRect = bg;
            handleRect = handle;
            maxRadius = radius;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (backgroundRect == null || handleRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    backgroundRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, maxRadius);
                handleRect.anchoredPosition = clamped;

                Vector2 normalizedInput = clamped / Mathf.Max(1f, maxRadius);
                if (playerController != null)
                {
                    playerController.SetVirtualJoystickInput(normalizedInput);
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;

            if (playerController != null)
                playerController.SetVirtualJoystickInput(Vector2.zero);
        }
    }
}
