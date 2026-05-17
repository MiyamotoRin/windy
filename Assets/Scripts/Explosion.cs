using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Explosion : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private float upwardsModifier = 0.3f;
    [SerializeField] private float maxRayDistance = 300f;
    [SerializeField] private LayerMask affectedLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Charge")]
    [SerializeField] private float maxChargeTime = 1.5f;

    [Header("Highlight UI")]
    [SerializeField] private float highlightSize = 120f;
    [SerializeField] private float highlightDuration = 0.35f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.4f, 0.1f, 0.45f);

    private Canvas overlayCanvas;
    private RawImage highlightImage;
    private RawImage chargeImage;
    private LineRenderer _arrowLine;
    private Coroutine highlightCoroutine;
    private readonly HashSet<Rigidbody> rigidbodyCache = new HashSet<Rigidbody>();

    // Charge state
    private bool _isCharging = false;
    private float _chargeStartTime = 0f;
    private float _chargeRatio = 0f;
    private Vector2 _pressScreenPosition;

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        EnsureHighlightUi();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
            _pressScreenPosition = Input.mousePosition;
            _chargeRatio = 0f;

            // 蓄積インジケータを押下位置に配置して表示開始
            if (chargeImage != null)
            {
                chargeImage.rectTransform.anchoredPosition = _pressScreenPosition;
                chargeImage.gameObject.SetActive(true);
            }
        }

        if (_isCharging && Input.GetMouseButton(0))
        {
            float elapsed = Mathf.Clamp(Time.time - _chargeStartTime, 0f, maxChargeTime);
            _chargeRatio = elapsed / maxChargeTime;

            // 蓄積インジケータのスケールを 0.5x → 1.5x で更新
            if (chargeImage != null)
            {
                float chargeScale = Mathf.Lerp(0.5f, 1.5f, _chargeRatio);
                chargeImage.rectTransform.localScale = new Vector3(chargeScale, chargeScale, 1f);

                Color c = highlightColor;
                c.a = Mathf.Lerp(0.15f, 0.55f, _chargeRatio);
                chargeImage.color = c;
            }

            // 方向矢印 LineRenderer を更新
            UpdateArrowLine();
        }

        if (Input.GetMouseButtonUp(0) && _isCharging)
        {
            _isCharging = false;

            // 蓄積インジケータを非表示
            if (chargeImage != null)
            {
                chargeImage.gameObject.SetActive(false);
            }

            // 方向矢印を非表示
            if (_arrowLine != null)
            {
                _arrowLine.enabled = false;
            }

            Vector2 releasePosition = _pressScreenPosition;
            ShowHighlight(releasePosition);

            // ドラッグ差分ベクトルを計算してワールド方向に変換
            Vector2 dragDelta = (Vector2)Input.mousePosition - _pressScreenPosition;
            Vector3 windDir = Vector3.zero;
            bool hasDrag = dragDelta.magnitude >= 10f;
            if (hasDrag)
            {
                Vector3 camRight = targetCamera.transform.right;
                Vector3 camUp = targetCamera.transform.up;
                Vector2 normalized = dragDelta.normalized;
                windDir = (camRight * normalized.x + camUp * normalized.y).normalized;
            }

            if (TryGetExplosionCenter(releasePosition, out Vector3 center))
            {
                float scaledForce = explosionForce * Mathf.Lerp(0.5f, 2.0f, _chargeRatio);
                ApplyExplosion(center, scaledForce, hasDrag ? windDir : Vector3.zero, scaledForce);
            }
        }
    }

    private bool TryGetExplosionCenter(Vector2 screenPosition, out Vector3 center)
    {
        center = Vector3.zero;

        if (targetCamera == null)
        {
            return false;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, affectedLayers, triggerInteraction))
        {
            center = hit.point;
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            center = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private void ApplyExplosion(Vector3 center, float force)
    {
        ApplyExplosion(center, force, Vector3.zero, 0f);
    }

    private void ApplyExplosion(Vector3 center, float force, Vector3 windDir, float scaledForce)
    {
        Collider[] colliders = Physics.OverlapSphere(center, explosionRadius, affectedLayers, triggerInteraction);
        rigidbodyCache.Clear();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null)
            {
                continue;
            }

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null && !col.gameObject.isStatic)
            {
                rb = col.gameObject.AddComponent<Rigidbody>();
            }

            if (rb == null || !rigidbodyCache.Add(rb))
            {
                continue;
            }

            rb.AddExplosionForce(force, center, explosionRadius, upwardsModifier, ForceMode.Impulse);

            // 指向性風圧：ドラッグ方向が有効な場合に追加で適用
            if (windDir != Vector3.zero)
            {
                rb.AddForce(windDir * scaledForce * 0.5f, ForceMode.Impulse);
            }
        }
    }

    private void ShowHighlight(Vector2 screenPosition)
    {
        if (highlightImage == null)
        {
            return;
        }

        RectTransform rt = highlightImage.rectTransform;
        rt.anchoredPosition = screenPosition;

        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }

        highlightCoroutine = StartCoroutine(AnimateHighlight());
    }

    private IEnumerator AnimateHighlight()
    {
        RectTransform rt = highlightImage.rectTransform;
        highlightImage.gameObject.SetActive(true);

        float elapsed = 0f;
        float startScale = 0.7f;
        float endScale = 1.3f;

        while (elapsed < highlightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / highlightDuration);

            float scale = Mathf.Lerp(startScale, endScale, t);
            rt.localScale = new Vector3(scale, scale, 1f);

            Color c = highlightColor;
            c.a = Mathf.Lerp(highlightColor.a, 0f, t);
            highlightImage.color = c;

            yield return null;
        }

        highlightImage.gameObject.SetActive(false);
        highlightCoroutine = null;
    }

    private void EnsureHighlightUi()
    {
        if (overlayCanvas == null)
        {
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                overlayCanvas = existing;
            }
            else
            {
                GameObject canvasObj = new GameObject("ExplosionHighlightCanvas");
                overlayCanvas = canvasObj.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 1000;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        if (highlightImage == null)
        {
            GameObject imageObj = new GameObject("ClickHighlight");
            imageObj.transform.SetParent(overlayCanvas.transform, false);
            highlightImage = imageObj.AddComponent<RawImage>();
            highlightImage.raycastTarget = false;
            highlightImage.texture = CreateCircleTexture(128);
            highlightImage.color = highlightColor;

            RectTransform rt = highlightImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(highlightSize, highlightSize);

            highlightImage.gameObject.SetActive(false);
        }

        if (chargeImage == null)
        {
            GameObject chargeObj = new GameObject("ChargeIndicator");
            chargeObj.transform.SetParent(overlayCanvas.transform, false);
            chargeImage = chargeObj.AddComponent<RawImage>();
            chargeImage.raycastTarget = false;
            chargeImage.texture = CreateCircleTexture(128);
            chargeImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.15f);

            RectTransform crt = chargeImage.rectTransform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.zero;
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(highlightSize, highlightSize);
            crt.localScale = new Vector3(0.5f, 0.5f, 1f);

            chargeImage.gameObject.SetActive(false);
        }

        if (_arrowLine == null)
        {
            _arrowLine = gameObject.AddComponent<LineRenderer>();
            _arrowLine.useWorldSpace = true;
            _arrowLine.positionCount = 2;
            _arrowLine.startWidth = 0.05f;
            _arrowLine.endWidth = 0.05f;
            _arrowLine.material = new Material(Shader.Find("Sprites/Default"));
            _arrowLine.startColor = new Color(1f, 0.5f, 0.1f, 0.8f);
            _arrowLine.endColor = new Color(1f, 0.5f, 0.1f, 0.8f);
            _arrowLine.enabled = false;
        }
    }

    private void UpdateArrowLine()
    {
        if (_arrowLine == null || targetCamera == null)
        {
            return;
        }

        Vector2 dragDelta = (Vector2)Input.mousePosition - _pressScreenPosition;

        if (dragDelta.magnitude < 10f)
        {
            _arrowLine.enabled = false;
            return;
        }

        float depth = 5f;
        Vector3 startWorld = targetCamera.ScreenToWorldPoint(
            new Vector3(_pressScreenPosition.x, _pressScreenPosition.y, depth));
        Vector3 endWorld = targetCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));

        _arrowLine.SetPosition(0, startWorld);
        _arrowLine.SetPosition(1, endWorld);
        _arrowLine.enabled = true;
    }

    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        float edgeSoftness = 2f;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01((distance - (radius - edgeSoftness)) / edgeSoftness);
                float alpha = 1f - (t * t * (3f - 2f * t));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
