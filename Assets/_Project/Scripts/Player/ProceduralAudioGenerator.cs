using System;
using System.IO;
using UnityEngine;

/// <summary>
/// مولّد ومُدير المؤثرات الصوتية الإجرائية (Procedural Sound FX & Audio Synthesizer)
/// يُنشئ ملفات WAV نقية وعالية الجودة داخل Assets/_Project/Audio تلقائياً عند التشغيل أو عبر الـ Menu
/// لتعمل جميع تفاعلات اللعبة بأصوات سحرية متناغمة وبدون الحاجة لملفات خارجية يدوياً.
/// </summary>
public static class ProceduralAudioGenerator
{
    private const string AudioDirectory = "Assets/_Project/Audio";

    public static AudioClip GetOrCreateClip(string clipName, Action<float[], int> generator = null)
    {
        // 1. محاولة التحميل من المجلد إن كان موجوداً
        AudioClip loaded = Resources.Load<AudioClip>(clipName);
        if (loaded != null) return loaded;

        return CreateRuntimeClip(clipName);
    }

    /// <summary>
    /// توليد صوت إجرائي مباشر في الذاكرة بأعلى جودة ونقاء
    /// </summary>
    public static AudioClip CreateRuntimeClip(string soundType)
    {
        int sampleRate = 44100;

        switch (soundType.ToLower())
        {
            case "click":
            case "ui_click":
                return GenerateTone("SFX_UIClick", 0.06f, sampleRate, (t, p) =>
                {
                    float env = Mathf.Exp(-p * 50f);
                    return Mathf.Sin(2f * Mathf.PI * (1200f - p * 400f) * t) * env * 0.5f;
                });

            case "pickup":
            case "item_collect":
                return GenerateTone("SFX_ItemPickup", 0.35f, sampleRate, (t, p) =>
                {
                    // رنين بلوري خفيف متصاعد (Crystal Chime)
                    float freq = Mathf.Lerp(520f, 1040f, p * p);
                    float env = Mathf.Exp(-p * 8f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                                 Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
                    return wave * env * 0.6f;
                });

            case "flame":
            case "candle_ignite":
                return GenerateTone("SFX_CandleIgnite", 0.5f, sampleRate, (t, p) =>
                {
                    // صوت احتكاك وشعلة نارية سحرية (Whoosh & Spark)
                    float noise = (UnityEngine.Random.value * 2f - 1f);
                    float freq = Mathf.Lerp(180f, 480f, p);
                    float env = Mathf.Sin(p * Mathf.PI) * Mathf.Exp(-p * 3f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + noise * 0.5f;
                    return wave * env * 0.7f;
                });

            case "dial_tick":
            case "lock_dial":
                return GenerateTone("SFX_DialTick", 0.045f, sampleRate, (t, p) =>
                {
                    // طقة ميكانيكية دقيقة لقفل الأرقام المعدني
                    float env = Mathf.Exp(-p * 80f);
                    float click = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.7f +
                                  Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.3f;
                    return click * env * 0.6f;
                });

            case "chest_unlock":
            case "unlock_fanfare":
                return GenerateTone("SFX_ChestUnlock", 0.85f, sampleRate, (t, p) =>
                {
                    // نغمة كشف وفتح سحرية مبهجة (Magic Revelation Arpeggio)
                    float freq = (p < 0.25f) ? 523.25f : // C5
                                 (p < 0.50f) ? 659.25f : // E5
                                 (p < 0.75f) ? 783.99f : // G5
                                               1046.50f; // C6
                    float env = Mathf.Exp(-(p % 0.25f) * 12f) * (1f - p * 0.3f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                                 Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f;
                    return wave * env * 0.75f;
                });

            case "door_open":
            case "creak_open":
                return GenerateTone("SFX_DoorOpen", 0.9f, sampleRate, (t, p) =>
                {
                    // صوت صرير باب خشبي أثري عميق ينفتح
                    float freq = Mathf.Lerp(140f, 220f, Mathf.Sin(p * Mathf.PI * 4f) * 0.2f + p);
                    float env = Mathf.Sin(p * Mathf.PI) * (1f - p * 0.2f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                                 Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;
                    return wave * env * 0.65f;
                });

            case "door_locked":
            case "locked_rattle":
                return GenerateTone("SFX_DoorLocked", 0.25f, sampleRate, (t, p) =>
                {
                    // اهتزاز مقبض مقفل
                    float env = Mathf.Exp(-p * 20f);
                    float wave = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f +
                                 (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                    return wave * env * 0.6f;
                });

            case "level_complete":
            case "victory_fanfare":
                return GenerateTone("SFX_LevelComplete", 1.8f, sampleRate, (t, p) =>
                {
                    // لحن احتفالي محمس بانتصار وإنهاء المرحلة (Grand Victory Fanfare)
                    // النغمات: C5 -> E5 -> G5 -> C6 (طويلة ومبهجة)
                    float freq;
                    float subEnv;
                    if (p < 0.2f)
                    {
                        freq = 523.25f; // C5
                        subEnv = Mathf.Exp(-(p / 0.2f) * 4f);
                    }
                    else if (p < 0.4f)
                    {
                        freq = 659.25f; // E5
                        subEnv = Mathf.Exp(-((p - 0.2f) / 0.2f) * 4f);
                    }
                    else if (p < 0.65f)
                    {
                        freq = 783.99f; // G5
                        subEnv = Mathf.Exp(-((p - 0.4f) / 0.25f) * 3f);
                    }
                    else
                    {
                        freq = 1046.50f; // High C6 (Victory culmination)
                        subEnv = Mathf.Exp(-((p - 0.65f) / 0.35f) * 2f);
                    }

                    // صوت براس وأبواق احتفالية مع هارمونيكس غنية
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f +
                                 Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f +
                                 Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.15f;
                    return wave * subEnv * 0.85f;
                });

            case "firework_pop":
            case "firework":
                return GenerateTone("SFX_Firework", 0.65f, sampleRate, (t, p) =>
                {
                    // صوت انفجار فتيش وألعاب نارية احتفالية (Boom & Crackle)
                    float noise = (UnityEngine.Random.value * 2f - 1f);
                    float env = Mathf.Exp(-p * 10f);
                    float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, p) * t) * env;
                    float crackle = (p > 0.15f) ? (UnityEngine.Random.value > 0.85f ? (UnityEngine.Random.value * 2f - 1f) : 0f) * Mathf.Exp(-(p - 0.15f) * 6f) : 0f;
                    return (thud * 0.7f + noise * 0.3f * env + crackle * 0.5f) * 0.9f;
                });

            case "magic_chime":
            case "code_reveal":
                return GenerateTone("SFX_MagicReveal", 0.7f, sampleRate, (t, p) =>
                {
                    // رنين سحري متألق عند ظهور الرمز السري على الجدار
                    float freq = Mathf.Lerp(600f, 1400f, p);
                    float tremolo = Mathf.Sin(2f * Mathf.PI * 15f * t) * 0.3f + 0.7f;
                    float env = Mathf.Exp(-p * 4f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * tremolo;
                    return wave * env * 0.65f;
                });

            case "room_music":
                return GenerateTone("BGM_RoomAmbient", 12.0f, sampleRate, (t, p) =>
                {
                    // نغمات خلفية موسيقية هادئة وسحرية (Mystical Ambient Pad)
                    float slowLfo = Mathf.Sin(2f * Mathf.PI * 0.12f * t) * 0.5f + 0.5f;
                    float chord1 = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.3f; // A3
                    float chord2 = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.25f; // C4
                    float chord3 = Mathf.Sin(2f * Mathf.PI * 329.63f * t) * 0.25f; // E4
                    float shimmer = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.1f * slowLfo;
                    return (chord1 + chord2 + chord3 + shimmer) * 0.4f;
                });

            default:
                return GenerateTone("SFX_Default", 0.1f, sampleRate, (t, p) => Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Exp(-p * 10f));
        }
    }

    private static AudioClip GenerateTone(string name, float duration, int sampleRate, Func<float, float, float> sampleFunc)
    {
        int samplesCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[samplesCount];

        for (int i = 0; i < samplesCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / samplesCount;
            samples[i] = Mathf.Clamp(sampleFunc(t, progress), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, samplesCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// تشغيل أي صوت مباشرة بنقطة معينة أو عالمياً بدون الحاجة لإنشاء كائنات صوتية مسبقاً
    /// </summary>
    public static void PlaySound(string soundType, float volume = 1f, Vector3? position = null)
    {
        AudioClip clip = CreateRuntimeClip(soundType);
        if (clip == null) return;

        Vector3 pos = position.HasValue ? position.Value : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }

    private static AudioSource bgmSource = null;

    /// <summary>
    /// تشغيل موسيقى وخلفية صوتية هادئة وسحرية ومستمرة للغرفة
    /// </summary>
    public static void EnsureBackgroundMusic()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("ProceduralAmbientBGM");
                UnityEngine.Object.DontDestroyOnLoad(bgmObj);
                bgmSource = bgmObj.AddComponent<AudioSource>();
            }

            bgmSource.clip = CreateRuntimeClip("room_music");
            bgmSource.loop = true;
            bgmSource.volume = 0.28f;
            bgmSource.spatialBlend = 0f; // 2D
            bgmSource.Play();
        }
    }
}
