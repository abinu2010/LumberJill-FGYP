using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panel In Scene")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button quitButton;

    [Header("Button Names")]
    [SerializeField] private string closeButtonName = "CloseButton";
    [SerializeField] private string quitButtonName = "QuitButton";

    [Header("Keyboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Mobile Settings")]
    [SerializeField] private bool tapOutsideToClose = true;
    [SerializeField] private bool useAndroidBackButton = true;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Behaviour")]
    [SerializeField] private bool closeOnStart = true;
    [SerializeField] private bool pauseOnOpen = false;
    [SerializeField] private bool lockPlayerInputOnOpen = true;

    private Slider musicSlider;
    private Slider sfxSlider;
    private RectTransform panelRect;

    private string MusicKey => $"VOL_{musicParam}";
    private string SfxKey => $"VOL_{sfxParam}";

    private void Awake()
    {
        if (settingsPanel == null)
        {
            return;
        }

        panelRect = settingsPanel.GetComponent<RectTransform>();

        FindSliders();
        FindButtons();
        WireButtons();
        LoadSavedVolumes();

        if (closeOnStart)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (useAndroidBackButton && toggleKey != KeyCode.Escape && Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                Close();
            }
        }

        if (tapOutsideToClose && settingsPanel != null && settingsPanel.activeSelf)
        {
            HandleTapOutsideToClose();
        }
    }

    private void FindSliders()
    {
        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);

        musicSlider = sliders.FirstOrDefault(s => s.name.ToLower().Contains("music"));
        sfxSlider = sliders.FirstOrDefault(s => s.name.ToLower().Contains("sfx"));

        if (musicSlider == null)
        {
            musicSlider = sliders.FirstOrDefault(s => s.CompareTag("MusicVolume"));
        }

        if (sfxSlider == null)
        {
            sfxSlider = sliders.FirstOrDefault(s => s.CompareTag("SfxVolume"));
        }

        if (musicSlider == null && sliders.Length > 0)
        {
            musicSlider = sliders[0];
        }

        if (sfxSlider == null && sliders.Length > 1)
        {
            sfxSlider = sliders[1];
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.RemoveListener(SetMusicVolumeFromSlider);
            musicSlider.onValueChanged.AddListener(SetMusicVolumeFromSlider);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolumeFromSlider);
            sfxSlider.onValueChanged.AddListener(SetSfxVolumeFromSlider);
        }
    }

    private void FindButtons()
    {
        Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true);

        if (closeButton == null)
        {
            closeButton = buttons.FirstOrDefault(b => b.name == closeButtonName);
        }

        if (quitButton == null)
        {
            quitButton = buttons.FirstOrDefault(b => b.name == quitButtonName);
        }
    }

    private void WireButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Toggle);
            openButton.onClick.AddListener(Toggle);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void LoadSavedVolumes()
    {
        float savedMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        float savedSfx = PlayerPrefs.GetFloat(SfxKey, 1f);

        if (musicSlider != null)
        {
            musicSlider.value = savedMusic;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSfx;
        }

        ApplyVolumeToMixer(musicParam, savedMusic);
        ApplyVolumeToMixer(sfxParam, savedSfx);
    }

    private void HandleTapOutsideToClose()
    {
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
        if (panelRect == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null);
    }

    public void Open()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanel.activeSelf)
        {
            return;
        }

        settingsPanel.SetActive(true);

        if (pauseOnOpen)
        {
            Time.timeScale = 0f;
        }

        if (lockPlayerInputOnOpen)
        {
            TrySetPlayerInputLocked(true);
        }
    }

    public void Close()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (!settingsPanel.activeSelf)
        {
            return;
        }

        settingsPanel.SetActive(false);

        if (pauseOnOpen)
        {
            Time.timeScale = 1f;
        }

        if (lockPlayerInputOnOpen)
        {
            TrySetPlayerInputLocked(false);
        }
    }

    public void Toggle()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanel.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetMusicVolumeFromSlider(float value)
    {
        ApplyVolumeToMixer(musicParam, value);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    private void SetSfxVolumeFromSlider(float value)
    {
        ApplyVolumeToMixer(sfxParam, value);
        PlayerPrefs.SetFloat(SfxKey, value);
    }

    private void ApplyVolumeToMixer(string param, float sliderValue)
    {
        if (mixer == null)
        {
            return;
        }

        float clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f);
        float decibels = Mathf.Log10(clamped) * 20f;
        mixer.SetFloat(param, decibels);
    }

    private void TrySetPlayerInputLocked(bool locked)
    {
        System.Type type = System.Type.GetType("PlayerController");

        if (type == null)
        {
            return;
        }

        System.Reflection.FieldInfo field = type.GetField(
            "IsInputLocked",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static
        );

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(null, locked);
        }
    }
}