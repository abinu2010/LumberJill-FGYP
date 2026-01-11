using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Clips (randomised)")]
    public AudioClip[] footstepClips;   

    [Header("Sound Tuning")]
    [Range(0.9f, 1.1f)] public float minPitch = 0.97f;
    [Range(0.9f, 1.1f)] public float maxPitch = 1.03f;
    public float volume = 1f;

    [Tooltip("Prevents double triggers from blends / overlapping events.")]
    public float minInterval = 0.08f;

    [Header("Routing (optional)")]
    public AudioMixerGroup sfxGroup;    

    private AudioSource src;
    private float lastPlayTime = -999f;
    private int lastIndex = -1;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D
        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
    }

   
    public void onFootstep(string foot = "")
    {
        // protect against blends or duplicate events
        if (Time.time - lastPlayTime < minInterval) return;

        if (footstepClips == null || footstepClips.Length == 0) return;

        // Pick a random clip, avoid immediate repeat
        int i;
        do { i = Random.Range(0, footstepClips.Length); }
        while (footstepClips.Length > 1 && i == lastIndex);

        lastIndex = i;

        src.pitch = Random.Range(minPitch, maxPitch);
        src.PlayOneShot(footstepClips[i], volume);
        lastPlayTime = Time.time;
    }
}