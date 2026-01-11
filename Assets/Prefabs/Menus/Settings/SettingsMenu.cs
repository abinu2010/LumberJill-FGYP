using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
    [Header("Prefab (from Project)")]
    [SerializeField] private GameObject settingsPanelPrefab;   
    [SerializeField] private Transform panelParentOverride;    

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Mobile Settings")]
    [Tooltip("On mobile, tapping outside the panel closes it")]
    [SerializeField] private bool tapOutsideToClose = true;
    [Tooltip(" back button for mobile (Android back button behavior)")]
    [SerializeField] private bool useAndroidBackButton = true;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Behaviour")]
    [SerializeField] private bool pauseOnOpen = false;
    [SerializeField] private bool lockPlayerInputOnOpen = true;

    // runtime
    private GameObject panelInstance;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Button closeButton;
    private RectTransform panelRect;

    private string MusicKey => $"VOL_{musicParam}";
    private string SfxKey => $"VOL_{sfxParam}";

    private void Awake()
    {
        if (settingsPanelPrefab == null)
        {
            return;
        }

        Transform parent = panelParentOverride;
        if (parent == null)
        {
            Canvas c = GetComponentInParent<Canvas>();
            parent = c != null ? c.transform : null;
        }

        panelInstance = Instantiate(settingsPanelPrefab, parent);
        panelInstance.name = settingsPanelPrefab.name + "_INSTANCE";
        panelInstance.SetActive(false);

        panelRect = panelInstance.GetComponent<RectTransform>();

        if (openButton != null)
        {
            openButton.onClick.AddListener(Toggle);
        }
        var sliders = panelInstance.GetComponentsInChildren<Slider>(true);

        musicSlider = sliders.FirstOrDefault(s => s.name.ToLower().Contains("music"));
        sfxSlider = sliders.FirstOrDefault(s => s.name.ToLower().Contains("sfx"));

        if (musicSlider == null)
            musicSlider = sliders.FirstOrDefault(s => s.CompareTag("MusicVolume"));
        if (sfxSlider == null)
            sfxSlider = sliders.FirstOrDefault(s => s.CompareTag("SfxVolume"));

        if (musicSlider == null && sliders.Length > 0) musicSlider = sliders[0];
        if (sfxSlider == null && sliders.Length > 1) sfxSlider = sliders[1];


        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(SetMusicVolumeFromSlider);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(SetSfxVolumeFromSlider);
        }

        closeButton = panelInstance.GetComponentsInChildren<Button>(true)
                                   .FirstOrDefault(b => b.name.ToLower().Contains("close"));

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }


        float savedMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        float savedSfx = PlayerPrefs.GetFloat(SfxKey, 1f);

        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSfx;

        ApplyVolumeToMixer(musicParam, savedMusic);
        ApplyVolumeToMixer(sfxParam, savedSfx);
    }

    private void Update()
    {
        // PC keyboard toggle
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (useAndroidBackButton && Input.GetKeyDown(KeyCode.Escape))
        {
            if (panelInstance != null && panelInstance.activeSelf)
            {
                Close();
            }
        }
        if (tapOutsideToClose && panelInstance != null && panelInstance.activeSelf)
        {
            HandleTapOutsideToClose();
        }
    }

    private void HandleTapOutsideToClose()
    {
        // Check for touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsTouchOverPanel(touch.position))
                {
                    Close();
                }
            }
        }
        // Check for mouse click
        else if (Input.GetMouseButtonDown(0))
        {
            if (!IsTouchOverPanel(Input.mousePosition))
            {
                Close();
            }
        }
    }

    private bool IsTouchOverPanel(Vector2 screenPosition)
    {
        if (panelRect == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null);
    }

    public void Open()
    {
        if (panelInstance == null) return;
        if (panelInstance.activeSelf) return;

        panelInstance.SetActive(true);

        if (pauseOnOpen) Time.timeScale = 0f;
        if (lockPlayerInputOnOpen) TrySetPlayerInputLocked(true);
    }

    public void Close()
    {
        if (panelInstance == null) return;
        if (!panelInstance.activeSelf) return;

        panelInstance.SetActive(false);

        if (pauseOnOpen) Time.timeScale = 1f;
        if (lockPlayerInputOnOpen) TrySetPlayerInputLocked(false);
    }

    public void Toggle()
    {
        if (panelInstance == null) return;

        if (panelInstance.activeSelf) Close();
        else Open();
    }

    private void SetMusicVolumeFromSlider(float v)
    {
        ApplyVolumeToMixer(musicParam, v);
        PlayerPrefs.SetFloat(MusicKey, v);
    }

    private void SetSfxVolumeFromSlider(float v)
    {
        ApplyVolumeToMixer(sfxParam, v);
        PlayerPrefs.SetFloat(SfxKey, v);
    }

    private void ApplyVolumeToMixer(string param, float slider01)
    {
        if (mixer == null) return;

        float clamped = Mathf.Clamp(slider01, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        mixer.SetFloat(param, dB);
    }

    private void TrySetPlayerInputLocked(bool locked)
    {
        var type = System.Type.GetType("PlayerController");
        if (type == null) return;

        var field = type.GetField("IsInputLocked",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static);

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(null, locked);
        }
    }
}