using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using InputDevice = UnityEngine.XR.InputDevice;

public class CaptureHandler : MonoBehaviour
{
    private ScreenshotCapture screenshotCapture;
    private InputDevice rightController;

    private void Start()
    {
        // Find the ScreenshotCapture component in the scene
        screenshotCapture = FindObjectOfType<ScreenshotCapture>();

        if (screenshotCapture == null)
        {
            Debug.LogError("[CaptureHandler] ScreenshotCapture component not found in scene!");
        }
        else
        {
            Debug.Log("[CaptureHandler] Successfully linked to ScreenshotCapture");
        }

        // Get the right controller device
        InitializeController();
    }

    private void InitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);

        if (devices.Count > 0)
        {
            rightController = devices[0];
            Debug.Log($"[CaptureHandler] Found right controller: {rightController.name}");
        }
        else
        {
            Debug.LogWarning("[CaptureHandler] Right controller not found yet, will retry...");
        }
    }

    private void Update()
    {
        // Ensure we have the controller
        if (!rightController.isValid)
        {
            InitializeController();
            return;
        }

        // Check for Button A (primaryButton)
        if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonPressed) && primaryButtonPressed)
        {
            Debug.Log("📸 [CaptureHandler] Button A PRESSED - Triggering capture!");
            TriggerCapture("Button A");
        }

        // Check for Button B (secondaryButton)
        if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonPressed) && secondaryButtonPressed)
        {
            Debug.Log("📸 [CaptureHandler] Button B PRESSED - Triggering capture!");
            TriggerCapture("Button B");
        }

        // Check for Trigger
        if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
        {
            Debug.Log("📸 [CaptureHandler] Trigger PRESSED - Triggering capture!");
            TriggerCapture("Trigger");
        }
    }

    private bool lastPrimaryState = false;
    private bool lastSecondaryState = false;
    private bool lastTriggerState = false;

    private void TriggerCapture(string buttonName)
    {
        // Prevent multiple triggers (debounce)
        bool currentPrimary = false, currentSecondary = false, currentTrigger = false;

        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out currentPrimary);
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out currentSecondary);
        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out currentTrigger);

        // Only trigger on button down (state change from false to true)
        bool shouldTrigger = false;
        if (buttonName == "Button A" && currentPrimary && !lastPrimaryState) shouldTrigger = true;
        if (buttonName == "Button B" && currentSecondary && !lastSecondaryState) shouldTrigger = true;
        if (buttonName == "Trigger" && currentTrigger && !lastTriggerState) shouldTrigger = true;

        lastPrimaryState = currentPrimary;
        lastSecondaryState = currentSecondary;
        lastTriggerState = currentTrigger;

        if (!shouldTrigger) return;

        if (screenshotCapture != null)
        {
            screenshotCapture.TriggerCapture($"VR {buttonName}");
        }
        else
        {
            Debug.LogError("[CaptureHandler] Cannot capture - ScreenshotCapture reference is null");
        }
    }
}
