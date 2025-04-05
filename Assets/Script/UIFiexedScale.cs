using UnityEngine;

public class UIFixedXScale : MonoBehaviour
{
    private float baseX;

    void Start()
    {
        // 처음 UI의 로컬 x 스케일의 절대값을 기본값으로 저장 (항상 양수여야 함)
        baseX = Mathf.Abs(transform.localScale.x);
    }

    void LateUpdate()
    {
        // 부모(캐릭터)의 x 스케일을 가져옴 (부모가 없으면 1)
        float parentX = transform.parent ? transform.parent.lossyScale.x : 1f;

        // 부모가 음수면, 자식의 로컬 x 스케일을 음수로 하여,
        // 부모의 음수와 곱해졌을 때 효과적으로 양수가 되도록 함.
        float desiredLocalX = (parentX < 0) ? -baseX : baseX;

        Vector3 scale = transform.localScale;
        scale.x = desiredLocalX;
        transform.localScale = scale;
    }
}
