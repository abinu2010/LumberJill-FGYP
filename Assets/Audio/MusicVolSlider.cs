using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MusicVolSlider : MonoBehaviour
{
    public AudioMixer mixer;
    public string parameter = "MusicVol";
    public Slider slider;

    void Start()
    {
        // Initialize
        if (slider != null)
            slider.onValueChanged.AddListener(SetVolume);
    }

    void SetVolume(float value)
    {
        // Convert linear 0–1 range to decibels (-80 dB to 0 dB)
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(parameter, dB);
    }
}
