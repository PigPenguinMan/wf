using UnityEngine;

namespace WildFrontier.Core
{
    /// <summary>
    /// 32x32 픽셀아트 탑다운 뷰에서 플레이어를 부드럽게 추적하는 카메라 스크립트
    /// </summary>
    public class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPos = target.position + offset;
            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

            // 32 PPU 픽셀 스내핑 보조 (서브픽셀 떨림 완화)
            float ppu = 32f;
            smoothed.x = Mathf.Round(smoothed.x * ppu) / ppu;
            smoothed.y = Mathf.Round(smoothed.y * ppu) / ppu;

            transform.position = smoothed;
        }
    }
}
