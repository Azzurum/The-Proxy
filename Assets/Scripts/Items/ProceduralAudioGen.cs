using UnityEngine;
using System.Collections.Generic;

public static class ProceduralAudioGen
{
    private const int SampleRate = 44100;
    
    // The global volume multiplier for all procedural sounds
    public static float globalVolume = 1.0f;

    public static void SetGlobalVolume(float volume)
    {
        globalVolume = Mathf.Clamp01(volume);
        clipCache.Clear(); // Clear the cache so future sounds use the new volume!
    }

    // The Cache: Stores generated clips by their unique parameter strings
    private static Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    // Generates a smooth, high-pitched "Beep" (Great for UI clicks or the Decoy ticking)
    public static AudioClip GenerateBeep(float frequency = 880f, float duration = 0.1f)
    {
        string key = $"Beep_{frequency}_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Sine wave math
            float time = i / (float)SampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * globalVolume;
            
            // Taper the end so it doesn't pop or click
            if (i > sampleCount - 1000) samples[i] *= (sampleCount - i) / 1000f; 
        }

        AudioClip clip = AudioClip.Create("ProcBeep", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a harsh, low-pitched "Buzz" (Great for Errors, full inventory, or jams)
    public static AudioClip GenerateErrorBuzz(float frequency = 150f, float duration = 0.3f)
    {
        string key = $"ErrorBuzz_{frequency}_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Square wave math (harsh/electronic)
            float time = i / (float)SampleRate;
            float sinValue = Mathf.Sin(2 * Mathf.PI * frequency * time);
            samples[i] = (sinValue > 0 ? 0.5f : -0.5f) * globalVolume; // Snap to extreme highs/lows

            // Taper the ends
            if (i < 500) samples[i] *= i / 500f;
            if (i > sampleCount - 1000) samples[i] *= (sampleCount - i) / 1000f;
        }

        AudioClip clip = AudioClip.Create("ProcError", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates harsh TV Static (Great for the Proxy attacking or Terminals closing)
    public static AudioClip GenerateStaticGlitch(float duration = 0.2f)
    {
        string key = $"StaticGlitch_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Random noise generation
            samples[i] = Random.Range(-0.8f, 0.8f) * globalVolume;
            
            // Fade out
            samples[i] *= 1f - ((float)i / sampleCount); 
        }

        AudioClip clip = AudioClip.Create("ProcStatic", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a descending "Pew" or "Zap" (Great for the ARC-Pulse stunner)
    public static AudioClip GeneratePew(float startFrequency = 2000f, float endFrequency = 200f, float duration = 0.3f)
    {
        // Quantize random pitch to 7 distinct variations to prevent cache memory leaks
        float actualStartFreq = startFrequency + (Mathf.Round(Random.Range(-3f, 3f)) * 100f);

        string key = $"Pew_{actualStartFreq}_{endFrequency}_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            // Calculate time progress from 0.0 to 1.0
            float t = (float)i / sampleCount;

            // Exponential frequency drop creates the classic "laser" slide
            float currentFreq = actualStartFreq * Mathf.Pow(endFrequency / actualStartFreq, t);
            
            // Advance the waveform phase by the current frequency
            phase += currentFreq * 2f * Mathf.PI / SampleRate;
            samples[i] = Mathf.Sin(phase) * globalVolume;

            // Fade out smoothly towards the end to avoid speaker popping
            if (i > sampleCount - 2000) samples[i] *= (sampleCount - i) / 2000f;
        }

        AudioClip clip = AudioClip.Create("ProcPew", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a deep, heavy pneumatic blast (Great for the K-80 Repulsor)
    public static AudioClip GeneratePneumaticBlast(float duration = 0.5f)
    {
        // Quantize random pitch to 5 distinct variations to prevent cache memory leaks
        float startFreq = Mathf.Round(Random.Range(13f, 17f)) * 10f; 

        string key = $"Pneumatic_{startFreq}_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        float endFreq = 20f;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = (float)i / sampleCount;

            // 1. The "Punch": Fast exponential frequency drop for a heavy kick impact
            float currentFreq = startFreq * Mathf.Pow(endFreq / startFreq, progress * 6f); 
            phase += currentFreq * 2f * Mathf.PI / SampleRate;
            float subBass = Mathf.Sin(phase);

            // 2. The "Air Hiss": White noise for the pneumatic gas burst
            float noise = Random.Range(-1f, 1f);

            // 3. The Envelope: Sharp attack that decays exponentially
            float envelope = Mathf.Exp(-7f * progress); 

            // Combine the thump (70%) and the hiss (30%), then apply the fade-out envelope
            samples[i] = (subBass * 0.7f + noise * 0.3f) * envelope * globalVolume;
        }

        AudioClip clip = AudioClip.Create("ProcPneumatic", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a sharp mechanical click (Great for Empty Weapons or UI Buttons)
    public static AudioClip GenerateClick(float frequency = 1000f, float duration = 0.05f)
    {
        string key = $"Click_{frequency}_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float env = Mathf.Exp(-time * 60f); // Extremely fast decay
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * env * 0.5f * globalVolume;
        }

        AudioClip clip = AudioClip.Create("ProcClick", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a pleasant ascending beep (Great for Picking Up Items)
    public static AudioClip GenerateAscendingChime(float duration = 0.2f)
    {
        string key = $"Chime_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float startFreq = 400f;
        float endFreq = 1200f;
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
            phase += currentFreq * 2f * Mathf.PI / SampleRate;
            
            float env = Mathf.Sin(t * Mathf.PI); // Swell and fade (bell curve)
            samples[i] = Mathf.Sin(phase) * env * 0.3f * globalVolume;
        }

        AudioClip clip = AudioClip.Create("ProcChime", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a sustained white noise hiss (Great for Heat Sinks venting)
    public static AudioClip GenerateHiss(float duration = 1.5f)
    {
        string key = $"Hiss_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 3f); // Slow fade out
            samples[i] = Random.Range(-1f, 1f) * env * 0.15f * globalVolume;
        }
        AudioClip clip = AudioClip.Create("ProcHiss", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a fast, sweeping noise burst (Great for the Proxy swinging its claw)
    public static AudioClip GenerateWhoosh(float duration = 0.4f)
    {
        string key = $"Whoosh_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f); // Swells up in the middle and fades
            samples[i] = Random.Range(-1f, 1f) * env * 0.2f * globalVolume;
        }
        AudioClip clip = AudioClip.Create("ProcWhoosh", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }
}