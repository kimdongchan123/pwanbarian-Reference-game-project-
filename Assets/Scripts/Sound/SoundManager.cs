using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    public void PlayBGM(AudioClip clip)
    {
        if (clip != null && bgmSource != null)
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }
    public void PlaySFX(AudioClip clip) { if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip); }
}