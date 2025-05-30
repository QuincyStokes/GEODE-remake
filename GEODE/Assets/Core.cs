using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class Core : BaseObject, IInteractable
{
    public static Core CORE;
    public float buildRadius;
    [SerializeField] private Light2D areaLight;
    public event Action OnCorePlaced;

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }

    private void Start()
    {
        if (FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.SetCorePosition(transform);
            FlowFieldManager.Instance.CalculateFlowField();
            CORE = this;
            areaLight.pointLightOuterRadius = buildRadius + 1;
            areaLight.pointLightInnerRadius = buildRadius;
            OnCorePlaced?.Invoke();
        }
        else
        {
            Debug.Log("FlowFieldManager was not found.");
        }
    }
}
