using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Manager responsible for spawning gold icons
public class GoldEffectManager : Singleton<GoldEffectManager>
{
    [Header("References")]
    public Canvas uiCanvas; // assign your UI canvas
    public GameObject goldIconPrefab; // prefab must contain Image + GoldIconEffect

    [Header("Spawn Settings")]
    public int spawnCount = 6; // how many icons to spawn per click
    public float spreadRadius = 120f; // how far icons will scatter (in canvas units)
    public float duration = 0.9f; // animation duration

    [Header("Pooling")]
    public bool usePooling = true;
    public int initialPoolSize = 20;

    [Header("Pool Organization")]
    public Transform poolParent; // optional parent under which pooled objects will be stored
    public bool moveToPoolParentOnRecycle = true; // if true, Recycle will reparent to poolParent

    private Queue<GameObject> _pool = new Queue<GameObject>();

    void Awake()
    {
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
        }

        // If a GameObject named "GoldEffects" exists under the canvas, use it as poolParent
        if (poolParent == null && uiCanvas != null)
        {
            var existing = uiCanvas.transform.Find("GoldEffects");
            if (existing != null)
                poolParent = existing;
        }

        // Ensure poolParent exists when pooling enabled
        if (usePooling && poolParent == null)
        {
            // Create a container under the canvas for organization so pooled objects stay in the same UI space
            GameObject container = new GameObject("GoldEffects", typeof(RectTransform));
            if (uiCanvas != null)
                container.transform.SetParent(uiCanvas.transform, false);
            else
                container.transform.SetParent(this.transform, false);
            poolParent = container.transform;
        }

        if (usePooling && goldIconPrefab != null && uiCanvas != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                // Instantiate directly under the pool parent so it never appears on the UI outside the pool
                Transform parentForNew = poolParent != null ? poolParent : uiCanvas.transform;
                GameObject go = Instantiate(goldIconPrefab, parentForNew, false);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }
    }

    GameObject GetPooled()
    {
        if (!usePooling) return null;
        if (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            // Keep under poolParent (do not reparent to canvas)
            go.SetActive(true);
            return go;
        }
        return null;
    }

    public void SpawnAtScreen(Vector2 screenPosition)
    {
        if (goldIconPrefab == null || uiCanvas == null)
        {
            Debug.LogWarning("GoldEffectManager: goldIconPrefab or uiCanvas is not assigned.");
            return;
        }

        RectTransform canvasRect = uiCanvas.transform as RectTransform;
        Camera cam = (uiCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? uiCanvas.worldCamera : null;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject go = null;
            if (usePooling)
            {
                go = GetPooled();
                if (go == null)
                {
                    // Create a new pooled instance under the poolParent so it doesn't appear on screen outside the pool
                    Transform parentForNew = poolParent != null ? poolParent : uiCanvas.transform;
                    go = Instantiate(goldIconPrefab, parentForNew, false);
                    go.SetActive(true);
                }
                else
                {
                    // already under poolParent; ensure active
                    go.SetActive(true);
                }
            }
            else
            {
                // Non-pooled instances should also be created under poolParent if available so they stay inside GoldEffects
                Transform parentForNew = poolParent != null ? poolParent : uiCanvas.transform;
                go = Instantiate(goldIconPrefab, parentForNew, false);
                go.SetActive(true);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            // Determine the RectTransform to convert screen -> local coordinates against.
            // Use the poolParent (or the object's parent) RectTransform so it stays correct while staying under poolParent.
            RectTransform referenceRect = poolParent as RectTransform ?? (RectTransform)uiCanvas.transform;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, screenPosition, cam, out localPoint);
            rt.anchoredPosition = localPoint;

            Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(spreadRadius * 0.4f, spreadRadius);
            Vector2 endPos = localPoint + randomDir;

            GoldIconEffect effect = go.GetComponent<GoldIconEffect>();
            if (effect != null)
            {
                effect.Play(endPos, duration);
            }
            else
            {
                rt.DOAnchorPos(endPos, duration).SetEase(Ease.OutCubic);
                Image img = go.GetComponent<Image>();
                if (img != null) img.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(() => Recycle(go));
                rt.DOScale(Vector3.one * 1.2f, duration * 0.4f).SetLoops(2, LoopType.Yoyo).OnComplete(() => Recycle(go));
            }
        }
    }

    // Recycle a gold icon back into the pool (or destroy if pooling disabled)
    public void Recycle(GameObject go)
    {
        if (go == null) return;

        // Kill any running tweens on the transform or image
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.DOKill();
        var img = go.GetComponent<Image>();
        if (img != null) img.DOKill();

        if (usePooling)
        {
            // Reset common properties
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, 1f);
            }
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            // Deactivate first to ensure it never shows on screen while reparenting
            go.SetActive(false);

            // Ensure it's parented under poolParent for the full lifecycle
            if (poolParent != null)
                go.transform.SetParent(poolParent, false);

            _pool.Enqueue(go);
        }
        else
        {
            Destroy(go);
        }
    }

    // Move all currently pooled objects under the poolParent for organization
    public void CollectAllToPoolParent()
    {
        if (!usePooling || poolParent == null) return;

        foreach (var go in _pool)
        {
            if (go != null)
                go.transform.SetParent(poolParent, false);
        }
    }

    // Set a custom pool parent at runtime
    public void SetPoolParent(Transform parent, bool moveExisting = true)
    {
        poolParent = parent;
        if (poolParent == null) return;
        if (moveExisting) CollectAllToPoolParent();
    }
}
