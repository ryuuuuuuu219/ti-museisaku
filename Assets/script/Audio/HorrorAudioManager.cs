using UnityEngine;
using UnityEngine.SceneManagement;

public class HorrorAudioManager : MonoBehaviour
{
    public static HorrorAudioManager Instance { get; private set; }

    const int SampleRate = 44100;

    [SerializeField] float hallucinationVolume = 0.22f;
    [SerializeField] float fadeSpeed = 2.5f;

    AudioSource hallucinationSource;
    AudioClip hallucinationClip;
    Info info;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("HorrorAudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<HorrorAudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        hallucinationSource = gameObject.AddComponent<AudioSource>();
        hallucinationSource.loop = true;
        hallucinationSource.playOnAwake = false;
        hallucinationSource.spatialBlend = 0f;
        hallucinationSource.volume = 0f;

        hallucinationClip = CreateHallucinationLoop();
        hallucinationSource.clip = hallucinationClip;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (info == null)
            info = FindAnyObjectByType<Info>();

        bool shouldPlay = info != null && info.GetSanity() <= info.GetHallucinationSanityThreshold();
        float targetVolume = shouldPlay ? hallucinationVolume : 0f;

        if (shouldPlay && !hallucinationSource.isPlaying)
            hallucinationSource.Play();

        hallucinationSource.volume = Mathf.MoveTowards(
            hallucinationSource.volume,
            targetVolume,
            fadeSpeed * Time.deltaTime);

        if (!shouldPlay && hallucinationSource.isPlaying && hallucinationSource.volume <= 0.001f)
            hallucinationSource.Stop();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        info = FindAnyObjectByType<Info>();
    }

    AudioClip CreateHallucinationLoop()
    {
        const float seconds = 6f;
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];
        float previousNoise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float time = i / (float)SampleRate;
            float breath = Mathf.Sin(2f * Mathf.PI * 0.18f * time) * 0.5f + 0.5f;
            float lowDrone = Mathf.Sin(2f * Mathf.PI * 54f * time) * 0.32f;
            float uneasyBeat = Mathf.Sin(2f * Mathf.PI * 71f * time) * 0.16f;
            float whisper = Mathf.Sin(2f * Mathf.PI * 417f * time + Mathf.Sin(2f * Mathf.PI * 0.31f * time) * 7f) * 0.06f;
            previousNoise = Mathf.Lerp(previousNoise, Random.Range(-1f, 1f), 0.035f);

            data[i] = (lowDrone + uneasyBeat + whisper + previousNoise * 0.08f) * Mathf.Lerp(0.45f, 1f, breath);
        }

        AudioClip clip = AudioClip.Create("Hallucination_Loop", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
