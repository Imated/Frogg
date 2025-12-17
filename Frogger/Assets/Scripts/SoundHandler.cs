using UnityEngine;
using System.Collections;

public class SoundHandler : MonoBehaviour
{

    public AudioSource musicSource;
    public AudioSource introSource;


    public AudioClip backgroundMusic;
    public AudioClip introClip;


    public float startMusicVolume = 0.05f;
    public float targetMusicVolume = 0.3f;
    public float duckedMusicVolume = 0.1f;


    public float fadeSpeed = 2f;
    public float postIntroFadeDuration = 5f;

    Coroutine duckCoroutine;
    Coroutine postIntroFadeCoroutine;

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = startMusicVolume;
        musicSource.Play();

        yield return PlaySfxAndWait(introClip);

        if (postIntroFadeCoroutine != null) StopCoroutine(postIntroFadeCoroutine);
        postIntroFadeCoroutine = StartCoroutine(FadeMusicTo(targetMusicVolume, postIntroFadeDuration));
    }

    IEnumerator PlaySfxAndWait(AudioClip clip)
    {
        if (clip == null) yield break;

        introSource.Stop();
        introSource.clip = clip;
        introSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    public void PlayTriggerSFX(AudioClip clip)
    {
        if (clip == null) return;

        introSource.PlayOneShot(clip);

        if (duckCoroutine != null) StopCoroutine(duckCoroutine);
        duckCoroutine = StartCoroutine(DuckMusicForSeconds(clip.length));
    }

    IEnumerator DuckMusicForSeconds(float seconds)
    {
        if (postIntroFadeCoroutine != null)
        {
            StopCoroutine(postIntroFadeCoroutine);
            postIntroFadeCoroutine = null;
        }

        yield return StartCoroutine(FadeMusicTo(duckedMusicVolume, 0.15f));

        yield return new WaitForSeconds(seconds);

        yield return StartCoroutine(FadeMusicTo(targetMusicVolume, 0.4f));
    }

    IEnumerator FadeMusicTo(float toVolume, float duration)
    {
        float fromVolume = musicSource.volume;
        float t = 0f;

        if (duration <= 0f)
        {
            musicSource.volume = toVolume;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(fromVolume, toVolume, t / duration);
            yield return null;
        }

        musicSource.volume = toVolume;
    }
}
