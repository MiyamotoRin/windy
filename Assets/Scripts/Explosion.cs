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

    [Header("Highlight UI")]
    [SerializeField] private float highlightSize = 120f;
    [SerializeField] private float highlightDuration = 0.35f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.4f, 0.1f, 0.45f);

    private Canvas overlayCanvas;
    private RawImage highlightImage;
    private Coroutine highlightCoroutine;
    private readonly HashSet<Rigidbody> rigidbodyCache = new HashSet<Rigidbody>();

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
            Vector2 clickPosition = Input.mousePosition;
            ShowHighlight(clickPosition);

            if (TryGetExplosionCenter(clickPosition, out Vector3 center))
            {
                ApplyExplosion(center);
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

    private void ApplyExplosion(Vector3 center)
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

            rb.AddExplosionForce(explosionForce, center, explosionRadius, upwardsModifier, ForceMode.Impulse);
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

        if (highlightImage != null)
        {
            return;
        }

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
