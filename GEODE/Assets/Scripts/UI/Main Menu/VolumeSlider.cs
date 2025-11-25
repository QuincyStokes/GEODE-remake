using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private string parameter;

    private void Start()
    {
        AudioManager.Instance.MasterMixer.GetFloat(parameter, out float volume);
        slider.value = volume;
    }

    public void OnSliderValueChanged()
    {
        AudioManager.Instance.ChangeVolume(parameter, slider.value);
    }
}
