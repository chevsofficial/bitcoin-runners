// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;
    public AudioSource sfx, music;
    public AudioClip coin, hit, whoosh, musicLoop;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (music != null && musicLoop != null)
        {
            music.clip = musicLoop;
            music.loop = true;
            music.Play();
        }
    }

    public void PlayCoin() { if (sfx != null && coin) sfx.PlayOneShot(coin, 0.9f); }
    public void PlayHit() { if (sfx != null && hit) sfx.PlayOneShot(hit, 1.0f); }
    public void PlayWhoosh() { if (sfx != null && whoosh) sfx.PlayOneShot(whoosh, 0.9f); }
}
