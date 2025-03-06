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

public class NavigationController : MonoBehaviour
{
    [SerializeField] private UnityMessageSender unityMessageSender;
    public GameObject xrOrigin; // XR Origin GameObject
    public GameObject navigationPointPrefab; // Prefab for navigation points destination
    public GameObject sourcePrefab; // Prefab for source
    public LineRenderer pathLine; // LineRenderer for navigation
    private OBJLoader objLoader = new OBJLoader();
    private Dictionary<string, Transform> destinationPoints = new Dictionary<string, Transform>();
    private string defaultDestination;
    private GameObject sceneRoot;
    private string lastInstruction = "";
    private float lastInstructionTime = 0f;
    private int currentPathIndex = 0;

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

    void Update()
    {
        if (xrOrigin != null && !string.IsNullOrEmpty(defaultDestination) &&
            destinationPoints.TryGetValue(defaultDestination, out Transform target))
        {
            ShowNavigationPath(target.position);
        }
    }

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

            foreach (var objData in sceneData.objects)
            {
                Debug.LogError($"navigation point for {objData} {objData.name}");
                if (objData.type == "NavigationLine" && objData.isSource == true)
                {
                    SetupNavigationPoint(objData);
                    xrOrigin.transform.position = objData.position;
                    xrOrigin.transform.rotation = objData.rotation;
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
            // Bake after a short delay to ensure all objects are included
            Invoke(nameof(BakeNavMesh), 2.0f);

            // Delete temp folder after baking NavMesh
            StartCoroutine(DeleteTempFolderAfterNavMesh(tempFolder));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error importing scene: {ex.Message}");
        }
    }

    private GameObject SetupNavigationPoint(SceneObjectData objData)
    {
        GameObject navPoint;
        if (objData.isSource) 
        {
            navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
        } else {
            navPoint = Instantiate(sourcePrefab, objData.position, objData.rotation);
        }
        navPoint.transform.SetParent(sceneRoot.transform, true);
        navPoint.transform.localScale = objData.scale;
        navPoint.tag = objData.type;
        navPoint.name = objData.name;
        navPoint.isStatic = true;
        navPoint.GetComponentInChildren<TextMeshPro>().text = objData.label;
        navPoint.layer = LayerMask.NameToLayer("AR Content");

        return navPoint;
    }

    private void Import3DModel(string folderPath, SceneObjectData objData)
    {
        string modelPath = Path.Combine(folderPath, $"{objData.name}.obj");
        if (!File.Exists(modelPath)) return;

        GameObject importedModel = objLoader.Load(modelPath);
        importedModel.transform.SetParent(sceneRoot.transform, true);
        if (importedModel == null) return;

        // Find all mesh objects
        List<GameObject> meshObjects = FindMeshObjects(importedModel);

        if (meshObjects.Count == 0)
        {
            Debug.LogError($"No mesh found in model {objData.name}");
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

        Debug.Log($"{objData.name} imported with {meshObjects.Count} mesh objects assigned to NavMesh.");
    }


     private void HideRenderers(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                // Use the Standard Shader with transparency settings
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null)
                {
                    Debug.LogError("Standard Shader not found!");
                    continue;
                }

                Material transparentMaterial = new Material(standardShader);
                transparentMaterial.SetFloat("_Mode", 3); // 3 = Transparent Mode
                transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMaterial.SetInt("_ZWrite", 0);
                transparentMaterial.DisableKeyword("_ALPHATEST_ON");
                transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
                transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMaterial.renderQueue = 3000; // Set render queue for transparency

                // Set transparency color
                Color color = transparentMaterial.color;
                color.a = 0f; // Fully transparent
                transparentMaterial.color = color;

                renderer.material = transparentMaterial;
            }
        }
    }


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

    public void SetDestination(string targetLabel)
    {
        defaultDestination = targetLabel;
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
            Debug.LogError($"Destination {targetLabel} not found.");
        }
    }

    private IEnumerator DeleteTempFolderAfterNavMesh(string folderPath)
    {
        yield return new WaitForSeconds(3.0f); // Wait for NavMesh baking
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    // Bake NavMesh for all imported objects
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
            settings.agentRadius = 0.1f;
            settings.agentHeight = 1.52f;
            settings.agentSlope = 45f;
            settings.agentClimb = 0.5f; // Step height from the image

            surface.BuildNavMesh();
        }

        Debug.Log("NavMesh successfully baked!");

        // Set default navigation path
        SetDefaultNavigationPath();
        // VisualizeNavMesh();
    }

    private void SetDefaultNavigationPath()
    {
        if (destinationPoints.Count > 0)
        {
            defaultDestination = destinationPoints.Keys.First();
            SetDestination(defaultDestination);
        }
    }
    
    private void ShowNavigationPath(Vector3 targetPosition)
    {
        if (xrOrigin == null) return;

        NavMeshPath path = new NavMeshPath();
        Vector3 startPosition = xrOrigin.transform.position; // Use XR Origin position as the player position

        // Ensure target position is on the NavMesh
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"Error: Target position {targetPosition} is NOT on the NavMesh. Finding nearest valid point...");
        }
        
        targetPosition = hit.position;  // Adjusted valid NavMesh position

        // Ensure player (xrOrigin) position is also on the NavMesh
        if (!NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, 5.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"Error: XR Origin position {startPosition} is NOT on the NavMesh.");
        }

        startPosition = startHit.position; // Adjusted valid start position

        // Calculate path
        if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            pathLine.positionCount = 0;
            pathLine.widthMultiplier = 0.5f;
            pathLine.startWidth = 0.5f;
            pathLine.endWidth = 0.5f;
            pathLine.positionCount = path.corners.Length;
            currentPathIndex = 0; // Reset index for new path
            pathLine.SetPositions(path.corners);

            ProvideVoiceNavigation(path);
        } 
        else
        {
            Debug.LogError($"Error: Path calculation failed: No valid path. {startPosition} → {targetPosition}");
            pathLine.positionCount = 0;
        }
    }

    private void ProvideVoiceNavigation(NavMeshPath path)
    {
        if (path.corners.Length < 2) return; // No valid path

        List<string> instructions = new List<string>();
        float minDistanceThreshold = 1.5f; // Avoid tiny movements triggering voice commands

        Vector3 userPosition = xrOrigin.transform.position;

        // If user has moved to the next path segment
        if (currentPathIndex < path.corners.Length - 1)
        {
            Vector3 current = path.corners[currentPathIndex];
            Vector3 next = path.corners[currentPathIndex + 1];

            // Check if user has reached the next segment
            if (Vector3.Distance(userPosition, next) < minDistanceThreshold)
            {
                currentPathIndex++; // Move to the next segment

                // Issue move forward instruction when reaching a new straight segment
                if (currentPathIndex < path.corners.Length - 1)
                {
                    float distance = Vector3.Distance(next, path.corners[currentPathIndex + 1]);
                    if (distance >= minDistanceThreshold)
                    {
                        string moveInstruction = $"Move forward {Mathf.Round(distance)} meters.";
                        instructions.Add(moveInstruction);
                        lastInstruction = moveInstruction;
                        lastInstructionTime = Time.time;
                    }
                }
            }

            // Check for turn instructions
            if (currentPathIndex < path.corners.Length - 2)
            {
                Vector3 direction = (next - current).normalized;
                Vector3 nextDirection = (path.corners[currentPathIndex + 2] - next).normalized;

                float angle = Vector3.SignedAngle(direction, nextDirection, Vector3.up);
                string turnInstruction = "";

                if (angle > 30 && angle < 150)
                    turnInstruction = "Turn right.";
                else if (angle < -30 && angle > -150)
                    turnInstruction = "Turn left.";
                else if (Mathf.Abs(angle) >= 150)
                    turnInstruction = "Take a U-turn.";

                if (!string.IsNullOrEmpty(turnInstruction) && turnInstruction != lastInstruction)
                {
                    instructions.Add(turnInstruction);
                    lastInstruction = turnInstruction;
                    lastInstructionTime = Time.time;
                }
            }
        }

        // Final instruction when reaching destination
        if (currentPathIndex >= path.corners.Length - 1)
        {
            string destinationInstruction = "You have reached your destination.";
            if (destinationInstruction != lastInstruction)
            {
                instructions.Add(destinationInstruction);
                lastInstruction = destinationInstruction;
                lastInstructionTime = Time.time;
            }
        }

        // Send to Flutter for voice output if there are new instructions
        if (instructions.Count > 0)
        {
            unityMessageSender.SendMessageToFlutter(JsonConvert.SerializeObject(new { navigationInstructions = instructions }));
        }
    }

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
