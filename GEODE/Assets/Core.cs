using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Core : BaseObject
{
    public static Core CORE;
    public float buildRadius;
    [SerializeField] private Light2D areaLight;
    private void Start()
    {
        if(FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.SetCorePosition(transform);
            FlowFieldManager.Instance.CalculateFlowField();
            CORE = this;
            areaLight.pointLightOuterRadius = buildRadius+1;
            areaLight.pointLightInnerRadius = buildRadius;
        }
        else
        {
            Debug.Log("FlowFieldManager was not found.");
        }
    }
}
