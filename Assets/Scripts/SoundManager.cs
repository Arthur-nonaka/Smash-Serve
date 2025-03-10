using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioMixerGroup sfxMixerGroup;

    // public AudioSource audioSource;

    public AudioClip bumpSound;
    public AudioClip spikeSound;
    public AudioClip setSound;
    public AudioClip groundSound;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySound(AudioClip audioClip, AudioSource audioSource)
    {
        if (audioSource != null && audioClip != null)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
            audioSource.PlayOneShot(audioClip);
        }
    }
}
