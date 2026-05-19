using UnityEngine;

[RequireComponent(typeof(Info))]
public class HorrorAudioManager : MonoBehaviour
{
    public static HorrorAudioManager Instance { get; private set; }

    const int SampleRate = 44100;

    [SerializeField] float hallucinationVolume = 0.22f;
    [SerializeField] [Range(0f, 100f)] float heartbeatVolume = 0.18f;
    [SerializeField] [Range(0f, 100f)] float whiteNoiseVolume = 0.06f;
    [SerializeField] float fadeSpeed = 2.5f;
    [SerializeField] [Range(30f, 180f)] float heartbeatBpm = 60f;
    [SerializeField] float heartbeatDecayCoefficient = 5.5f;
    [SerializeField] float heartbeat60HzCoefficient = 0.9f;
    [SerializeField] float heartbeat80HzCoefficient = 0.65f;
    [SerializeField] float heartbeat100HzCoefficient = 0.45f;

    AudioSource hallucinationSource;
    AudioSource heartbeatSource;
    AudioSource whiteNoiseSource;
    AudioClip hallucinationClip;
    AudioClip heartbeatClip;
    AudioClip whiteNoiseClip;
    Info info;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        info = GetComponent<Info>();

        hallucinationSource = gameObject.AddComponent<AudioSource>();
        hallucinationSource.loop = true;
        hallucinationSource.playOnAwake = false;
        hallucinationSource.spatialBlend = 0f;
        hallucinationSource.volume = 0f;

        hallucinationClip = CreateHallucinationLoop();
        hallucinationSource.clip = hallucinationClip;

        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f;
        heartbeatSource.volume = 0f;

        RebuildHeartbeatLoop();

        whiteNoiseSource = gameObject.AddComponent<AudioSource>();
        whiteNoiseSource.loop = true;
        whiteNoiseSource.playOnAwake = false;
        whiteNoiseSource.spatialBlend = 0f;
        whiteNoiseSource.volume = 0f;

        whiteNoiseClip = CreateWhiteNoiseLoop();
        whiteNoiseSource.clip = whiteNoiseClip;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        bool shouldPlay = info != null && info.GetSanity() <= info.GetHallucinationSanityThreshold();
        float targetVolume = shouldPlay ? hallucinationVolume : 0f;

        if (shouldPlay && !hallucinationSource.isPlaying)
            hallucinationSource.Play();

        hallucinationSource.volume = Mathf.MoveTowards(
            hallucinationSource.volume,
            targetVolume,
            fadeSpeed * Time.deltaTime);

        if (shouldPlay && !heartbeatSource.isPlaying)
            heartbeatSource.Play();

        heartbeatSource.volume = Mathf.MoveTowards(
            heartbeatSource.volume,
            shouldPlay ? heartbeatVolume : 0f,
            fadeSpeed * Time.deltaTime);

        if (shouldPlay && !whiteNoiseSource.isPlaying)
            whiteNoiseSource.Play();

        whiteNoiseSource.volume = Mathf.MoveTowards(
            whiteNoiseSource.volume,
            shouldPlay ? GetNormalizedWhiteNoiseVolume() : 0f,
            fadeSpeed * Time.deltaTime);

        if (!shouldPlay && hallucinationSource.isPlaying && hallucinationSource.volume <= 0.001f)
            hallucinationSource.Stop();

        if (!shouldPlay && heartbeatSource.isPlaying && heartbeatSource.volume <= 0.001f)
            heartbeatSource.Stop();

        if (!shouldPlay && whiteNoiseSource.isPlaying && whiteNoiseSource.volume <= 0.001f)
            whiteNoiseSource.Stop();
    }

    public void SetHeartbeatVolume(float volume)
    {
        heartbeatVolume = Mathf.Max(0f, volume);
    }

    public void SetWhiteNoiseVolume(float volume)
    {
        whiteNoiseVolume = Mathf.Max(0f, volume);
    }

    public void SetHeartbeatBpm(float bpm)
    {
        heartbeatBpm = Mathf.Clamp(bpm, 30f, 180f);
        RebuildHeartbeatLoop();
    }

    public void SetHeartbeatDecayCoefficient(float coefficient)
    {
        heartbeatDecayCoefficient = Mathf.Max(0f, coefficient);
        RebuildHeartbeatLoop();
    }

    public void SetHeartbeatFrequencyCoefficients(float hz60, float hz80, float hz100)
    {
        heartbeat60HzCoefficient = hz60;
        heartbeat80HzCoefficient = hz80;
        heartbeat100HzCoefficient = hz100;
        RebuildHeartbeatLoop();
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

    AudioClip CreateWhiteNoiseLoop()
    {
        const float seconds = 2f;
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
            data[i] = Random.Range(-1f, 1f);

        AudioClip clip = AudioClip.Create("WhiteNoise_Loop", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    float GetNormalizedWhiteNoiseVolume()
    {
        return Mathf.Clamp01(whiteNoiseVolume / 10000f);
    }

    void RebuildHeartbeatLoop()
    {
        heartbeatClip = CreateHeartbeatLoop();

        if (heartbeatSource == null)
            return;

        bool wasPlaying = heartbeatSource.isPlaying;
        heartbeatSource.clip = heartbeatClip;

        if (wasPlaying)
            heartbeatSource.Play();
    }

    AudioClip CreateHeartbeatLoop()
    {
        float seconds = 60f / Mathf.Max(1f, heartbeatBpm);
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];

        AddHeartbeatPulse(data, 0f, 1f);

        if (seconds > 0.3f)
            AddHeartbeatPulse(data, 0.3f, 0.62f);

        AudioClip clip = AudioClip.Create("Heartbeat_Loop", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void AddHeartbeatPulse(float[] data, float startTime, float gain)
    {
        int startSample = Mathf.RoundToInt(startTime * SampleRate);

        for (int i = startSample; i < data.Length; i++)
        {
            float time = (i - startSample) / (float)SampleRate;
            float envelope = Mathf.Max(0f, 1f - heartbeatDecayCoefficient * time);

            if (envelope <= 0f)
                break;

            float pulse =
                Mathf.Sin(2f * Mathf.PI * 60f * time) * heartbeat60HzCoefficient +
                Mathf.Sin(2f * Mathf.PI * 80f * time) * heartbeat80HzCoefficient +
                Mathf.Sin(2f * Mathf.PI * 100f * time) * heartbeat100HzCoefficient;

            data[i] += pulse * envelope * gain * 0.18f;
        }
    }
}
