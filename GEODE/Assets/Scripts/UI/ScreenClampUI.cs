using UnityEngine;

public class ScreenClampUI : MonoBehaviour
{
    private RectTransform rectTrans;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
    }
    private void OnEnable() {
        if(rectTrans==null){
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, 0, Screen.width),
                Mathf.Clamp(transform.position.y, 0, Screen.height),
                0);
        }
        else
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, 0+rectTrans.rect.width*1.5f, Screen.width-rectTrans.rect.width*1.5f),
                Mathf.Clamp(transform.position.y, 0+rectTrans.rect.height*.5f, Screen.height-rectTrans.rect.height*1.5f),
                0);
        }
    }
}
