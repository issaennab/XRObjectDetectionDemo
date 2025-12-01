# XR Object Detection Demo

A Unity XR prototype that captures frames from the VR camera and sends them to a YOLO FastAPI server for object detection. Used for experimenting with real-time detection inside a 3D environment and for building custom datasets from Unity scenes.

## Features

- 📸 **Automatic or manual camera screenshot capture**
- 🔍 **Sends images to YOLO API** (`/detect`) for real-time object detection
- 🎯 **Displays detection results inside Unity** with confidence scores
- 🥽 **Works with XR Interaction Toolkit & Meta Quest**
- 🎮 **Multiple input methods**: Keyboard (C key), Mouse Click, Right Trigger, A Button (Quest)
- 💾 **Optional disk saving** for captured frames
- 🖼️ **Simple UI overlay** for detection messages and status

## Project Structure

```
XRObjectDetectionDemo/
├── Assets/
│   ├── Scripts/
│   │   ├── ScreenshotCapture.cs    # Main capture & API integration script
│   │   └── TestRequest.cs          # API testing utility
│   ├── Scenes/                     # Unity scenes
│   ├── Resources/                  # UI and prefab resources
│   ├── Settings/                   # XR and project settings
│   └── MetaXR/                     # Meta Quest SDK integration
├── ProjectSettings/                # Unity project configuration
├── Packages/                       # Unity package dependencies
└── README.md
```

## Requirements

### Unity

- **Unity 2022.3 LTS or later**
- **XR Interaction Toolkit** (installed via Package Manager)
- **Oculus XR Plugin** or **OpenXR Plugin** (for Meta Quest)
- **TextMeshPro** (for UI text rendering)

### Backend API

- **YOLO FastAPI server** running and accessible
- Endpoint: `POST /detect`
- Expected request format: `{"image_base64": "..."}`
- Expected response format:
  ```json
  {
    "detections": [
      {
        "class": "person",
        "confidence": 0.95,
        "x1": 100,
        "y1": 150,
        "x2": 300,
        "y2": 450
      }
    ],
    "image_width": 1920,
    "image_height": 1080
  }
  ```

## Setup

### 1. Clone the Repository

```bash
git clone https://github.com/issaennab/XRObjectDetectionDemo.git
cd XRObjectDetectionDemo
```

### 2. Open in Unity

1. Open Unity Hub
2. Click **Add** → Select the `XRObjectDetectionDemo` folder
3. Open the project (Unity will import packages automatically)

### 3. Configure the Scene

1. Open the main scene: `Assets/SampleScene.unity`
2. Locate the **ScreenshotCapture** GameObject in the Hierarchy
3. In the Inspector, assign:
   - **Capture Camera**: Drag your XR Main Camera here
   - **API URL**: Set to your YOLO server (e.g., `http://localhost:8000/detect`)
   - **Save To Disk**: Check if you want to save captured images locally

### 4. Set Up XR

1. Go to **Edit → Project Settings → XR Plug-in Management**
2. Enable **Oculus** (for Quest) or **OpenXR**
3. Configure build settings for Android (Meta Quest) or your target platform

### 5. Run the YOLO Backend

Ensure your YOLO FastAPI server is running:

```bash
python app.py  # or however you start your YOLO server
```

## Usage

### In Unity Editor

1. Press **Play** in Unity Editor
2. Press **C** on the keyboard to capture a frame
3. Check the Console for detection results
4. UI overlay will show detected objects and confidence scores

### On Meta Quest

1. Build and deploy to Quest via **File → Build Settings**
2. Wear the headset and launch the app
3. Use these controls:
   - **Right Trigger**: Capture frame
   - **A Button**: Capture frame
   - Mouse click also works in passthrough mode

### Detection Output

When objects are detected, you'll see:

- Console logs with detailed bounding boxes
- UI overlay showing: `"Detected: person (95%), car (87%)"`
- Messages persist for 3 seconds before fading

## Key Scripts

### `ScreenshotCapture.cs`

Main script handling:

- Camera frame capture using `RenderTexture`
- Base64 encoding of PNG images
- HTTP POST requests to YOLO API
- Parsing detection responses
- UI message display

**Key Methods:**

- `CaptureAndSend()`: Main capture workflow
- `SendImageToAPI()`: Handles API communication
- `OnGUI()`: Displays detection results overlay

### `TestRequest.cs`

Simple utility for testing API connectivity without XR setup.

## API Integration Details

The script sends images as JSON:

```csharp
{
  "image_base64": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

And expects this response:

```csharp
{
  "detections": [
    {
      "class": "object_name",
      "confidence": 0.95,
      "x1": 100, "y1": 150,
      "x2": 300, "y2": 450
    }
  ],
  "image_width": 1920,
  "image_height": 1080
}
```

## Configuration

Edit `ScreenshotCapture.cs` to customize:

```csharp
public string apiUrl = "http://localhost:8000/detect";  // YOLO server URL
public bool saveToDisk = false;                          // Save images locally
private int captureWidth = 1920;                         // Screenshot resolution
private int captureHeight = 1080;
```

## Troubleshooting

### "No camera assigned" warning

- Assign the **XR Origin → Camera Offset → Main Camera** to the `captureCamera` field

### API connection fails

- Verify YOLO server is running: `curl http://localhost:8000/health`
- Check firewall settings if using external device (Quest needs network access to PC)
- Use your PC's local IP instead of `localhost` when running on Quest

### Low frame rate

- Reduce capture resolution in `ScreenshotCapture.cs`
- Implement frame skipping (capture every N frames instead of every press)

### No detections

- Verify YOLO model is loaded correctly on server
- Check server logs for errors
- Test with a simple image containing known objects

## Future Enhancements

- [ ] Real-time detection (stream frames continuously)
- [ ] Draw bounding boxes in 3D space using Unity Gizmos
- [ ] Dataset recording mode (auto-save frames + labels)
- [ ] Multiple camera support
- [ ] Performance metrics (FPS, latency)
- [ ] Custom YOLO model selection via UI
- [ ] Local inference using ONNX Runtime

## License

MIT License - feel free to use for research and experimentation.

## Contributing

Pull requests welcome! Please open an issue first to discuss proposed changes.

## Contact

Created by [@issaennab](https://github.com/issaennab)

---

**Built with Unity XR Toolkit, Meta Quest SDK, and YOLO v8+**
