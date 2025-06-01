using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class Core : BaseObject, IInteractable
{
    public static Core CORE;
    public float buildRadius;
    [SerializeField] private Light2D areaLight;
    public event Action OnCorePlaced;
    public event Action OnCoreDestroyed;

    private void Awake()
    {
        CORE = this;
    }
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
            areaLight.pointLightOuterRadius = buildRadius + 1;
            areaLight.pointLightInnerRadius = buildRadius;
            NotifyClientsCorePlacedClientRpc();
        }
        else
        {
            Debug.Log("FlowFieldManager was not found.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            DestroyThis(true);
        }
    }

    public override void DestroyThis(bool dropItems)
    {
        NotifyClientsCoreDestroyedClientRpc();
        base.DestroyThis(true);

    }

    [ClientRpc]
    private void NotifyClientsCoreDestroyedClientRpc()
    {
        OnCoreDestroyed?.Invoke();
    }

    [ClientRpc]
    private void NotifyClientsCorePlacedClientRpc()
    {
        OnCorePlaced?.Invoke();
    }
}
