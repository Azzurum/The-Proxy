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

    // Generates a satisfying mechanical switch/tray latch sound
    public static AudioClip GenerateTrayLatch(bool opening)
    {
        string key = $"TrayLatch_{opening}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * 0.1f);
        float[] samples = new float[sampleCount];
        
        float startFreq = opening ? 800f : 1200f;
        float endFreq = opening ? 1200f : 800f;
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 20f); // Fast decay for a crisp mechanical click
            
            float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
            phase += currentFreq * 2f * Mathf.PI / SampleRate;
            
            samples[i] = Mathf.Sin(phase) * env * 0.6f * globalVolume;
        }

        AudioClip clip = AudioClip.Create("ProcTrayLatch", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // ==========================================
    // SURVIVAL HORROR ADDITIONS
    // ==========================================

    // Generates a deep, double-thump heartbeat (Great for the Stress/Panic system)
    public static AudioClip GenerateHeartbeat(float duration = 1.0f)
    {
        string key = $"Heartbeat_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            
            // Create two distinct thumps per beat using math envelopes
            float thump1 = Mathf.Max(0, 1f - t * 8f); // Sharp decay for first thump
            float thump2 = t > 0.25f ? Mathf.Max(0, 1f - (t - 0.25f) * 6f) : 0f; // Second thump at 0.25s
            float envelope = thump1 + thump2;
            
            // Low frequency that drops slightly as the thump fades
            float freq = 60f - 30f * envelope; 
            phase += 2f * Mathf.PI * freq / SampleRate;
            
            samples[i] = Mathf.Sin(phase) * envelope * globalVolume * 0.8f; // Deep bass
        }

        AudioClip clip = AudioClip.Create("ProcHeartbeat", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a sweeping emergency siren (Great for reactor meltdowns or Kernel Panic)
    public static AudioClip GenerateAlarm(float duration = 2.0f)
    {
        string key = $"Alarm_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            
            // FM Synthesis: Sweep the frequency up and down continuously at 1.5Hz
            float freq = 800f + 400f * Mathf.Sin(2f * Mathf.PI * 1.5f * t); 
            phase += 2f * Mathf.PI * freq / SampleRate;
            
            samples[i] = Mathf.Sin(phase) * globalVolume * 0.4f;
            
            // Taper the extreme edges to prevent speaker popping
            if (i < 1000) samples[i] *= i / 1000f;
            if (i > sampleCount - 1000) samples[i] *= (sampleCount - i) / 1000f;
        }

        AudioClip clip = AudioClip.Create("ProcAlarm", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a heavy, gravelly footstep (Great for the Proxy walking)
    public static AudioClip GenerateFootstep(float duration = 0.2f)
    {
        string key = $"Footstep_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 25f); // Extremely fast decay (like a real impact)
            
            // The Highs: White noise to simulate gravel, boot scuffing, or metal scraping
            float noise = Random.Range(-1f, 1f) * 0.2f; 
            
            // The Lows: A pitch-dropping thud for the weight of the step
            float freq = 120f * Mathf.Exp(-t * 20f); 
            phase += 2f * Mathf.PI * freq / SampleRate;
            float thump = Mathf.Sin(phase) * 0.8f;
            
            // Mix them together
            samples[i] = (thump + noise) * envelope * globalVolume;
        }

        AudioClip clip = AudioClip.Create("ProcFootstep", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }

    // Generates a creepy, breathy "pssst" whisper (Great for psychological horror)
    public static AudioClip GenerateWhisper(float duration = 0.6f)
    {
        string key = $"Whisper_{duration}";
        if (clipCache.TryGetValue(key, out AudioClip cachedClip)) return cachedClip;

        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        
        float lastNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            float env = 0f;
            float sampleVal = 0f;

            if (t < 0.08f) 
            {
                // "P" sound: broadband noise + low plosive
                env = 1f - (t / 0.08f);
                float noise = Random.Range(-1f, 1f);
                float plosive = Mathf.Sin(2f * Mathf.PI * 100f * t);
                sampleVal = (noise * 0.4f + plosive * 0.6f) * env;
            }
            else if (t < 0.15f)
            {
                // Brief silence/gap between P and sss
                sampleVal = 0f;
            }
            else
            {
                // "Ssss" sound: high-pass noise with a swelling envelope
                float sTime = (t - 0.15f) / 0.85f; // 0 to 1 over the S part
                env = Mathf.Pow(Mathf.Sin(sTime * Mathf.PI), 0.5f); // Swell and fade
                
                float currentNoise = Random.Range(-1f, 1f);
                float highPassNoise = (currentNoise - lastNoise) * 0.5f;
                lastNoise = currentNoise;
                
                sampleVal = highPassNoise * env;
            }

            samples[i] = sampleVal * globalVolume * 0.6f;
        }

        AudioClip clip = AudioClip.Create("ProcWhisper", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }
}