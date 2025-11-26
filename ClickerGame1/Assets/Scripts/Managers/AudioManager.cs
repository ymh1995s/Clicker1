using System;
using UnityEngine;

// AudioManager: singleton that manages audio on a single empty GameObject.
// - mainBgm: loops and is used as the default background music.
// - sfxSource: used for PlayOneShot SFX (allows overlapping short sounds).
// The script will add AudioSource components automatically if they are not present.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Assign audio clips in the Inspector")]
    [SerializeField] private AudioClip _mainBgm;

    // Separate SFX: play-area tap vs UI clicks
    [Header("SFX Clips")]
    [SerializeField] private AudioClip _playAreaSfx;    // sound when clicking the play area (tap)
    [SerializeField] private AudioClip _uiClickSfx;      // sound for UI buttons (buy/upgrade/gacha)

    private AudioSource _mainSource;
    private AudioSource _sfxSource;

    // Target volumes (master settings)
    private float _bgmVolume = 0.3f; // default bgm level
    private float _sfxVolume = 0.2f; // default sfx level (user requested click SFX 0.2)

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Ensure two audio sources exist: main and sfx
        var sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            _mainSource = sources[0];
            _sfxSource = sources[1];
        }
        else
        {
            _mainSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            _sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }

        // Configure sources
        if (_mainSource != null)
        {
            _mainSource.playOnAwake = false;
            _mainSource.loop = true;
            _mainSource.clip = _mainBgm;
            _mainSource.spatialBlend = 0f; // 2D
            _mainSource.volume = _bgmVolume;
        }

        if (_sfxSource != null)
        {
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = _sfxVolume;
            // do not assign clip for sfx; PlayOneShot will be used
        }

        // Ensure AudioListener volume is neutral so we control volumes via sources
        AudioListener.volume = 1f;

        // Do NOT hardcode resource paths here. Assign clips in the Inspector.
        // If you prefer Resources loading, call SetMainClip/SetPlayAreaClip/SetUiClickClip at runtime.
    }

    private void Start()
    {
        // Subscribe to rebirth sequence complete to resume main BGM
        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthSequenceComplete += OnRebirthSequenceComplete;

        // Start main bgm if assigned
        if (_mainSource != null && _mainSource.clip != null && !_mainSource.isPlaying)
            _mainSource.Play();
        else if (_mainSource != null && _mainSource.clip == null)
            Debug.LogWarning("AudioManager: main BGM clip is not assigned. Assign in inspector.");

        if (_sfxSource == null)
            Debug.LogWarning("AudioManager: SFX AudioSource missing.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthSequenceComplete -= OnRebirthSequenceComplete;

        if (Instance == this) Instance = null;
    }

    private void OnRebirthSequenceComplete()
    {
        PlayMain();
    }

    public void PlayMain()
    {
        if (_mainSource == null)
            return;

        if (_mainSource.clip == null && _mainBgm != null)
            _mainSource.clip = _mainBgm;

        _mainSource.loop = true;
        _mainSource.volume = _bgmVolume;

        if (!_mainSource.isPlaying && _mainSource.clip != null)
            _mainSource.Play();
    }

    public void StopMain()
    {
        if (_mainSource == null) return;
        if (_mainSource.isPlaying) _mainSource.Stop();
    }

    // Play a short SFX using PlayOneShot so multiple sounds can overlap
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (_sfxSource == null || clip == null) return;
        try
        {
            // PlayOneShot volume is multiplied by the AudioSource.volume
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * _sfxVolume));
        }
        catch { }
    }

    // Convenience methods for categorized SFX (default to full, master SFX controls overall loudness)
    public void PlayPlayAreaSfx(float volume = 1f)
    {
        if (_playAreaSfx == null) return;
        PlaySfx(_playAreaSfx, volume);
    }

    public void PlayUiClickSfx(float volume = 1f)
    {
        if (_uiClickSfx == null) return;
        PlaySfx(_uiClickSfx, volume);
    }

    // Optional helper to set clips at runtime
    public void SetMainClip(AudioClip clip)
    {
        _mainBgm = clip;
        if (_mainSource != null) _mainSource.clip = clip;
    }

    public void SetPlayAreaClip(AudioClip clip)
    {
        _playAreaSfx = clip;
    }

    public void SetUiClickClip(AudioClip clip)
    {
        _uiClickSfx = clip;
    }

    // New: control master bgm/sfx volumes and enable/disable master sound
    public void SetBgmVolume(float vol)
    {
        _bgmVolume = Mathf.Clamp01(vol);
        if (_mainSource != null) _mainSource.volume = _bgmVolume;
    }

    public void SetSfxVolume(float vol)
    {
        _sfxVolume = Mathf.Clamp01(vol);
        if (_sfxSource != null) _sfxSource.volume = _sfxVolume;
    }

    public void SetMasterEnabled(bool enabled)
    {
        if (enabled)
        {
            SetBgmVolume(_bgmVolume);
            SetSfxVolume(_sfxVolume);
        }
        else
        {
            if (_mainSource != null) _mainSource.volume = 0f;
            if (_sfxSource != null) _sfxSource.volume = 0f;
        }
    }
}
