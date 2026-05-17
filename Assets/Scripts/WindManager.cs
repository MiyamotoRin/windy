using UnityEngine;

public class WindManager : MonoBehaviour
{
    public static WindManager Instance { get; private set; }
    public Vector3 CurrentWind { get; private set; }

    [SerializeField] private float baseWindSpeed = 3.0f;
    [SerializeField] private float noiseTimeScale = 0.03f;

    public float BaseWindSpeed
    {
        get => baseWindSpeed;
        set => baseWindSpeed = Mathf.Max(0f, value);
    }

    private Rigidbody[] _rigidbodies;
    private bool _gustActive = false;
    private float _gustSpeed = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        Rigidbody[] all = FindObjectsOfType<Rigidbody>();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].isKinematic)
            {
                count++;
            }
        }

        _rigidbodies = new Rigidbody[count];
        int index = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].isKinematic)
            {
                _rigidbodies[index++] = all[i];
            }
        }
    }

    void FixedUpdate()
    {
        float noiseX = Mathf.PerlinNoise(Time.time * noiseTimeScale, 0f);
        float noiseZ = Mathf.PerlinNoise(0f, Time.time * noiseTimeScale);

        float windX = noiseX * 2f - 1f;
        float windZ = noiseZ * 2f - 1f;

        Vector3 windDir = new Vector3(windX, 0f, windZ);
        if (windDir.sqrMagnitude < 0.0001f)
        {
            windDir = Vector3.right;
        }

        CurrentWind = windDir.normalized * (_gustActive ? _gustSpeed : baseWindSpeed);

        if (_rigidbodies == null)
        {
            return;
        }

        for (int i = 0; i < _rigidbodies.Length; i++)
        {
            Rigidbody rb = _rigidbodies[i];
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            rb.AddForce(CurrentWind, ForceMode.Force);
        }
    }

    public void TriggerGust(float gustSpeed, float duration)
    {
        if (_gustActive) return;
        _gustSpeed = gustSpeed;
        StartCoroutine(GustCoroutine(gustSpeed, duration));
    }

    private System.Collections.IEnumerator GustCoroutine(float gustSpeed, float duration)
    {
        _gustActive = true;
        yield return new WaitForSeconds(duration);
        _gustActive = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
