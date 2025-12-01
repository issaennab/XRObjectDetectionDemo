using UnityEngine;

public class XRSetup : MonoBehaviour
{
    // Store references to our objects
    private GameObject[] sceneObjects;
    private string[] objectNames = { "Red Cube", "Blue Sphere", "Green Cylinder", "Yellow Capsule", "Magenta Cube" };

    void Start()
    {
        Debug.Log("🚀 XRSetup: Creating VR scene with interactive objects");

        // Create array to hold our objects
        sceneObjects = new GameObject[5];

        // Create objects in an arc in front of the player
        CreateRedCube(0);
        CreateBlueSphere(1);
        CreateGreenCylinder(2);
        CreateYellowCapsule(3);
        CreateMagentaCube(4);

        // Create a floor for spatial reference
        CreateFloor();

        Debug.Log("✅ Scene created! Look around to see 5 colored objects");
        Debug.Log("👉 Point your controller (press T) at objects to see them highlight");
    }

    void CreateRedCube(int index)
    {
        sceneObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sceneObjects[index].name = objectNames[index];
        sceneObjects[index].transform.position = new Vector3(-1.5f, 1.5f, 2f);
        sceneObjects[index].transform.localScale = Vector3.one * 0.3f;
        sceneObjects[index].GetComponent<Renderer>().material.color = Color.red;

        // Add interactable component
        var interactable = sceneObjects[index].AddComponent<RayInteractableObject>();
        interactable.objectName = objectNames[index];
        interactable.originalColor = Color.red;
    }

    void CreateBlueSphere(int index)
    {
        sceneObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sceneObjects[index].name = objectNames[index];
        sceneObjects[index].transform.position = new Vector3(-0.75f, 1.5f, 2.5f);
        sceneObjects[index].transform.localScale = Vector3.one * 0.3f;
        sceneObjects[index].GetComponent<Renderer>().material.color = Color.blue;

        var interactable = sceneObjects[index].AddComponent<RayInteractableObject>();
        interactable.objectName = objectNames[index];
        interactable.originalColor = Color.blue;
    }

    void CreateGreenCylinder(int index)
    {
        sceneObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sceneObjects[index].name = objectNames[index];
        sceneObjects[index].transform.position = new Vector3(0f, 1.5f, 3f);
        sceneObjects[index].transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
        sceneObjects[index].GetComponent<Renderer>().material.color = Color.green;

        var interactable = sceneObjects[index].AddComponent<RayInteractableObject>();
        interactable.objectName = objectNames[index];
        interactable.originalColor = Color.green;
    }

    void CreateYellowCapsule(int index)
    {
        sceneObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        sceneObjects[index].name = objectNames[index];
        sceneObjects[index].transform.position = new Vector3(0.75f, 1.5f, 2.5f);
        sceneObjects[index].transform.localScale = Vector3.one * 0.3f;
        sceneObjects[index].GetComponent<Renderer>().material.color = Color.yellow;

        var interactable = sceneObjects[index].AddComponent<RayInteractableObject>();
        interactable.objectName = objectNames[index];
        interactable.originalColor = Color.yellow;
    }

    void CreateMagentaCube(int index)
    {
        sceneObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sceneObjects[index].name = objectNames[index];
        sceneObjects[index].transform.position = new Vector3(1.5f, 1.5f, 2f);
        sceneObjects[index].transform.localScale = Vector3.one * 0.3f;
        sceneObjects[index].GetComponent<Renderer>().material.color = Color.magenta;

        var interactable = sceneObjects[index].AddComponent<RayInteractableObject>();
        interactable.objectName = objectNames[index];
        interactable.originalColor = Color.magenta;
    }

    void CreateFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = Vector3.one * 2;
        floor.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
    }
}

// Component that makes objects respond to controller rays
public class RayInteractableObject : MonoBehaviour
{
    public string objectName;
    public Color originalColor;
    private Renderer rend;
    private bool isHovered = false;
    private LineRenderer leftRay;
    private LineRenderer rightRay;

    void Start()
    {
        rend = GetComponent<Renderer>();
        Debug.Log($"🎯 RayInteractableObject Start() called for: {objectName}");

        // Create ray visualizers on first object only
        if (objectName == "Red Cube")
        {
            Debug.Log("🔧 Red Cube detected - attempting to create ray visualizers");
            CreateRayVisualizers();
        }
    }

    void CreateRayVisualizers()
    {
        Debug.Log("🔍 Searching for controller anchors...");

        // Find controller anchors
        GameObject leftController = GameObject.Find("LeftControllerAnchor");
        GameObject rightController = GameObject.Find("RightControllerAnchor");

        Debug.Log($"🎮 LeftControllerAnchor found: {leftController != null}");
        Debug.Log($"🎮 RightControllerAnchor found: {rightController != null}");

        if (leftController != null)
        {
            Debug.Log("➕ Adding LineRenderer to left controller");
            leftRay = leftController.AddComponent<LineRenderer>();
            SetupLineRenderer(leftRay);
            Debug.Log("✅ Left ray visualizer created");
        }
        else
        {
            Debug.LogWarning("⚠️ Left controller not found!");
        }

        if (rightController != null)
        {
            Debug.Log("➕ Adding LineRenderer to right controller");
            rightRay = rightController.AddComponent<LineRenderer>();
            SetupLineRenderer(rightRay);
            Debug.Log("✅ Right ray visualizer created");
        }
        else
        {
            Debug.LogWarning("⚠️ Right controller not found!");
        }

        Debug.Log("✨ Ray visualizers setup complete!");
    }

    void SetupLineRenderer(LineRenderer lr)
    {
        Debug.Log("🎨 Setting up LineRenderer properties");
        lr.startWidth = 0.005f;
        lr.endWidth = 0.005f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.blue;
        lr.positionCount = 2;
        Debug.Log($"📏 LineRenderer configured: width={lr.startWidth}, positions={lr.positionCount}");
    }

    void Update()
    {
        // Only log from Red Cube to avoid spam
        if (objectName == "Red Cube")
        {
            LogRayStatus();
        }

        // Update ray visualization
        UpdateRayVisuals();

        // Check for ray hits
        CheckControllerRay();
    }

    void UpdateRayVisuals()
    {
        if (leftRay != null)
        {
            Transform controller = leftRay.transform;
            leftRay.SetPosition(0, controller.position);
            leftRay.SetPosition(1, controller.position + controller.forward * 10f);
        }

        if (rightRay != null)
        {
            Transform controller = rightRay.transform;
            rightRay.SetPosition(0, controller.position);
            rightRay.SetPosition(1, controller.position + controller.forward * 10f);
        }
    }

    private float lastLogTime = 0f;
    void LogRayStatus()
    {
        // Log every 2 seconds to avoid spam
        if (Time.time - lastLogTime > 2f)
        {
            Debug.Log($"🔄 Ray Update - Left: {leftRay != null}, Right: {rightRay != null}");
            if (rightRay != null)
            {
                Debug.Log($"📍 Right controller pos: {rightRay.transform.position}, forward: {rightRay.transform.forward}");
            }
            lastLogTime = Time.time;
        }
    }

    void CheckControllerRay()
    {
        bool hitByRay = false;

        // Try to find controller transforms
        GameObject[] controllers = new GameObject[]
        {
            GameObject.Find("LeftControllerAnchor"),
            GameObject.Find("RightControllerAnchor"),
            GameObject.Find("LeftHandAnchor"),
            GameObject.Find("RightHandAnchor")
        };

        int foundControllers = 0;
        foreach (var c in controllers) if (c != null) foundControllers++;

        foreach (var controller in controllers)
        {
            if (controller != null)
            {
                // Cast ray forward from controller
                Ray ray = new Ray(controller.transform.position, controller.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 10f))
                {
                    // Log when ANY object is hit (helps debug raycasting)
                    if (objectName == "Red Cube" && Time.frameCount % 60 == 0) // Log once per second
                    {
                        Debug.Log($"🎯 Raycast hit something: {hit.collider.gameObject.name} at distance {hit.distance:F2}m");
                    }

                    if (hit.collider.gameObject == gameObject)
                    {
                        hitByRay = true;

                        // Change color when ray hits
                        if (!isHovered)
                        {
                            rend.material.color = Color.white;
                            isHovered = true;
                            Debug.Log($"👉 Ray hovering: {objectName}");
                        }

                        // Check for trigger press
                        bool triggerPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

                        // Check OVR trigger
                        try
                        {
                            triggerPressed = triggerPressed ||
                                           OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
                                           OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
                        }
                        catch { }

                        if (triggerPressed)
                        {
                            OnSelected();
                        }
                    }
                }
            }
        }

        // Reset color if no ray is hitting
        if (!hitByRay && isHovered)
        {
            rend.material.color = originalColor;
            isHovered = false;
        }
    }

    void OnSelected()
    {
        Debug.Log($"✨ SELECTED: {objectName}!");
        // Make it jump
        transform.position += Vector3.up * 0.2f;
    }
}
