    using UnityEngine;

    public class ScreenshotTaker : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) // Press 'P' to take a screenshot
            {
                ScreenCapture.CaptureScreenshot("GameScreenshot.png");
                Debug.Log("Screenshot taken!");
            }
        }
    }