using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShadowFollower : MonoBehaviour
{
    public Transform target;                 // 따라갈 캐릭터 (Transform)
    public LayerMask groundLayer;            // 바닥 레이어
    public float maxRayDistance = 5f;        // 캐릭터 아래로 Ray 쏘는 거리
    public float followSmoothness = 10f;     // 그림자 위치 부드럽게 따라가기 속도
    public float yOffset = 0.0f;             // 지면보다 살짝 띄우기

    [Header("그림자 효과")]
    public float maxJumpHeight = 2f;         // 최대 점프 높이 (이걸 기준으로 투명도/크기 조절)
    public float minScale = 0.5f;            // 가장 높이 점프했을 때 그림자 크기
    public float minAlpha = 0.3f;            // 가장 높이 점프했을 때 그림자 투명도

    [Header("그림자 기본 설정")]
    public float baseScale = 1f;             // 그림자의 기본 크기 (바닥에 있을 때의 크기)

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (target == null)
        {
            Debug.LogWarning("ShadowFollower: 타겟을 지정해 주세요!");
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Raycast로 지면 감지
        Vector2 rayOrigin = target.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, maxRayDistance, groundLayer);

        if (hit.collider != null)
        {
            // 목표 위치 계산 (지면 위치 + 약간의 Y 오프셋)
            Vector3 targetPos = new Vector3(target.position.x, hit.point.y + yOffset, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothness);

            // 점프 높이에 따라 투명도 & 크기 조절
            float height = Mathf.Clamp(target.position.y - hit.point.y, 0f, maxJumpHeight);
            float t = height / maxJumpHeight;

            float alpha = Mathf.Lerp(1f, minAlpha, t);
            // baseScale을 기본 크기로 사용하여, 점프 높이에 따라 minScale까지 보간
            float scale = Mathf.Lerp(baseScale, minScale, t);

            sr.color = new Color(0f, 0f, 0f, alpha);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
