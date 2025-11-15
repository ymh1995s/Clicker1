using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// UI_Animation: manages a set of 6 VideoClips (5 item buy clips + 1 game-clear clip).
/// - Clips[0..4] correspond to UI_ItemBuy Tier1..Tier5 purchases.
/// - Clips[5] is the GameClear clip.
/// Behavior:
/// - If there are no UI_ItemBuy components present in the scene at Start, this GameObject is disabled.
/// - If there are UI_ItemBuy components, this manager loops indefinitely through purchased item clips
///   chosen at random. When a clip finishes it picks another purchased clip (prefers a different one
///   when possible) and continues.
/// - PlayGameClearImmediate() plays the game-clear clip (Clips[5]) once immediately, interrupting
///   the item loop; after it finishes the item loop resumes.
/// - Fade-in and fade-out are applied at clip start/end using the provided CanvasGroup.
/// - All clips and references are exposed in the Inspector so they can be configured in the Editor.
/// </summary>
public class UI_Animation : MonoBehaviour
{
    [Header("Video setup (0..4 = ItemBuy Tier1..Tier5, 5 = GameClear)")]
    [Tooltip("Assign up to 6 clips: index 0..4 = item buy 1..5, index 5 = game clear clip")]
    [SerializeField] private List<VideoClip> Clips = new List<VideoClip>(6);

    [Header("References")]
    [Tooltip("VideoPlayer used to play clips. If null, one on the same GameObject will be auto-assigned.")]
    [SerializeField] private VideoPlayer _videoPlayer;

    [Tooltip("CanvasGroup used for fade in/out. If null, one on this GameObject will be created automatically.")]
    [SerializeField] private CanvasGroup _fadeGroup;

    [Header("Fade settings")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Prepare timeout (seconds)")]
    [SerializeField] private float _prepareTimeout = 5f;

    // internal
    private Coroutine _loopCoroutine;
    private Coroutine _playCoroutine;
    private Coroutine _pollCoroutine;
    private bool _isGameClearPlaying = false;
    private int _lastPlayedIndex = -1;
    private Action _onGameClearFinished = null;

    private void Awake()
    {
        // Robust VideoPlayer search: try component on self, then children, then scene
        if (_videoPlayer == null)
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                var vps = GetComponentsInChildren<VideoPlayer>(true);
                if (vps != null && vps.Length > 0)
                    _videoPlayer = vps[0];
            }

            if (_videoPlayer == null)
            {
                var all = UnityEngine.Object.FindObjectsOfType<VideoPlayer>(true);
                if (all != null && all.Length > 0)
                    _videoPlayer = all[0];
            }
        }

        // If no fade group provided, create one on this GameObject so fade always works without editor setup
        if (_fadeGroup == null)
        {
            _fadeGroup = GetComponent<CanvasGroup>();
            if (_fadeGroup == null)
            {
                _fadeGroup = gameObject.AddComponent<CanvasGroup>();
                // Start invisible until a clip fades in
                _fadeGroup.alpha = 0f;
            }
        }

        if (Clips == null)
            Clips = new List<VideoClip>(6);
        while (Clips.Count < 6) Clips.Add(null);

        if (_videoPlayer == null)
            Debug.LogWarning("UI_Animation: VideoPlayer not assigned or found on GameObject or children.", this);
        if (_fadeGroup == null)
            Debug.LogWarning("UI_Animation: CanvasGroup for fade could not be created.", this);

        // Ensure video player does not loop by default
        if (_videoPlayer != null)
            _videoPlayer.isLooping = false;
    }

    private void Start()
    {
        // If there are no UI_ItemBuy components at all, disable this GameObject entirely
        var itemBuys = FindObjectsOfType<UI_ItemBuy>(true);
        if (itemBuys == null || itemBuys.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        // If none of the item buys are purchased yet, keep visual hidden but continue polling
        var purchased = GetPurchasedItemIndices();
        if (purchased == null || purchased.Count == 0)
        {
            HideDisplayImmediate();
            // start polling for purchases so we can enable when an item is bought
            if (_pollCoroutine == null)
                _pollCoroutine = StartCoroutine(PollForPurchases());
            return;
        }

        // Start the loop coroutine
        StartLoopIfNeeded();
    }

    private IEnumerator PollForPurchases()
    {
        while (true)
        {
            var available = GetPurchasedItemIndices();
            if (available != null && available.Count > 0)
            {
                // ensure visible and start loop
                ShowDisplay();
                StartLoopIfNeeded();
                _pollCoroutine = null;
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void StartLoopIfNeeded()
    {
        if (_loopCoroutine != null) return;
        var available = GetPurchasedItemIndices();
        if (available != null && available.Count > 0)
        {
            _loopCoroutine = StartCoroutine(ItemLoopCoroutine());
        }
    }

    private void StopLoop()
    {
        if (_loopCoroutine != null)
        {
            try { StopCoroutine(_loopCoroutine); } catch { }
            _loopCoroutine = null;
        }
    }

    private void ShowDisplay()
    {
        if (_fadeGroup != null)
            _fadeGroup.alpha = 0f; // will fade in when clip starts
        // Ensure component enabled so coroutines run
        enabled = true;
    }

    private void HideDisplayImmediate()
    {
        // Keep script enabled so we can detect purchases, but hide visuals and stop loop
        StopLoop();
        if (_fadeGroup != null)
            _fadeGroup.alpha = 0f;
        // also stop any playing video
        try { _videoPlayer?.Stop(); } catch { }
    }

    private List<int> GetPurchasedItemIndices()
    {
        var list = new List<int>();
        if (GameManager.Instance == null) return list;

        for (int i = 0; i < 5; i++)
        {
            var tier = (EGPCUpgradeType)i;
            try
            {
                if (GameManager.Instance.PurchasedGPCItems != null && GameManager.Instance.PurchasedGPCItems.ContainsKey(tier) && GameManager.Instance.PurchasedGPCItems[tier])
                {
                    if (i >= 0 && i < Clips.Count && Clips[i] != null)
                        list.Add(i);
                }
            }
            catch { }
        }
        return list;
    }

    private IEnumerator ItemLoopCoroutine()
    {
        while (true)
        {
            if (_isGameClearPlaying)
            {
                yield return null;
                continue;
            }

            var available = GetPurchasedItemIndices();
            if (available == null || available.Count == 0)
            {
                // nothing purchased anymore - hide visuals and stop looping
                HideDisplayImmediate();
                yield break;
            }

            // pick random index, prefer different from last
            int pick = available[UnityEngine.Random.Range(0, available.Count)];
            if (available.Count > 1)
            {
                int attempts = 0;
                while (pick == _lastPlayedIndex && attempts < 5)
                {
                    pick = available[UnityEngine.Random.Range(0, available.Count)];
                    attempts++;
                }
            }

            _lastPlayedIndex = pick;

            // Play clip and wait for completion
            yield return StartCoroutine(PlayClipCoroutine(Clips[pick]));

            // small delay between clips
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator PlayClipCoroutine(VideoClip clip)
    {
        if (clip == null || _videoPlayer == null)
            yield break;

        // Stop any currently playing video
        try { _videoPlayer.Stop(); } catch { }

        _videoPlayer.clip = clip;
        _videoPlayer.isLooping = false;

        // Prepare
        try { _videoPlayer.Prepare(); } catch { }
        float t = 0f;
        while (!_videoPlayer.isPrepared && t < _prepareTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Start playback
        try { _videoPlayer.Play(); } catch { }

        // Fade in if possible
        if (_fadeGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_fadeGroup, 0f, 1f, _fadeDuration));

        // Determine playback time and initiate fade out before end
        float duration = (float)clip.length;
        float timeUntilFadeOut = Mathf.Max(0f, duration - _fadeDuration);

        float elapsed = 0f;
        while (elapsed < timeUntilFadeOut)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        if (_fadeGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_fadeGroup, 1f, 0f, _fadeDuration));

        // Stop video
        try { _videoPlayer.Stop(); } catch { }

        yield break;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    /// <summary>
    /// Play the GameClear clip (Clips[5]) immediately once. This will interrupt the item loop,
    /// play the game-clear clip without looping, then resume the item loop.
    /// Optional callback invoked after clip finishes.
    /// </summary>
    public void PlayGameClearImmediate(Action onFinished = null)
    {
        if (Clips == null || Clips.Count < 6 || Clips[5] == null)
        {
            Debug.LogWarning("UI_Animation: GameClear clip not assigned.");
            return;
        }

        // Stop any running play coroutine
        if (_playCoroutine != null) StopCoroutine(_playCoroutine);

        _onGameClearFinished = onFinished;
        // Signal game clear playing and start the sequence
        _playCoroutine = StartCoroutine(PlayGameClearRoutine());
    }

    private IEnumerator PlayGameClearRoutine()
    {
        _isGameClearPlaying = true;

        // stop loop coroutine temporarily
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }

        yield return StartCoroutine(PlayClipCoroutine(Clips[5]));

        // finished game clear
        _isGameClearPlaying = false;

        // resume loop if there are purchased items
        var purchased = GetPurchasedItemIndices();
        if (purchased != null && purchased.Count > 0)
            _loopCoroutine = StartCoroutine(ItemLoopCoroutine());
        else
            HideDisplayImmediate();

        // invoke callback after playback completes
        try
        {
            _onGameClearFinished?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_Animation: OnGameClearFinished callback threw - {ex}");
        }
        finally
        {
            _onGameClearFinished = null;
            _playCoroutine = null;
        }
    }
}
