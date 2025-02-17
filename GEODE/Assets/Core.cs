using UnityEngine;

public class Core : MonoBehaviour
{
    private void Start()
    {
        if(FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.SetCorePosition(transform);
            FlowFieldManager.Instance.CalculateFlowField();
        }
        else
        {
            Debug.Log("FlowFieldManager was not found.");
        }
    }
}
