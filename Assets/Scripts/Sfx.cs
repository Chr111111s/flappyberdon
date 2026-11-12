using UnityEngine;

/// <summary>
/// Efectos de sonido sintetizados en runtime (sin assets de audio externos).
/// </summary>
public class Sfx : MonoBehaviour
{
    AudioSource source;
    AudioClip flapClip;
    AudioClip scoreClip;
    AudioClip hitClip;
    AudioClip bossClip;
    AudioClip startClip;

    public static Sfx Create(GameObject host)
    {
        var sfx = host.AddComponent<Sfx>();
        sfx.Init();
        return sfx;
    }

    void Init()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0.7f;

        BuildClips();
    }

    void BuildClips()
    {
        flapClip = Build("Flap", 0.11f, t =>
        {
            float f = Mathf.Lerp(540f, 980f, t / 0.11f);
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 30f);
        });

        scoreClip = Build("Score", 0.22f, t =>
        {
            float f = t < 0.08f ? 880f : 1320f;
            float local = t < 0.08f ? t : t - 0.08f;
            return Mathf.Sin(2f * Mathf.PI * f * local) * Mathf.Exp(-local * 14f) * 0.7f;
        });

        var rng = new System.Random(1337);
        hitClip = Build("Hit", 0.38f, t =>
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float thump = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 9f);
            return (noise * 0.55f * Mathf.Exp(-t * 24f)) + thump * 0.9f;
        });

        bossClip = Build("Boss", 0.65f, t =>
        {
            float f = Mathf.Lerp(150f, 55f, t / 0.65f);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 9f * t) * 18f;
            float wave = Mathf.Sin(2f * Mathf.PI * (f + vibrato) * t);
            float saw = Mathf.Repeat((f + vibrato) * t, 1f) * 2f - 1f;
            return (wave * 0.65f + saw * 0.35f) * Mathf.Exp(-t * 2.4f);
        });

        startClip = Build("Start", 0.18f, t =>
        {
            float f = Mathf.Lerp(440f, 760f, t / 0.18f);
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 8f);
        });
    }

    static AudioClip Build(string name, float duration, System.Func<float, float> generator)
    {
        const int rate = 44100;
        int samples = Mathf.Max(1, Mathf.CeilToInt(duration * rate));
        var data = new float[samples];
        float peak = 0.001f;

        for (int i = 0; i < samples; i++)
        {
            float value = generator(i / (float)rate);
            data[i] = value;
            peak = Mathf.Max(peak, Mathf.Abs(value));
        }

        float norm = 0.85f / peak;
        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Clamp(data[i] * norm, -1f, 1f);
        }

        var clip = AudioClip.Create(name, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void Flap()
    {
        if (flapClip == null) BuildClips();
        if (source != null && flapClip != null) source.PlayOneShot(flapClip, 0.45f);
    }

    public void Score()
    {
        if (scoreClip == null) BuildClips();
        if (source != null && scoreClip != null) source.PlayOneShot(scoreClip, 0.55f);
    }

    public void Hit()
    {
        if (hitClip == null) BuildClips();
        if (source != null && hitClip != null) source.PlayOneShot(hitClip, 0.8f);
    }

    public void Boss()
    {
        if (bossClip == null) BuildClips();
        if (source != null && bossClip != null) source.PlayOneShot(bossClip, 0.55f);
    }

    public void StartGame()
    {
        if (startClip == null) BuildClips();
        if (source != null && startClip != null) source.PlayOneShot(startClip, 0.5f);
    }
}
