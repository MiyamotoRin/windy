using UnityEngine;

public class WindVisualizer : MonoBehaviour
{
    [SerializeField] private float emissionRate = 50f;

    private ParticleSystem _ps;

    void Start()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();

        // main モジュール設定
        var main = _ps.main;
        main.startSize = 0.05f;
        main.startLifetime = 2.0f;
        main.startSpeed = 0f; // 速度は velocityOverLifetime で制御
        main.startColor = new Color(1f, 1f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // emission モジュール設定
        var emission = _ps.emission;
        emission.rateOverTime = emissionRate;

        // shape モジュール設定（Box、広域に散らばる）
        var shape = _ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 1f, 20f);

        // velocityOverLifetime モジュール設定（初期値）
        var vel = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = 0f;
        vel.y = 0f;
        vel.z = 0f;

        // Renderer を非表示にしない（デフォルトで表示）
        var renderer = _ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }

    void Update()
    {
        if (_ps == null) return;
        if (WindManager.Instance == null) return;

        Vector3 wind = WindManager.Instance.CurrentWind;

        var vel = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = wind.x;
        vel.y = wind.y;
        vel.z = wind.z;
    }
}
