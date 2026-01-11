using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicJukebox : MonoBehaviour
{
    [Header("Playlist")]
    public List<AudioClip> tracks = new List<AudioClip>();

    [Tooltip("Shuffle with a no-repeat-until-all-played bag.")]
    public bool shuffle = true;

    [Tooltip("Avoid playing the same track twice in a row when there are 2+ tracks.")]
    public bool avoidImmediateRepeat = true;

    [Header("Playback")]
    [Tooltip("Automatically start and keep playing across scenes.")]
    public bool autoPlay = true;

    [Tooltip("Seconds to crossfade between songs.")]
    [Range(0f, 10f)] public float crossfadeSeconds = 1.5f;

    [Tooltip("Minimum headroom before track end to start the next crossfade.")]
    [Range(0.1f, 10f)] public float preFadeLeadIn = 1.5f;

    [Header("Mixer (optional but recommended)")]
    public AudioMixerGroup outputGroup; // route to Music group if you have a Mixer

    // --- internals ---
    static MusicJukebox instance;       // simple singleton
    AudioSource a, b;                   // dual sources for crossfade
    AudioSource active, idle;           // pointers
    Coroutine loopRoutine;

    int lastIndex = -1;                 // last played track
    readonly List<int> shuffleBag = new List<int>();
    System.Random rng = new System.Random();

    void Awake()
    {
        // Singleton so music persists between scenes
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Create / configure two AudioSources
        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.playOnAwake = false;
            s.loop = false;            // crossfader handles looping across tracks
            s.spatialBlend = 0f;       // 2D music
            s.volume = 0f;             // we’ll fade in
            if (outputGroup != null) s.outputAudioMixerGroup = outputGroup;
        }
        active = a; idle = b;
    }

    void Start()
    {
        if (autoPlay && tracks.Count > 0)
        {
            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = StartCoroutine(MainLoop());
        }
    }

    public void Play() { if (loopRoutine == null && tracks.Count > 0) loopRoutine = StartCoroutine(MainLoop()); }
    public void StopAll()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        loopRoutine = null;
        if (a) a.Stop();
        if (b) b.Stop();
    }
    public void Skip() { if (isActiveAndEnabled && loopRoutine != null) StartCoroutine(CrossfadeToIndex(GetNextIndex(), crossfadeSeconds)); }

    IEnumerator MainLoop()
    {
        // Start first song
        int first = GetNextIndex();
        yield return StartCoroutine(StartFresh(first, Mathf.Max(0.01f, crossfadeSeconds)));

        while (true)
        {
            // Wait until it's time to begin crossfade to the next song
            float lead = Mathf.Clamp(preFadeLeadIn, 0.1f, 10f);
            while (active != null && active.isPlaying)
            {
                float remaining = (active.clip.length - active.time);
                if (remaining <= Mathf.Max(lead, crossfadeSeconds * 0.75f)) break;
                yield return null;
            }

            // Crossfade to the next selected song
            int next = GetNextIndex();
            yield return StartCoroutine(CrossfadeToIndex(next, crossfadeSeconds));
        }
    }

    IEnumerator StartFresh(int index, float fadeIn)
    {
        var clip = tracks[index];
        lastIndex = index;

        active.clip = clip;
        active.time = 0f;
        active.volume = 0f;
        active.Play();

        // simple fade in
        yield return StartCoroutine(FadeVolume(active, 0f, 1f, fadeIn));
    }

    IEnumerator CrossfadeToIndex(int index, float duration)
    {
        var next = tracks[index];
        lastIndex = index;

        // prepare idle
        idle.clip = next;
        idle.time = 0f;
        idle.volume = 0f;
        idle.Play();

        // fade: active 1->0, idle 0->1
        float d = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < d)
        {
            float k = t / d;
            if (active) active.volume = 1f - k;
            if (idle) idle.volume = k;
            t += Time.deltaTime;
            yield return null;
        }

        // finalize
        if (active) { active.volume = 0f; active.Stop(); }
        if (idle) { idle.volume = 1f; }

        // swap roles
        var tmp = active; active = idle; idle = tmp;
    }

    IEnumerator FadeVolume(AudioSource src, float from, float to, float seconds)
    {
        float t = 0f, d = Mathf.Max(0.01f, seconds);
        if (src) src.volume = from;
        while (t < d)
        {
            if (src) src.volume = Mathf.Lerp(from, to, t / d);
            t += Time.deltaTime;
            yield return null;
        }
        if (src) src.volume = to;
    }

    int GetNextIndex()
    {
        int n = tracks.Count;
        if (n == 0) return -1;
        if (!shuffle)
        {
            // simple sequential with no immediate repeat
            int next = (lastIndex + 1) % n;
            if (avoidImmediateRepeat && n > 1 && next == lastIndex) next = (next + 1) % n;
            return next;
        }

        // Shuffle-bag: refill when empty
        if (shuffleBag.Count == 0)
        {
            shuffleBag.Clear();
            for (int i = 0; i < n; i++)
                if (!(avoidImmediateRepeat && n > 1 && i == lastIndex))
                    shuffleBag.Add(i);
        }

        // pick random from bag
        int pick = rng.Next(shuffleBag.Count);
        int index = shuffleBag[pick];
        shuffleBag.RemoveAt(pick);
        return index;
    }
}
