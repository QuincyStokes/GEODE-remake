using Unity.Cinemachine;
using UnityEngine;

public class CameraWorldConfiner : MonoBehaviour
{
    public static CameraWorldConfiner Instance;
    [SerializeField] private BoxCollider2D cameraBoundary;
    [SerializeField] private CinemachineConfiner2D confiner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


    }

    public void SetCameraBoundary()
    {
        if (WorldGenManager.Instance != null)
        {
            cameraBoundary.size = new Vector2(WorldGenManager.Instance.WorldSizeX, WorldGenManager.Instance.WorldSizeY);
            transform.position = new Vector2(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2);
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
