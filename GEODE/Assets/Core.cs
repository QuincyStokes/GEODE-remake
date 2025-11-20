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

    public static event Action OnCorePlacedStatic;

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

    protected override void Start()
    {
        base.Start();
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
            DestroyThisServerRpc(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void DestroyThisServerRpc(bool dropItems)
    {
        if (!IsServer)
        {
            return;
        }
        NotifyClientsCoreDestroyedClientRpc();
        // Call the internal method directly to avoid RPC recursion
        DestroyThisInternal(dropItems);
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
        OnCorePlacedStatic?.Invoke();
    }

    public void DoClickedThings()
    {
        
    }

    public void DoUnclickedThings()
    {
        
    }
}
