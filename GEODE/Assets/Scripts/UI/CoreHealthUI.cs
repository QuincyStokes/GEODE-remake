using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreHealthUI : MonoBehaviour
{
    [SerializeField] private GameObject coreHealthUI;
    [SerializeField] private Slider coreHealthSlider;
    [SerializeField] private TMP_Text corehealthText;

    private void Start()
    {
        DayCycleManager.Instance.becameDay += HandleBecameDay;
        DayCycleManager.Instance.becameNight += HandleBecameNight;

        Core.OnCorePlacedStatic += HandleCorePlaced;

        coreHealthUI.SetActive(false);
    }

    private void HandleCorePlaced()
    {   
        Core.CORE.CurrentHealth.OnValueChanged += HandleCoreHealthChanged;

        coreHealthSlider.maxValue = Core.CORE.MaxHealth.Value;
        coreHealthSlider.value = Core.CORE.CurrentHealth.Value;
    }

    private void HandleCoreHealthChanged(float previousValue, float newValue)
    {
        coreHealthSlider.value = newValue;
        corehealthText.text = $"{newValue}/{Core.CORE.MaxHealth.Value}";
    }

    private void HandleBecameDay()
    {
        coreHealthUI.SetActive(false);
    }

    private void HandleBecameNight()
    {
        coreHealthUI.SetActive(true);
    }




}
