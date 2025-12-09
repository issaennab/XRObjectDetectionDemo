using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("YOLO API Settings")]
    [Tooltip("Your Mac's local IP address (find it with: ifconfig | grep 'inet ')")]
    public string yoloServerIP = "127.0.0.1";  // Use 127.0.0.1 with adb reverse, or 192.168.2.50 for WiFi
    public int yoloServerPort = 8000;

    [Header("VR UI Display")]
    [Tooltip("Assign a TextMeshPro - Text component to display detection results in VR")]
    public TextMeshProUGUI detectionText;
    [Tooltip("Distance from camera to place the text panel (in meters)")]
    public float textDistance = 2.0f;
    [Tooltip("Auto-create text display if none assigned")]
    public bool autoCreateTextDisplay = true;

    // Input System actions for XR controllers
    public InputActionProperty rightControllerButtonA;
    public InputActionProperty rightControllerButtonB;
    public InputActionProperty rightControllerTrigger;

    private bool showMessage = false;
    private string uiMessage = "";
    private GameObject textCanvasObject;
    private bool isCapturing = false;  // Prevent multiple simultaneous captures

    void Start()
    {
        Debug.Log("[ScreenshotCapture] Ready. Press Button A, B, or Trigger on right controller to capture.");

        if (captureCamera == null)
        {
            Debug.LogWarning("[ScreenshotCapture] No camera assigned. Assign XR Main Camera!");
        }

        // Auto-create VR text display if needed
        if (autoCreateTextDisplay && detectionText == null)
        {
            CreateVRTextDisplay();
        }

        // Enable input actions
        rightControllerButtonA.action?.Enable();
        rightControllerButtonB.action?.Enable();
        rightControllerTrigger.action?.Enable();

        Debug.Log("[ScreenshotCapture] Input actions enabled");
    }

    void Update()
    {
        // Check XR Input System buttons
        if (rightControllerButtonA.action != null && rightControllerButtonA.action.WasPressedThisFrame())
        {
            Debug.Log("[ScreenshotCapture] RIGHT CONTROLLER BUTTON A detected");
            TriggerCapture("Right Controller Button A");
        }

        if (rightControllerButtonB.action != null && rightControllerButtonB.action.WasPressedThisFrame())
        {
            Debug.Log("[ScreenshotCapture] RIGHT CONTROLLER BUTTON B detected");
            TriggerCapture("Right Controller Button B");
        }

        if (rightControllerTrigger.action != null && rightControllerTrigger.action.WasPressedThisFrame())
        {
            Debug.Log("[ScreenshotCapture] RIGHT CONTROLLER TRIGGER detected");
            TriggerCapture("Right Controller Trigger");
        }

        // 1️⃣ Keyboard → C key to clear message OR capture if no message shown
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (showMessage)
            {
                Debug.Log("[ScreenshotCapture] C KEY detected - Clearing message");
                showMessage = false;
                uiMessage = "";

                // Hide VR text display
                if (textCanvasObject != null)
                {
                    textCanvasObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log("[ScreenshotCapture] C KEY detected - Capturing");
                TriggerCapture("Keyboard C");
            }
        }

        // 2️⃣ Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[ScreenshotCapture] MOUSE CLICK detected");
            TriggerCapture("Mouse Click");
        }
    }

    public void TriggerCapture(string source)
    {
        if (isCapturing)
        {
            Debug.Log($"[ScreenshotCapture] ⏸️ Capture already in progress, ignoring trigger from {source}");
            return;
        }

        Debug.Log($"[ScreenshotCapture] TRIGGER → {source}");
        Debug.Log("[ScreenshotCapture] Starting coroutine...");
        StartCoroutine(Capture());
    }

    IEnumerator Capture()
    {
        isCapturing = true;
        ShowMessage("📸 Analyzing...");

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
        Debug.Log($"[ScreenshotCapture] Image size being sent: {width}x{height}");
        Debug.Log($"[ScreenshotCapture] JPG file size: {jpegBytes.Length / 1024f:F2} KB ({jpegBytes.Length} bytes)");

        Destroy(tex);

        // Send to YOLO API (keeps "Analyzing..." message visible)
        StartCoroutine(SendToYoloAPI(jpegBytes));
    }

    IEnumerator SendToYoloAPI(byte[] imageBytes)
    {
        Debug.Log("[YOLO] ====== Starting YOLO API Request ======");
        string url = $"http://{yoloServerIP}:{yoloServerPort}/detect";
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

        // Allow insecure HTTP connections (needed for local development)
        request.certificateHandler = new AcceptAllCertificatesHandler();

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

        // Release the capture lock
        isCapturing = false;
    }

    void ShowMessage(string msg)
    {
        uiMessage = msg;
        showMessage = true;
        Debug.Log($"[UI] Showing message: {msg} (Press C to clear)");

        // Update VR text display
        if (detectionText != null)
        {
            detectionText.text = msg;
            if (textCanvasObject != null)
            {
                textCanvasObject.SetActive(true);
            }
        }
    }

    void CreateVRTextDisplay()
    {
        Debug.Log("[UI] Creating VR text display...");

        // Create Canvas GameObject
        textCanvasObject = new GameObject("DetectionTextCanvas");
        Canvas canvas = textCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Add CanvasScaler for better resolution
        UnityEngine.UI.CanvasScaler scaler = textCanvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Position canvas in front of camera
        textCanvasObject.transform.SetParent(captureCamera.transform, false);
        textCanvasObject.transform.localPosition = new Vector3(0, 0.3f, textDistance);
        textCanvasObject.transform.localRotation = Quaternion.identity;

        // Set canvas size (800x200 units in world space)
        RectTransform canvasRect = textCanvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 200);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // Scale down for VR

        // Create Text GameObject
        GameObject textObject = new GameObject("DetectionText");
        textObject.transform.SetParent(textCanvasObject.transform, false);

        // Add TextMeshProUGUI component
        detectionText = textObject.AddComponent<TextMeshProUGUI>();
        detectionText.text = "Ready to detect objects...";
        detectionText.fontSize = 48;
        detectionText.color = Color.green;
        detectionText.alignment = TextAlignmentOptions.Center;
        detectionText.textWrappingMode = TextWrappingModes.Normal;

        // Set text RectTransform to fill canvas
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);

        // Initially hide the canvas
        textCanvasObject.SetActive(false);

        Debug.Log("[UI] VR text display created successfully!");
    }

}

// Certificate handler to allow HTTP connections (for local development)
public class AcceptAllCertificatesHandler : UnityEngine.Networking.CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Accept all certificates (use only for local development!)
    }
}