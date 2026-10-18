using UnityEngine;

/// <summary>
/// Controls dust particles and progressive room darkness.
/// Darkness reduces step-by-step with each solved puzzle,
/// and completely disappears once the door is unlocked/opened.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RoomDustAtmosphere : MonoBehaviour
{
    public static RoomDustAtmosphere Instance { get; private set; }

    [Header("Floor Dust Dimensions")]
    [SerializeField] private Vector3 roomDimensions = new Vector3(12f, 0.45f, 12f);
    [SerializeField] private Vector3 roomCenterOffset = new Vector3(0f, 0.2f, 0f);

    private ParticleSystem ps;
    private static Texture2D cachedDustTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadCallback()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitAfterSceneLoad()
    {
        OnSceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "Introduction") return;

        RoomDustAtmosphere existing = Object.FindAnyObjectByType<RoomDustAtmosphere>();
        if (existing == null)
        {
            GameObject go = new GameObject("RoomDustAtmosphere");
            go.AddComponent<RoomDustAtmosphere>();
        }
        else
        {
            existing.SetupDustParticleSystem();
            existing.ApplyProgressiveRoomLighting();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        ps = GetComponent<ParticleSystem>();

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.position = cam.transform.position + cam.transform.forward * 1.5f;
        }
        else
        {
            transform.position = new Vector3(0f, 1.2f, 0f);
        }
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (cachedDustTexture == null)
        {
            cachedDustTexture = CreateSoftCircularDustTexture();
        }

        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            Shader pShader = Shader.Find("Sprites/Default") 
                          ?? Shader.Find("Particles/Standard Unlit")
                          ?? Shader.Find("Mobile/Particles/Alpha Blended");
            
            if (pShader != null)
            {
                Material dustMat = new Material(pShader);
                dustMat.mainTexture = cachedDustTexture;
                psRenderer.material = dustMat;
            }
        }

        SetupDustParticleSystem();
        ApplyProgressiveRoomLighting();
    }

    private void Start()
    {
        if (IsRoomCompletedOrWon())
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var em = ps.emission;
                em.enabled = false;
            }
        }
        else
        {
            if (ps != null && !ps.isPlaying)
            {
                ps.Play();
            }
        }

        ApplyProgressiveRoomLighting();
        ProceduralAudioGenerator.EnsureBackgroundMusic();
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.position = cam.transform.position + cam.transform.forward * 1.5f;
        }
        ApplyProgressiveRoomLighting();
    }

    private Texture2D CreateSoftCircularDustTexture()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float radius = res * 0.48f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - norm, 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    public static bool IsRoomCompletedOrWon()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return PlayerPrefs.GetInt("Room1_Done", 0) == 1 || 
               PlayerPrefs.GetInt("Level1_Done", 0) == 1 || 
               PlayerPrefs.GetInt("Room1_Won", 0) == 1 ||
               PlayerPrefs.GetInt($"{sceneName}_Task_door", 0) == 1 ||
               (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("door"));
    }

    public void SetupDustParticleSystem()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        if (IsRoomCompletedOrWon())
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var em = ps.emission;
            em.enabled = false;
            return;
        }

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.maxParticles = 250;
        
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.82f, 0.85f),
            new Color(0.95f, 0.88f, 0.72f, 0.90f)
        );

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 35f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.scale = new Vector3(8f, 3.5f, 8f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.World;
        vol.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vol.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.03f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.09f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.04f;
        noise.damping = true;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.25f),
                new GradientAlphaKey(0.8f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        ps.Clear();
        ps.Play();
    }

    /// <summary>
    /// Progressive lighting: fades darkness step-by-step with each task completed,
    /// and completely clears all darkness once the door opens.
    /// </summary>
    public void ApplyProgressiveRoomLighting()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        bool trashDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("trash")) 
                         || PlayerPrefs.GetInt($"{sceneName}_Task_trash", 0) == 1;

        bool dustDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("dust")) 
                        || PlayerPrefs.GetInt($"{sceneName}_Task_dust", 0) == 1 
                        || PlayerPrefs.GetInt("HasItem_SecretBook", 0) == 1;

        bool candleDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle")) 
                          || PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1;

        bool chestOrKeyDone = (LevelProgressGuide.Instance != null && (LevelProgressGuide.Instance.IsTaskDone("chest") || LevelProgressGuide.Instance.IsTaskDone("key"))) 
                              || PlayerPrefs.GetInt($"{sceneName}_Task_chest", 0) == 1 
                              || PlayerPrefs.GetInt($"{sceneName}_Task_key", 0) == 1;

        bool doorDone = IsRoomCompletedOrWon();

        if (doorDone)
        {
            // عند فتح الباب: يزول التعتيم تماماً ويصبح الضوء ساطعاً وعالياً جداً بنور نهاري باهر
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientIntensity = 1.40f;
            RenderSettings.ambientLight = new Color(1f, 0.98f, 0.92f);

            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = 1.35f;
                    l.color = new Color(1f, 0.96f, 0.90f);
                }
                else if (l.type == LightType.Point)
                {
                    l.intensity = 1.6f;
                }
            }

            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var em = ps.emission;
                em.enabled = false;
            }
            return;
        }

        int completedSteps = (trashDone ? 1 : 0) + (dustDone ? 1 : 0) + (candleDone ? 1 : 0) + (chestOrKeyDone ? 1 : 0);
        float stepProgress = (float)completedSteps / 4f;

        // أثناء حل الألغاز: يخف التعتيم شوية صغيرة فقط، وتبقى الغرفة معتمة حتى فتح الباب
        // من 0.04f إلى 0.16f كحد أقصى (تخفيف طفيف للغموض مع وضوح الألغاز)
        float targetAmbientIntensity = Mathf.Lerp(0.04f, 0.16f, stepProgress);
        Color targetAmbientColor = Color.Lerp(new Color(0.02f, 0.03f, 0.05f), new Color(0.12f, 0.13f, 0.16f), stepProgress);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientIntensity = targetAmbientIntensity;
        RenderSettings.ambientLight = targetAmbientColor;

        float targetDirIntensity = Mathf.Lerp(0.05f, 0.18f, stepProgress);

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = targetDirIntensity;
            }
            else if (l.type == LightType.Point)
            {
                if (l.name.ToLower().Contains("candle") || l.name.ToLower().Contains("flame"))
                {
                    l.intensity = 1.4f;
                }
                else
                {
                    l.intensity = Mathf.Lerp(0.06f, 0.20f, stepProgress);
                }
            }
        }

        if (ps != null)
        {
            var em = ps.emission;
            em.rateOverTime = Mathf.Lerp(35f, 22f, stepProgress);
        }
    }

    public void ReduceDustAfterCleaning()
    {
        ApplyProgressiveRoomLighting();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.3f);
        Gizmos.DrawWireCube(transform.position + roomCenterOffset, roomDimensions);
    }
}