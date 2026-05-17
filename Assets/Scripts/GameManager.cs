using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------------
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ----------------------------------------------------------------
    // Inspector references
    // ----------------------------------------------------------------
    [SerializeField] private Transform playerTransform;
    [SerializeField] private TextMeshProUGUI gameInfoText;

    // ----------------------------------------------------------------
    // Internal state
    // ----------------------------------------------------------------
    private float _startTime;
    private bool _isGameOver = false;
    private int _currentPhase = 0;
    private float _nextGustTime;

    // ----------------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------------
    void Start()
    {
        _startTime = Time.time;
        _nextGustTime = _startTime + 30f;

        // PlayerDeathTrigger を playerTransform の GameObject に追加
        if (playerTransform != null)
        {
            PlayerDeathTrigger trigger = playerTransform.gameObject.GetComponent<PlayerDeathTrigger>();
            if (trigger == null)
            {
                trigger = playerTransform.gameObject.AddComponent<PlayerDeathTrigger>();
            }
            trigger.Init(this);
        }
    }

    void Update()
    {
        if (_isGameOver) return;

        // 敗北条件1: プレイヤーがステージ外（y < -10）に落下した
        if (playerTransform != null && playerTransform.position.y < -10f)
        {
            TriggerGameOver();
            return;
        }

        float elapsed = Time.time - _startTime;

        // 経過時間によるフェーズ判定と WindManager への反映
        int newPhase;
        float newSpeed;
        if (elapsed < 30f)
        {
            newPhase = 1;
            newSpeed = 3.0f;
        }
        else if (elapsed < 60f)
        {
            newPhase = 2;
            newSpeed = 5.0f;
        }
        else if (elapsed < 90f)
        {
            newPhase = 3;
            newSpeed = 8.0f;
        }
        else
        {
            newPhase = 4;
            newSpeed = 12.0f;
        }

        if (newPhase != _currentPhase)
        {
            _currentPhase = newPhase;
            if (WindManager.Instance != null)
            {
                WindManager.Instance.BaseWindSpeed = newSpeed;
            }
        }

        // 30秒ごとの突風イベント
        if (Time.time >= _nextGustTime)
        {
            WindManager.Instance?.TriggerGust(20f, 3f);
            _nextGustTime += 30f;
        }

        // 生存時間をフェーズ付きで毎フレーム表示
        if (gameInfoText != null)
        {
            gameInfoText.text = string.Format("Lv.{0} | Time: {1:F1}s", _currentPhase, elapsed);
        }
    }

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------
    public void TriggerGameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        float survived = Time.time - _startTime;
        if (gameInfoText != null)
        {
            gameInfoText.text = string.Format("GAME OVER\n{0:F1}s survived", survived);
        }

        StartCoroutine(RestartAfterDelay(2f));
    }

    // ----------------------------------------------------------------
    // Coroutines
    // ----------------------------------------------------------------
    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

// ----------------------------------------------------------------
// Helper component — 同ファイル内の別クラス
// ----------------------------------------------------------------
public class PlayerDeathTrigger : MonoBehaviour
{
    private GameManager _manager;

    public void Init(GameManager manager)
    {
        _manager = manager;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            _manager?.TriggerGameOver();
        }
    }
}
