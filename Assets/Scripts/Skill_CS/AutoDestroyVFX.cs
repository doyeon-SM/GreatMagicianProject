using UnityEngine;
using UnityEngine.VFX;

public class AutoDestroyVFX : MonoBehaviour
{
    [Tooltip("파티클/VFX에서 지속 시간을 알 수 없을 때의 최대 생존 시간(초)")]
    public float fallbackLifetime = 5f;

    void Start()
    {
        float ttl = fallbackLifetime;

        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            // 대충 '재생시간 + (최대) 수명' 정도를 합산
            float startLifetime =
                main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : main.startLifetime.constant;
            ttl = Mathf.Max(ttl, main.duration + startLifetime + 0.25f);
        }
        
        var vfx = GetComponent<VisualEffect>();
        if (vfx != null)
        {
            // 예: VFX Graph에서 'Lifetime'라는 float 프로퍼티를 노출했다면
            if (vfx.HasFloat("Lifetime"))
                ttl = Mathf.Max(ttl, vfx.GetFloat("Lifetime") + 0.25f);
        }

        Destroy(gameObject, ttl);
    }
}
