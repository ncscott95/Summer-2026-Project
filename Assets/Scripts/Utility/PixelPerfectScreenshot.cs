using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class PixelPerfectScreenshot : MonoBehaviour
{
    [SerializeField] private string fileName = "PixelPerfectScreenshot.png";

    void Update()
    {
        // Trigger screenshot with the 'K' key
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            StartCoroutine(CaptureScreenshotCoroutine());
        }
    }

    private IEnumerator CaptureScreenshotCoroutine()
    {
        // Wait until all rendering is finished for this frame
        yield return new WaitForEndOfFrame();

        // Get actual render resolution (Screen size)
        int width = Screen.width;
        int height = Screen.height;

        // Create a texture matching the screen dimensions
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Set filter mode to Point to keep raw, crisp pixels if upscaling
        screenshot.filterMode = FilterMode.Point; 

        // Read screen pixels into the texture
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // Encode to PNG bytes
        byte[] bytes = screenshot.EncodeToPNG();
        Destroy(screenshot); // Clean up memory immediately

        // Ensure the filename is unique
        string baseFileName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        if (File.Exists(Path.Combine(Application.persistentDataPath, fileName)))
        {
            // If the file already exists, append a number to the filename
            int count = 1;
            fileName = $"{baseFileName}({count}){extension}";
            while (File.Exists(Path.Combine(Application.persistentDataPath, fileName)))
            {
                count++;
                fileName = $"{baseFileName}({count}){extension}";
            }
        }

        // Define saving path
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, bytes);

        Debug.Log($"Pixel Perfect Screenshot saved to: {path}");
    }
}
