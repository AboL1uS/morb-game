using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Звуковые клипы — назначаются в Inspector ─────────────
    [Header("Орб")]
    [SerializeField] private AudioClip sfxShoot;     // выстрел
    [SerializeField] private AudioClip sfxOrbHit;    // орб получил урон
    [SerializeField] private AudioClip sfxOrbDie;    // смерть орба

    [Header("Враги")]
    [SerializeField] private AudioClip sfxEnemyHit;  // попадание по врагу
    [SerializeField] private AudioClip sfxEnemyDie;  // смерть врага
    [SerializeField] private AudioClip sfxBossDie;   // смерть босса

    [Header("UI")]
    [SerializeField] private AudioClip sfxButton;    // клик кнопки
    [SerializeField] private AudioClip sfxLevelUp;   // уровень вверх

    [Header("Музыка")]
    [SerializeField] private AudioClip musicGame;    // фоновая музыка
    [SerializeField] private AudioClip musicMenu;    // музыка меню

    [Header("Громкость")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.4f;

    // ── Пул AudioSource ───────────────────────────────────────
    private List<AudioSource> _pool = new List<AudioSource>();
    private int _poolIndex = 0;
    private const int POOL_SIZE = 8;

    private AudioSource _musicSource;
    private AudioClip _currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Источник для музыки
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;

        // Пул источников для SFX
        for (int i = 0; i < POOL_SIZE; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool.Add(src);
        }

        // Загружаем настройки громкости
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.4f);
    }

    
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource src = _pool[_poolIndex % POOL_SIZE];
        _poolIndex++;

        src.clip = clip;
        src.volume = sfxVolume * volumeScale;

        // Лёгкая случайная вариация тона — звуки не звучат одинаково
        src.pitch = Random.Range(0.92f, 1.08f);
        src.Play();
    }

    
    public void PlayShoot() => PlaySFX(sfxShoot, 0.7f);
    public void PlayOrbHit() => PlaySFX(sfxOrbHit, 1.0f);
    public void PlayOrbDie() => PlaySFX(sfxOrbDie, 1.0f);
    public void PlayEnemyHit() => PlaySFX(sfxEnemyHit, 0.6f);
    public void PlayEnemyDie() => PlaySFX(sfxEnemyDie, 0.8f);
    public void PlayBossDie() => PlaySFX(sfxBossDie, 1.0f);
    public void PlayButton() => PlaySFX(sfxButton, 0.5f);
    public void PlayLevelUp() => PlaySFX(sfxLevelUp, 1.0f);

    public void PlayMenuMusic() => PlayMusic(musicMenu);
    public void PlayGameMusic() => PlayMusic(musicGame);

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || clip == _currentMusic) return;
        _currentMusic = clip;
        StartCoroutine(CrossfadeMusic(clip));
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        // Затухание текущей
        float start = _musicSource.volume;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.5f)
        {
            _musicSource.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }

        // Смена клипа
        _musicSource.clip = newClip;
        _musicSource.Play();

        // Нарастание
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.5f)
        {
            _musicSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }
        _musicSource.volume = musicVolume;
    }

    
    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        _musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
}