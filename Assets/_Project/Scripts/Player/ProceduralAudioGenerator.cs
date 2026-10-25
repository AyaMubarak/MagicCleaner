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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitBGM()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            EnsureBackgroundMusic();
        };
    }

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

            case "dust_wipe":
            case "wipe_surface":
                return GenerateTone("SFX_DustWipe", 0.18f, sampleRate, (t, p) =>
                {
                    // صوت مسح الأسطح الحسي واحتكاك إزالة الغبار (Soft Cloth Friction)
                    float noise = (UnityEngine.Random.value * 2f - 1f);
                    float filterFreq = Mathf.Lerp(800f, 350f, p);
                    float env = Mathf.Sin(p * Mathf.PI);
                    float tone = Mathf.Sin(2f * Mathf.PI * filterFreq * t) * 0.25f + noise * 0.75f;
                    return tone * env * 0.55f;
                });

            case "chain_break":
            case "break_shackle":
                return GenerateTone("SFX_ChainBreak", 0.65f, sampleRate, (t, p) =>
                {
                    // صوت انكسار القيود والسلاسل المعدنية السحرية
                    float clang1 = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-p * 18f);
                    float clang2 = Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-p * 12f);
                    float shatter = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-p * 25f) * 0.5f;
                    return (clang1 * 0.5f + clang2 * 0.4f + shatter) * 0.85f;
                });

            case "room_music":
                return GenerateTone("BGM_RoomAmbient", 24.0f, sampleRate, (t, p) =>
                {
                    // عزف بيانو وهارب كلاسيكي دافئ وراقي جداً (Acoustic Piano & Soft Harp Ballad)
                    // نغمات متفرقة بطيئة تداعب الروح وتبعث على الاسترخاء والتركيز التام
                    // توقيت وتردد النغمات (Arpeggiated Piano & Harp Notes)
                    // Progression: A minor 9 -> F major 7 -> C major add9 -> G suspended
                    (float timeOffset, float freq, float velocity)[] melodyNotes = new (float, float, float)[]
                    {
                        // Chord 1: Am (0.0s - 6.0s)
                        (0.0f,  220.00f, 0.40f), // A3 (Bass root)
                        (0.8f,  329.63f, 0.32f), // E4
                        (1.8f,  440.00f, 0.28f), // A4
                        (2.8f,  523.25f, 0.25f), // C5
                        (3.8f,  659.25f, 0.22f), // E5
                        (4.6f,  523.25f, 0.18f), // C5

                        // Chord 2: Fmaj7 (6.0s - 12.0s)
                        (6.0f,  174.61f, 0.42f), // F3 (Bass root)
                        (6.9f,  261.63f, 0.30f), // C4
                        (7.8f,  349.23f, 0.26f), // F4
                        (8.9f,  440.00f, 0.28f), // A4
                        (9.9f,  523.25f, 0.24f), // C5
                        (10.8f, 392.00f, 0.19f), // G4

                        // Chord 3: Cadd9 (12.0s - 18.0s)
                        (12.0f, 130.81f, 0.45f), // C3 (Deep root)
                        (12.9f, 196.00f, 0.32f), // G3
                        (13.8f, 293.66f, 0.28f), // D4
                        (14.8f, 329.63f, 0.25f), // E4
                        (15.9f, 392.00f, 0.22f), // G4
                        (16.8f, 293.66f, 0.18f), // D4

                        // Chord 4: G/B (18.0s - 24.0s)
                        (18.0f, 123.47f, 0.42f), // B2 (Root)
                        (18.9f, 196.00f, 0.30f), // G3
                        (19.8f, 293.66f, 0.26f), // D4
                        (20.9f, 392.00f, 0.25f), // G4
                        (21.9f, 493.88f, 0.20f), // B4
                        (22.8f, 392.00f, 0.18f)  // G4
                    };

                    float musicSignal = 0f;

                    // محاكاة رنين أوتار البيانو والهارب الصوتية (Acoustic String Physical Decay)
                    for (int i = 0; i < melodyNotes.Length; i++)
                    {
                        float noteTime = t - melodyNotes[i].timeOffset;
                        if (noteTime >= 0f && noteTime < 5.0f)
                        {
                            float freq = melodyNotes[i].freq;
                            float vel = melodyNotes[i].velocity;

                            // انحسار ناعم وطبيعي جداً يشبه ضربة مطرقة البيانو الحقيقية
                            float attack = Mathf.Clamp01(noteTime / 0.035f);
                            float decay = Mathf.Exp(-noteTime * 1.15f);
                            float envelope = attack * decay * vel;

                            // طبقات الهارمونيكس الطبيعية العذبة للبيانو (Harmonics)
                            float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
                            float harmonic2   = Mathf.Sin(2f * Mathf.PI * (freq * 2.001f) * t) * 0.28f;
                            float harmonic3   = Mathf.Sin(2f * Mathf.PI * (freq * 3.002f) * t) * 0.10f;
                            float harmonic4   = Mathf.Sin(2f * Mathf.PI * (freq * 4.004f) * t) * 0.04f;

                            // رنين خافت في الصندوق الخشبي للبيانو (Body Resonance)
                            float bodyWarmth  = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.12f;

                            musicSignal += (fundamental + harmonic2 + harmonic3 + harmonic4 + bodyWarmth) * envelope;
                        }
                    }

                    // تنعيم أطراف المقطع لضمان تكرار ناعم جداً ومستمر كالهواء
                    float loopSmooth = 1f;
                    if (p < 0.04f) loopSmooth = p / 0.04f;
                    else if (p > 0.96f) loopSmooth = (1f - p) / 0.04f;

                    // مستوى هادئ وخفيف جداً مريح للأذن
                    return musicSignal * loopSmooth * 0.38f;
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
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("ProceduralAmbientBGM");
            UnityEngine.Object.DontDestroyOnLoad(bgmObj);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.spatialBlend = 0f; // 2D Stereo
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (bgmSource.clip == null)
        {
            // محاولة تحميل الأغنية الحقيقية Beneath_the_Silver_Canopy من StreamingAssets
            LoadSongFromStreamingAssets();
        }

        ApplySavedAudioSettings();

        if (PlayerPrefs.GetInt("IsMuted", 0) == 0)
        {
            if (bgmSource.clip != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    private static void LoadSongFromStreamingAssets()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Beneath_the_Silver_Canopy.mp3");
        if (System.IO.File.Exists(filePath))
        {
            // تشغيل كوروتين للتحميل الفوري
            if (bgmSource != null)
            {
                MonoBehaviour runner = UnityEngine.Object.FindAnyObjectByType<MonoBehaviour>();
                if (runner != null)
                {
                    runner.StartCoroutine(LoadSongCoroutine(filePath));
                    return;
                }
            }
        }

        // في حال عدم وجود الملف نستخدم نغمة البيانو
        if (bgmSource != null && bgmSource.clip == null)
        {
            bgmSource.clip = CreateRuntimeClip("room_music");
        }
    }

    private static System.Collections.IEnumerator LoadSongCoroutine(string filePath)
    {
        string url = "file://" + filePath.Replace("\\", "/");
        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip song = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                if (song != null && bgmSource != null)
                {
                    bgmSource.clip = song;
                    Debug.Log("<color=green>[BGM]:</color> تم تحميل وتشغيل أغنية Beneath_the_Silver_Canopy بنجاح!");
                    ApplySavedAudioSettings();
                    if (PlayerPrefs.GetInt("IsMuted", 0) == 0 && !bgmSource.isPlaying)
                    {
                        bgmSource.Play();
                    }
                }
            }
            else
            {
                Debug.LogWarning("[BGM]: تعذر تحميل MP3، استخدام العزف البديل: " + uwr.error);
                if (bgmSource != null && bgmSource.clip == null)
                {
                    bgmSource.clip = CreateRuntimeClip("room_music");
                    if (PlayerPrefs.GetInt("IsMuted", 0) == 0 && !bgmSource.isPlaying)
                    {
                        bgmSource.Play();
                    }
                }
            }
        }
    }

    public static void RefreshAudioSettings()
    {
        ApplySavedAudioSettings();
    }

    private static void ApplySavedAudioSettings()
    {
        bool muted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        float master = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
        float music = Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 1f));
        float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));

        // التحكم الشامل بصوت اللعبة (Master Mute / Master Volume)
        AudioListener.volume = muted ? 0f : master;

        if (bgmSource != null)
        {
            bgmSource.volume = muted ? 0f : (0.28f * music);

            if (muted && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
            else if (!muted && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    public static float GetSFXVolume()
    {
        bool muted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        if (muted) return 0f;
        float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        return sfx;
    }
}
