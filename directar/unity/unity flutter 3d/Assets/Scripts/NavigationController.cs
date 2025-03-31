using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using FlutterUnityIntegration;
using TMPro;
using Dummiesman;
using Newtonsoft.Json;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;

[Serializable]
public class SceneData
{
    public List<SceneObjectData> objects;

    public CameraData cameraData;
}

[Serializable]
public class SceneObjectData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string type;
    public string label;
    public bool isSource;
    public bool isDestination;
    public string name;
}

[System.Serializable]
public class CameraData
{
    public Vector3 position;
    public Quaternion rotation;
    public float fieldOfView;
}

/// <summary>
/// Controls AR navigation, managing waypoints, line rendering, and directional guidance.
/// </summary>
public class NavigationController : MonoBehaviour
{
    [SerializeField] private UnityMessageSender unityMessageSender;
    public GameObject xrOrigin; // XR Origin GameObject
    public GameObject navigationPointPrefab; // Prefab for navigation points destination
    public LineRenderer pathLine; // LineRenderer for navigation
    private OBJLoader objLoader = new OBJLoader();
    private Dictionary<string, Transform> destinationPoints = new Dictionary<string, Transform>();
    private string defaultDestination;
    private GameObject sceneRoot;
    private int currentPathIndex = 0;
    private string lastInstruction = "";
    private float instructionCooldown = 3.0f; // Prevents instructions from repeating too often
    private string lastSpokenInstruction = "";
    private readonly float destinationThreshold = 1.3f;
    private float minMoveDistance = 0.1f;
    private bool shouldRecalculatePath = true;
    private Vector3 lastUserPosition = Vector3.zero;
    private DateTime lastInstructionTime = DateTime.MinValue;
    public Material redMaterial;
    private Vector3 userPosition;
    [SerializeField] private ARAnchorManager anchorManager;
    private Vector3[] lastValidPathCorners;
    private bool hasSpokenInitialInstruction = false;
    private bool hasReachedDestination = false;
    private float stopDuration = 1f;
    private Vector3 lastCheckedPositionXZ;
    private float lastCheckedTime;
    private float userStopThreshold = 0.05f;
    public FoxWalk fox;
    private Material transparentMaterial;

    /// <summary>
    /// Initializes the AR scene and configures settings for Android.
    /// </summary>
    void Start()
    {
        #if UNITY_ANDROID
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.clear;
            Camera.main.cullingMask = LayerMask.GetMask("AR Content", "UI"); // Show only AR layers
            sceneRoot = new GameObject("SceneRoot");
            Input.multiTouchEnabled = false;
        #endif
    }

    /// <summary>
    /// Updates the navigation path when the user moves significantly.
    /// </summary>
    void Update()
    {
        if (xrOrigin == null || string.IsNullOrEmpty(defaultDestination)) return;
        
        // Get the AR Camera
        Camera mainCamera = Camera.main; 

        // Use the camera's position
        userPosition = mainCamera.transform.position;

        if (destinationPoints.TryGetValue(defaultDestination, out Transform target))
        {
            if (((userPosition - lastUserPosition).sqrMagnitude > minMoveDistance * minMoveDistance) 
                || shouldRecalculatePath 
                || (currentPathIndex < lastValidPathCorners.Length - 2 
                    && Vector3.Angle(lastValidPathCorners[currentPathIndex] - userPosition, 
                                    lastValidPathCorners[currentPathIndex + 1] - lastValidPathCorners[currentPathIndex]) > 20))
            {
                ShowNavigationPath(target.position);
                lastUserPosition = userPosition;
                shouldRecalculatePath = false;
            }
        }
    }

    /// <summary>
    /// Imports a scene from a base64-encoded ZIP file.
    /// </summary>
    /// <param name="base64String">Base64 string containing the ZIP file.</param>
    public void ImportSceneFromBase64ForNavigationLine(string base64String)
    {
        try
        {
            // Decode and extract the zip
            string tempZipPath = Path.Combine(Application.persistentDataPath, "TempScene.zip");
            File.WriteAllBytes(tempZipPath, Convert.FromBase64String(base64String));
            ImportScene(tempZipPath);
            File.Delete(tempZipPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import scene: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts and processes a ZIP file containing 3D models and navigation data.
    /// </summary>
    /// <param name="zipFilePath">Path to the ZIP file.</param>
    public void ImportScene(string zipFilePath)
    {
        string tempFolder = Path.Combine(Application.persistentDataPath, Guid.NewGuid().ToString());

        try
        {
            // Create the temporary directory
            Directory.CreateDirectory(tempFolder);
            // Extract the zip file
            ZipFile.ExtractToDirectory(zipFilePath, tempFolder);
            // Check for sceneData.json
            string jsonPath = Path.Combine(tempFolder, "sceneData.json");
            string json = File.ReadAllText(jsonPath);
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

            Vector3 sceneStartPosition = Vector3.zero;
            Quaternion sceneStartRotation = Quaternion.identity;

            foreach (var objData in sceneData.objects)
            {
                if (objData.type == "NavigationLine" && objData.isSource == true)
                {
                    sceneStartPosition = objData.position;
                    sceneStartRotation = objData.rotation;
                    // SetupNavigationPoint(objData);
                    xrOrigin.SetActive(true);
                } else if (objData.type == "NavigationLine" && objData.isDestination == true)
                {
                    GameObject navPoint = SetupNavigationPoint(objData);
                    destinationPoints.Add(objData.label, navPoint.transform);
                }
                else
                {
                    Import3DModel(tempFolder, objData);
                }
            }

            SendDestinationLabelsToFlutter();   
            AdjustScenePosition(sceneStartPosition, sceneStartRotation);

            // Bake after a short delay to ensure all objects are included
            Invoke(nameof(BakeNavMesh), 2.0f);
            // Delete temp folder after baking NavMesh
            StartCoroutine(DeleteTempFolderAfterNavMesh(tempFolder));
            fox.PositionFox();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error importing scene: {ex.Message}");
        }
    }

    private void AdjustScenePosition(Vector3 sceneStartPosition, Quaternion sceneStartRotation)
    {
        // Get the XR Origin's REAL-WORLD starting position (from AR tracking)
        Vector3 xrOriginStartPosition = xrOrigin.transform.position;
        Quaternion xrOriginStartRotation = xrOrigin.transform.rotation;

        // Calculate offset between AR camera and source prefab's admin position
        Vector3 positionOffset = xrOriginStartPosition - sceneStartPosition;
        Quaternion rotationOffset = Quaternion.Inverse(sceneStartRotation) * xrOriginStartRotation;

        // Apply offset to all scene objects (models, destinations, etc.)
        foreach (Transform obj in sceneRoot.transform)
        {
            obj.position += positionOffset;
            obj.rotation = rotationOffset * obj.rotation;
        }
    }

    /// <summary>
    /// Creates and sets up a navigation point in the scene.
    /// </summary>
    /// <param name="objData">Scene object data.</param>
    /// <returns>The created navigation point GameObject.</returns>
    private GameObject SetupNavigationPoint(SceneObjectData objData)
    {
        GameObject navPoint;
        if (objData.isSource) 
        {
            navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
        } else {
            navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
        }

        // Ensure it remains anchored in AR
        AttachAnchor(navPoint, objData.position, objData.rotation);

        navPoint.transform.SetParent(sceneRoot.transform, true);
        navPoint.transform.localScale = objData.scale;
        navPoint.tag = objData.type;
        navPoint.name = objData.name;
        navPoint.isStatic = true;
        navPoint.GetComponentInChildren<TextMeshPro>().text = objData.label;
        navPoint.layer = LayerMask.NameToLayer("AR Content");

        return navPoint;
    }

    /// <summary>
    /// Attaches an AR anchor to a given GameObject at a specified position and rotation.
    /// </summary>
    /// <param name="obj">The GameObject to attach an anchor to.</param>
    /// <param name="position">The world position where the anchor should be placed.</param>
    /// <param name="rotation">The rotation of the anchor.</param>
    private void AttachAnchor(GameObject obj, Vector3 position, Quaternion rotation)
    {
        if (anchorManager == null)
        {
            Debug.LogError("ARAnchorManager not assigned!");
            return;
        }

        ARAnchor anchor = anchorManager.AddAnchor(new Pose(position, rotation));
        if (anchor != null)
        {
            obj.transform.SetParent(anchor.transform, true);
        }
        else
        { 
            Debug.LogError("Failed to create an anchor.");
        }
    }

    /// <summary>
    /// Loads and imports a 3D model from the specified folder.
    /// </summary>
    /// <param name="folderPath">Path to the folder containing the model.</param>
    /// <param name="objData">Scene object data.</param>
    private void Import3DModel(string folderPath, SceneObjectData objData)
    {
        string modelPath = Path.Combine(folderPath, $"{objData.name}.obj");
        if (!File.Exists(modelPath)) return;

        GameObject importedModel = objLoader.Load(modelPath);
        AttachAnchor(importedModel, objData.position, objData.rotation);
        importedModel.transform.SetParent(sceneRoot.transform, true);

        if (importedModel == null) return;

        // Find all mesh objects
        List<GameObject> meshObjects = FindMeshObjects(importedModel);

        if (meshObjects.Count == 0)
        {
            Debug.LogError($"Error: No mesh found in model {objData.name}");
            return;
        }

        // Apply transformation properties to the parent object
        importedModel.transform.SetPositionAndRotation(objData.position, objData.rotation);
        importedModel.transform.localScale = objData.scale;
        importedModel.tag = objData.type;
        importedModel.name = objData.name;
        importedModel.isStatic = true;

        // HIDE VISUALS: Disable renderers (but keep colliders active)
        HideRenderers(meshObjects);

        // Add colliders to all found mesh objects
        foreach (GameObject meshObject in meshObjects)
        {
            if (meshObject.GetComponent<Collider>() == null)
            {
                meshObject.AddComponent<MeshCollider>();
            }
            meshObject.layer = LayerMask.NameToLayer("Walkable");
        }
    }

    /// <summary>
    /// Hides renderers of the provided objects to make them invisible.
    /// </summary>
    /// <param name="objects">List of GameObjects to hide.</param>
    private void HideRenderers(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                renderer.material = GetTransparentMaterial();
            }
        }
    }

    /// <summary>
    /// Finds all mesh objects within the given GameObject hierarchy.
    /// </summary>
    /// <param name="obj">Root GameObject to search.</param>
    /// <returns>List of found mesh GameObjects.</returns>
    private List<GameObject> FindMeshObjects(GameObject obj)
    {
        List<GameObject> meshObjects = new List<GameObject>();

        if (obj.GetComponent<MeshRenderer>() != null || obj.GetComponent<MeshFilter>() != null)
        {
            meshObjects.Add(obj);
        }

        foreach (Transform child in obj.transform)
        {
            meshObjects.AddRange(FindMeshObjects(child.gameObject));
        }

        return meshObjects;
    }

    /// <summary>
    /// Recursively adds colliders to a GameObject and its children.
    /// </summary>
    /// <param name="obj">The GameObject to add colliders to.</param>
    private void AddCollidersRecursively(GameObject obj)
    {
        // Add a MeshCollider if the object has a MeshRenderer and no collider
        if (obj.GetComponent<MeshRenderer>() != null && obj.GetComponent<Collider>() == null)
        {
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.convex = false; // Ensure proper collision for complex objects
        }

        // Recursively add colliders to children
        foreach (Transform child in obj.transform)
        {
            AddCollidersRecursively(child.gameObject);
        }
    }

    /// <summary>
    /// Deletes the temporary folder after the NavMesh is baked.
    /// </summary>
    /// <param name="folderPath">Path to the folder to delete.</param>
    private IEnumerator DeleteTempFolderAfterNavMesh(string folderPath)
    {
        yield return new WaitForSeconds(3.0f); // Wait for NavMesh baking
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    /// <summary>
    /// Bakes the NavMesh for all imported objects in the scene.
    /// </summary>
    void BakeNavMesh()
    {
        List<NavMeshSurface> navMeshSurfaces = new List<NavMeshSurface>();

        foreach (Transform child in sceneRoot.transform)
        {
            List<GameObject> meshObjects = FindMeshObjects(child.gameObject);

            if (meshObjects.Count == 0)
            {
                Debug.LogWarning($"Error: No mesh objects found in {child.name}, skipping NavMesh baking.");
                continue;
            }

            foreach (GameObject meshObject in meshObjects)
            {
                NavMeshSurface navMeshSurface = meshObject.GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = meshObject.AddComponent<NavMeshSurface>();
                }

                navMeshSurface.collectObjects = CollectObjects.Children;
                navMeshSurfaces.Add(navMeshSurface);
            }
        }

        if (navMeshSurfaces.Count == 0)
        {
            Debug.LogError("No valid NavMeshSurface found on imported models!");
            return;
        }

        foreach (var surface in navMeshSurfaces)
        {
            // Set parameters from the image
            surface.agentTypeID = 0; // Use the default Humanoid agent type

            // Step Height & Slope from the image
            surface.defaultArea = 0; // Default walkable area
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(surface.agentTypeID);
            settings.agentRadius = 1f;
            settings.agentHeight = 1f;
            settings.agentSlope = 20f;
            settings.agentClimb = 1f; // Step height from the image

            surface.BuildNavMesh();
        }

        Debug.Log("NavMesh successfully baked!");

        // Set default navigation path
        SetDefaultNavigationPath();
        // VisualizeNavMesh();
    }

    /// <summary>
    /// Sets the default navigation path to the first available destination.
    /// </summary>
    private void SetDefaultNavigationPath()
    {
        if (destinationPoints.Count > 0)
        {
            defaultDestination = destinationPoints.Keys.First();
            SetDestination(defaultDestination);
        }
    }
    
    /// <summary>
    /// Sets the destination for navigation and updates the path accordingly.
    /// </summary>
    /// <param name="targetLabel">The label of the target destination.</param>
    public void SetDestination(string targetLabel)
    {
        defaultDestination = targetLabel;
        currentPathIndex = 0;
        lastSpokenInstruction = "";
        lastInstructionTime = DateTime.UtcNow;
        hasSpokenInitialInstruction = false;
        hasReachedDestination = false;
        lastValidPathCorners = null;
        pathLine.positionCount = 0;
        shouldRecalculatePath = true;

        if (destinationPoints.TryGetValue(targetLabel, out Transform target))
        {
            ShowNavigationPath(target.position);
        }
        else if (destinationPoints.TryGetValue(destinationPoints.Keys.First(), out Transform defaultTarget))
        {
            ShowNavigationPath(defaultTarget.position);
        }
        else
        {
            Debug.LogError($"debug log: Destination {targetLabel} not found.");
        }

        // Loop through all destinations and show only the selected one
        foreach (var kvp in destinationPoints)
        {
            Transform destination = kvp.Value;
            bool isSelected = kvp.Key == targetLabel;
            MeshRenderer[] renderers = destination.GetComponentsInChildren<MeshRenderer>();
            TextMeshPro textMesh = destination.transform.Find("LabelText")?.GetComponent<TextMeshPro>();

            if (textMesh != null) {
                textMesh.enabled = isSelected;
                Debug.Log($"debug log: textMesh.enabled {textMesh.enabled}");
            }

            foreach (var renderer in renderers)
            {
                if (kvp.Key == targetLabel)
                {
                    ResetShader(renderer);
                }
                else
                {
                    // Apply transparent shader to hide other destinations
                    ApplyTransparentShader(renderer);
                }
            }
        }
    }

    /// <summary>
    /// Resets the shader of a renderer to the default red material for location pins.
    /// </summary>
    private void ResetShader(Renderer renderer)
    {
        TextMeshPro tmp = renderer.GetComponent<TextMeshPro>(); 
        Debug.Log($"debug log: textMesh.enabled {tmp}");
        // Only apply red material to non-TextMeshPro objects
        if (tmp == null)
        {
            renderer.material = redMaterial;
        }
    }

    /// <summary>
    /// Applies a transparent shader to a given renderer, but ensures TextMeshPro retains its material.
    /// </summary>
    /// <param name="renderer">Renderer to apply transparency to.</param>
    private void ApplyTransparentShader(Renderer renderer)
    {
        if (renderer == null)
        {
            Debug.LogError("debug log: ApplyTransparentShader - Renderer is null!");
            return;
        }

        if (transparentMaterial != null)
        {
            renderer.material = GetTransparentMaterial();
            Debug.Log($"debug log: ApplyTransparentShader - Set transparent material for {renderer.gameObject.name}");
        }
        else
        {
            Debug.LogError("debug log: ApplyTransparentShader - Failed to create transparent material!");
        }
    }

    /// <summary>
    /// Creates a transparent material to be used for rendering.
    /// </summary>
    /// <returns>The created transparent material.</returns>
    private Material GetTransparentMaterial()
    {
        if (transparentMaterial == null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null)
            {
                Debug.LogError("Standard Shader not found!");
                return null;
            }

            transparentMaterial = new Material(standardShader);
            transparentMaterial.SetFloat("_Mode", 3); // Transparent Mode
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;

            Color color = transparentMaterial.color;
            color.a = 0f;
            transparentMaterial.color = color;
        }
        return transparentMaterial;
    }

    /// <summary>
    /// Displays the navigation path from the user's position to the target.
    /// </summary>
    /// <param name="targetPosition">Target destination position.</param>
    private void ShowNavigationPath(Vector3 targetPosition)
    {
        if (xrOrigin == null) return;

        NavMeshPath path = new NavMeshPath();
        Vector3 startPosition = Camera.main.transform.position; // Use AR Camera position

        // Ensure target position is on the NavMesh
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"Error: Target position {targetPosition} is NOT on the NavMesh. Finding nearest valid point...");
        }

        // Adjusted valid NavMesh position
        targetPosition = hit.position;  

        // Ensure player (mainCamera) position is also on the NavMesh
        if (!NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, 5.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"Error: XR Origin position {startPosition} is NOT on the NavMesh.");
        }

        // Adjusted valid start position
        startPosition = startHit.position;
        bool pathValid = NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path);

        // Calculate path
        if (pathValid && path.status == NavMeshPathStatus.PathComplete)
        {
            lastValidPathCorners = path.corners;
            pathLine.positionCount = 0;
            pathLine.widthMultiplier = 1.2f;
            pathLine.startWidth = 2.5f;
            pathLine.endWidth = 2.5f;
            pathLine.positionCount = path.corners.Length;
            pathLine.SetPositions(path.corners);

            ProvideVoiceNavigation(path);
        } 
        else
        {
            SendVoiceInstruction($"Path calculation failed. Using last valid path.");

            // Show last valid path if it exists
            if (lastValidPathCorners != null && lastValidPathCorners.Length > 0)
            {
                pathLine.positionCount = lastValidPathCorners.Length;
                pathLine.SetPositions(lastValidPathCorners);
            }
            else
            {
                pathLine.positionCount = 0;
            }
        }
    }
    
    /// <summary>
    /// Provides voice guidance for the user along the navigation path.
    /// </summary>
    /// <param name="path">The calculated navigation path.</param>
    private void ProvideVoiceNavigation(NavMeshPath path)
    {
        if (path.corners.Length < 2 || hasReachedDestination) return; // Ensure path has enough points

        Vector3 userPosition = Camera.main.transform.position;
        double secondsSinceLastInstruction = (DateTime.UtcNow - lastInstructionTime).TotalSeconds;

        // **Check if the user reached the destination**
        if (UserReachedDestination(userPosition, path))
        {
            currentPathIndex = 0;  // Reset the index
            hasReachedDestination = true;
            return;
        }

        if (currentPathIndex >= path.corners.Length - 1)
        {
            return;
        }

        Vector3 current = path.corners[currentPathIndex];

        if (currentPathIndex + 1 < path.corners.Length)
        {
            Vector3 next = path.corners[currentPathIndex + 1];

            if (UserNeedsToRotate(userPosition, next))
            {
                SendVoiceInstruction("Rotate 180 degrees and then move forward.");
                return;
            }

            if (secondsSinceLastInstruction < instructionCooldown && Vector3.Distance(userPosition, lastUserPosition) < 0.1f)
            {
                return;
            }

            if (currentPathIndex == 0)
            {
                GiveInitialInstruction(path);
            }
            else
            {
                if (ShouldMoveToNextPoint( secondsSinceLastInstruction))
                {
                    currentPathIndex++;
                    GiveNextInstruction(path);
                    lastUserPosition = userPosition;
                }
            }

            if (currentPathIndex < path.corners.Length - 2)
            {
                string turnInstruction = CalculateTurnInstruction(current, next, path.corners[currentPathIndex + 2]);
                if (!string.IsNullOrEmpty(turnInstruction) && lastInstruction != turnInstruction)
                {
                    SendVoiceInstruction(turnInstruction);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a turn instruction should be given based on the path's direction change.
    /// </summary>
    private string CalculateTurnInstruction(Vector3 current, Vector3 next, Vector3 nextSegment)
    {
        Vector3 direction = (next - current).normalized;
        Vector3 nextDirection = (nextSegment - next).normalized;
        float angle = Vector3.SignedAngle(direction, nextDirection, Vector3.up);

        if (Mathf.Abs(angle) > 20) // Detect turn
        {
            if (angle > 20) return "Turn left";
            if (angle < -20) return "Turn right";
            if (Mathf.Abs(angle) >= 150) return "Take a U-turn";
        }
        return null;
    }

    /// <summary>
    /// Checks if the user should move to the next path index based on distance and time.
    /// </summary>
    private bool ShouldMoveToNextPoint(double secondsSinceLastInstruction)
    {
        return secondsSinceLastInstruction >= instructionCooldown;
    }

    /// <summary>
    /// Checks if the user has reached the destination.
    /// </summary>
    private bool UserReachedDestination(Vector3 userPosition, NavMeshPath path)
    {
        // Ensure there's a valid path
        if (path.corners.Length < 2) return false; 

        int lastIndex = path.corners.Length - 1;
        Vector3 userXZ = new Vector3(userPosition.x, 0, userPosition.z);
        Vector3 finalXZ = new Vector3(path.corners[lastIndex].x, 0, path.corners[lastIndex].z);

        float distanceToFinal = Vector3.Distance(userXZ, finalXZ);
        float nearThreshold = destinationThreshold / 2;
        if (distanceToFinal < nearThreshold)
        {
            SendVoiceInstruction($"You have arrived at your destination {defaultDestination}");
            return true;
        }
        else if (distanceToFinal < destinationThreshold) // If close but not inside threshold
        {
            SendVoiceInstruction($"Your destination {defaultDestination} is about {distanceToFinal:F1} meters ahead.");
        }

        return false;
    }


    /// <summary>
    /// Determines whether the user needs to rotate before proceeding.
    /// </summary>
    private bool UserNeedsToRotate(Vector3 userPosition, Vector3 next)
    {
        Vector3 userDirection = Camera.main.transform.forward.normalized;
        Vector3 directionToNext = (next - userPosition).normalized;
        float dotProduct = Vector3.Dot(userDirection, directionToNext);
        
        return dotProduct < -0.3f;
    }

    /// <summary>
    /// Gives an initial voice instruction to start moving.
    /// </summary>
    /// <param name="path">The navigation path.</param>
    private void GiveInitialInstruction(NavMeshPath path)
    {
        if (path.corners.Length < 2 || hasSpokenInitialInstruction) return;

        Vector3 start = path.corners[0];
        Vector3 next = path.corners[1];
        float initialDistance = Vector3.Distance(start, next);
        string instruction = $"Start moving forward {Mathf.Round(initialDistance)} meters.";

        currentPathIndex = 1;
        SendVoiceInstruction(instruction);
        hasSpokenInitialInstruction = true;
    }

    /// <summary>
    /// Provides the next voice instruction for navigation.
    /// </summary>
    /// <param name="path">The navigation path.</param>
    private void GiveNextInstruction(NavMeshPath path)
    {
        if (currentPathIndex >= path.corners.Length - 1) return;

        Vector3 current = path.corners[currentPathIndex];
        Vector3 next = path.corners[currentPathIndex + 1];

        float distance = Vector3.Distance(current, next);
        SendVoiceInstruction($"Move forward {Mathf.Round(distance)} meters.");

        currentPathIndex++;
    }

    /// <summary>
    /// Sends a voice instruction to Flutter if it's different from the last spoken instruction.
    /// </summary>
    /// <param name="instruction">The voice instruction to be sent.</param>
    private void SendVoiceInstruction(string instruction)
    {
        if (instruction == lastSpokenInstruction)
        {
            return;
        }

        lastSpokenInstruction = instruction;
        lastInstructionTime = DateTime.UtcNow; // Update last spoken time

        string jsonMessage = JsonConvert.SerializeObject(new { navigationInstructions = new List<string> { instruction } });
        unityMessageSender.SendMessageToFlutter(jsonMessage);
    }

    /// <summary>
    /// Sends the list of destination labels to Flutter.
    /// </summary>
    public void SendDestinationLabelsToFlutter()
    {
        // Extract all destination labels
        List<string> destinationLabels = new List<string>(destinationPoints.Keys);
    
        // Convert to proper JSON format
        string jsonLabels = JsonConvert.SerializeObject(new { destinations = destinationLabels });

        Debug.Log($"Destination Points: {string.Join(", ", destinationPoints.Keys)}, \n {jsonLabels}");

        unityMessageSender.SendMessageToFlutter(jsonLabels);
    }

    // void VisualizeNavMesh()
    // {
    //     Mesh navMesh = new Mesh();
    //     NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();

    //     navMesh.vertices = triangulatedNavMesh.vertices;
    //     navMesh.triangles = triangulatedNavMesh.indices;

    //     GameObject navMeshObject = new GameObject("NavMeshVisualization");
    //     MeshFilter meshFilter = navMeshObject.AddComponent<MeshFilter>();
    //     MeshRenderer meshRenderer = navMeshObject.AddComponent<MeshRenderer>();

    //     meshFilter.mesh = navMesh;

    //     // Assign shader only to NavMesh visualization, not the imported object
    //     Material navMeshMaterial = new Material(Shader.Find("Custom/NavMeshVisualizer"));
    //     meshRenderer.material = navMeshMaterial;
    // }


    // void DrawNavMeshEdges()
    // {
    //     var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
    //     GameObject navMeshEdges = new GameObject("NavMeshEdges");

    //     for (int i = 0; i < triangulation.indices.Length; i += 3)
    //     {
    //         Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
    //         Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
    //         Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

    //         DrawLine(navMeshEdges, v1, v2);
    //         DrawLine(navMeshEdges, v2, v3);
    //         DrawLine(navMeshEdges, v3, v1);
    //     }
    // }

    // void DrawLine(GameObject parent, Vector3 start, Vector3 end)
    // {
    //     GameObject line = new GameObject("NavMeshLine");
    //     LineRenderer lr = line.AddComponent<LineRenderer>();
    //     lr.startWidth = 0.02f;
    //     lr.endWidth = 0.02f;
    //     lr.positionCount = 2;
    //     lr.SetPositions(new Vector3[] { start, end });
    //     lr.material = new Material(Shader.Find("Sprites/Default")); // Use a simple material
    //     lr.startColor = Color.green;
    //     lr.endColor = Color.green;
    //     line.transform.SetParent(parent.transform);
    // }
}
