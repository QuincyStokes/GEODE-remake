    using UnityEngine;

    public class ScreenshotTaker : MonoBehaviour
    {
        int counter;
        string _path = @"C:\Users\quinc\Documents\GEODE\Screenshots";

        
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) // Press 'P' to take a screenshot
            {
                string _file = $"geode_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                string fullPath = System.IO.Path.Combine(_path, _file);
                ScreenCapture.CaptureScreenshot(fullPath);
                Debug.Log("Screenshot taken!");
                Debug.Log("Saved to: " + fullPath);
            }
        }
    }