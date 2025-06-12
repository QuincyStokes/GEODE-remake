using UnityEngine;
using UnityEngine.UI;
public class SimpleToggle : MonoBehaviour
{
    public Image image;
    public bool isOn;
    public Sprite onImage;
    public Sprite offImage;

    public void Toggle()
    {
        isOn = !isOn;
        ChangeImage();
    }

    public void SetToggle(bool toggle)
    {
        isOn = toggle;
        ChangeImage();
    }



    private void ChangeImage()
    {
        if (isOn)
            image.sprite = onImage;
        else
            image.sprite = offImage;
    }


}
