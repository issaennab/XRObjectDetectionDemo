using UnityEngine;
using System.Collections;
using System.IO;

[System.Serializable]
public class ImageRequest
{
    public string image_base64;
    public ImageRequest(string img) { image_base64 = img; }
}

[System.Serializable]
public class Detection
{
    public string @class;
    public float confidence;
    public float x1, y1, x2, y2;
}

[System.Serializable]
public class DetectionResponse
{
    public Detection[] detections;
    public int image_width;
    public int image_height;
}

public class ScreenshotCapture : MonoBehaviour
{
    public Camera captureCamera;
    public bool saveToDisk = false;

    private float messageTimer = 0f;
    private string uiMessage = "";

    void Start()
    {
        Debug.Log("[ScreenshotCapture] Ready. Press C, Mouse Click, Right Trigger, or A Button (Quest) to capture.");

        if (captureCamera == null)
        {
            Debug.LogWarning("[ScreenshotCapture] No camera assigned. Assign XR Main Camera!");
        }
    }

    void Update()
    {
        // 1️⃣ Keyboard → C key for capture
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[ScreenshotCapture] C KEY detected");
            TriggerCapture("Keyboard C");
        }

        // 2️⃣ Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[ScreenshotCapture] MOUSE CLICK detected");
            TriggerCapture("Mouse Click");
        }

        // 3️⃣ Quest 3 trigger (primary index trigger)
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            Debug.Log("[ScreenshotCapture] QUEST R INDEX TRIGGER detected");
            TriggerCapture("Quest R Index Trigger");
        }

        // 4️⃣ Also support Oculus mapped "Button.One" (A button)
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            Debug.Log("[ScreenshotCapture] QUEST A BUTTON detected");
            TriggerCapture("Quest A Button");
        }

        // Handle UI fade timer
        if (messageTimer > 0f)
            messageTimer -= Time.deltaTime;
    }

    void TriggerCapture(string source)
    {
        Debug.Log($"[ScreenshotCapture] TRIGGER → {source}");
        Debug.Log("[ScreenshotCapture] Starting coroutine...");
        StartCoroutine(Capture());
    }

    IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();

        Debug.Log("[ScreenshotCapture] Beginning capture...");

        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = new RenderTexture(width, height, 24);
        captureCamera.targetTexture = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        captureCamera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        Debug.Log("[ScreenshotCapture] Screenshot captured!");
        Debug.Log($"[ScreenshotCapture] Texture size: {width}x{height}");

        if (saveToDisk)
        {
            string path = Application.dataPath + "/screenshot_" + System.DateTime.Now.Ticks + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[ScreenshotCapture] Saved to → " + path);
        }

        byte[] jpegBytes = tex.EncodeToJPG(60);
        Debug.Log("[ScreenshotCapture] JPG byte size → " + jpegBytes.Length);

        Destroy(tex);

        ShowMessage("Screenshot Captured ✓");

        // Send to YOLO API
        StartCoroutine(SendToYoloAPI(jpegBytes));
    }

    IEnumerator SendToYoloAPI(byte[] imageBytes)
    {
        Debug.Log("[YOLO] ====== Starting YOLO API Request ======");
        string url = "http://127.0.0.1:8000/detect";
        Debug.Log($"[YOLO] Target URL: {url}");
        Debug.Log($"[YOLO] Input image bytes length: {imageBytes.Length}");

        // Convert image to Base64
        Debug.Log("[YOLO] Converting image to Base64...");
        string base64Image = System.Convert.ToBase64String(imageBytes);
        Debug.Log($"[YOLO] Base64 string length: {base64Image.Length}");
        Debug.Log($"[YOLO] Base64 preview (first 100 chars): {base64Image.Substring(0, Mathf.Min(100, base64Image.Length))}...");

        // Build JSON
        Debug.Log("[YOLO] Building JSON payload...");
        string jsonPayload = JsonUtility.ToJson(new ImageRequest(base64Image));
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        Debug.Log($"[YOLO] JSON payload size: {jsonBytes.Length} bytes");
        Debug.Log($"[YOLO] JSON preview (first 200 chars): {jsonPayload.Substring(0, Mathf.Min(200, jsonPayload.Length))}...");

        Debug.Log("[YOLO] Creating UnityWebRequest...");
        UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST");

        Debug.Log("[YOLO] Setting upload handler...");
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(jsonBytes);
        Debug.Log("[YOLO] Setting download handler...");
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        Debug.Log("[YOLO] Setting Content-Type header...");
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("[YOLO] Sending POST request to YOLO API...");
        float startTime = Time.time;
        yield return request.SendWebRequest();
        float endTime = Time.time;
        Debug.Log($"[YOLO] Request completed in {(endTime - startTime):F2} seconds");

        Debug.Log($"[YOLO] Response Code: {request.responseCode}");
        Debug.Log($"[YOLO] Result: {request.result}");

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log("[YOLO] ✓ SUCCESS!");
            Debug.Log($"[YOLO] Response length: {request.downloadHandler.text.Length} characters");
            Debug.Log("[YOLO] API Response: " + request.downloadHandler.text);

            // Parse detection response
            try
            {
                var resp = JsonUtility.FromJson<DetectionResponse>(request.downloadHandler.text);

                if (resp != null && resp.detections != null && resp.detections.Length > 0)
                {
                    Debug.Log($"[YOLO] Parsed {resp.detections.Length} detections");
                    Debug.Log($"[YOLO] Image dimensions: {resp.image_width}x{resp.image_height}");

                    string summary = "Detected: ";
                    foreach (var det in resp.detections)
                    {
                        summary += $"{det.@class} ({(det.confidence * 100f):F0}%), ";
                        Debug.Log($"[YOLO]   - {det.@class}: {(det.confidence * 100f):F1}% at [{det.x1:F0}, {det.y1:F0}, {det.x2:F0}, {det.y2:F0}]");
                    }

                    summary = summary.TrimEnd(',', ' ');
                    Debug.Log("[YOLO SUMMARY] " + summary);
                    ShowMessage(summary);
                }
                else
                {
                    Debug.Log("[YOLO] No detections in response");
                    ShowMessage("No Objects Detected");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[YOLO] Failed to parse response: {e.Message}");
                ShowMessage("Detection Parse Error");
            }
        }
        else
        {
            Debug.LogError("[YOLO] ✗ REQUEST FAILED!");
            Debug.LogError($"[YOLO] Error Type: {request.result}");
            Debug.LogError($"[YOLO] Error Message: {request.error}");
            Debug.LogError($"[YOLO] Response Code: {request.responseCode}");

            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Debug.LogError($"[YOLO] Error Response Body: {request.downloadHandler.text}");
            }

            ShowMessage("Detection Failed - Check Logs");
        }

        Debug.Log("[YOLO] ====== YOLO API Request Complete ======");
        request.Dispose();
    }

    void ShowMessage(string msg)
    {
        uiMessage = msg;
        messageTimer = 3f;  // Extended to 3 seconds for detection results
        Debug.Log($"[UI] Showing message: {msg}");
    }

    void OnGUI()
    {
        if (messageTimer > 0f)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;  // Slightly smaller to fit longer detection messages
            style.normal.textColor = Color.green;  // Green for better visibility
            style.alignment = TextAnchor.UpperCenter;
            style.wordWrap = true;  // Allow text wrapping for long detection lists

            GUI.Label(new Rect(10, 20, Screen.width - 20, 100), uiMessage, style);
        }
    }
}