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

    void Start()
    {
        #if UNITY_ANDROID
            Camera.main.clearFlags = CameraClearFlags.Depth; // No background, keeps AR view
            Camera.main.cullingMask = LayerMask.GetMask("AR Content", "UI", "Walkable"); // Show only AR layers
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
                    Debug.Log($"XR Origin placed at start: {xrOrigin.transform.position}");
                } else if (objData.type == "NavigationLine" && objData.isDestination == true)
                {
                    Debug.Log($"swat placed at start: {objData.position}");
                    GameObject navPoint = SetupNavigationPoint(objData);
                    Debug.Log($"swat placed after: {objData.position}");
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
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error importing scene: {ex.Message}");
        }
        finally
        {
            Directory.Delete(tempFolder, true);
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
        navPoint.transform.SetParent(null, true);
        navPoint.transform.localScale = objData.scale;
        navPoint.tag = objData.type;
        navPoint.name = objData.name;
        navPoint.isStatic = true;
        navPoint.GetComponentInChildren<TextMeshPro>().text = objData.label;

        return navPoint;
    }

    private void Import3DModel(string folderPath, SceneObjectData objData)
    {
        string modelPath = Path.Combine(folderPath, $"{objData.name}.obj");
        if (!File.Exists(modelPath)) return;

        GameObject importedModel = objLoader.Load(modelPath);
        importedModel.transform.SetParent(null, true);
        if (importedModel == null) return;

        GameObject meshObject = FindMeshObject(importedModel);

        if (meshObject == null) return;

        importedModel.transform.SetPositionAndRotation(objData.position, objData.rotation);
        importedModel.transform.localScale = objData.scale;
        importedModel.tag = objData.type;
        importedModel.name = objData.name;
        importedModel.isStatic = true;
        
        meshObject.layer = LayerMask.NameToLayer("Walkable");
        AddCollidersRecursively(meshObject);
        Debug.Log($"3D Model Imported - Name: {objData.name}, Position: {importedModel.transform.position}, Scale: {importedModel.transform.localScale}, Rotation: {importedModel.transform.rotation}");
        Renderer objRenderer = meshObject.GetComponent<Renderer>();
        if (objRenderer != null)
        {
            objRenderer.material = new Material(Shader.Find("Standard")); // Or any default shader
        }
        NavMeshSurface navMeshSurface = meshObject.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.layerMask = LayerMask.GetMask("Walkable");    
        Debug.Log($"{meshObject.name} assigned to layer: {meshObject.layer}");
    }

    // Bake NavMesh for all imported objects
    void BakeNavMesh()
    {
        NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>();

        if (surfaces.Length == 0)
        {
            Debug.LogError("No NavMeshSurface found in the scene!");
            return;
        }

        foreach (var surface in surfaces)
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

        if (destinationPoints.TryGetValue(destinationPoints.Keys.First(), out Transform target))
        {
            ShowNavigationPath(target.position);
        }

        LogNavMeshData();
        VisualizeNavMesh();
        Debug.Log("VisualizeNavMesh added successfully!");
    }

    void VisualizeNavMesh()
    {
        Mesh navMesh = new Mesh();
        NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();

        navMesh.vertices = triangulatedNavMesh.vertices;
        navMesh.triangles = triangulatedNavMesh.indices;

        GameObject navMeshObject = new GameObject("NavMeshVisualization");
        MeshFilter meshFilter = navMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = navMeshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = navMesh;

        // Assign shader only to NavMesh visualization, not the imported object
        Material navMeshMaterial = new Material(Shader.Find("Custom/NavMeshVisualizer"));
        meshRenderer.material = navMeshMaterial;
    }


    void DrawNavMeshEdges()
    {
        var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
        GameObject navMeshEdges = new GameObject("NavMeshEdges");

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
            Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

            DrawLine(navMeshEdges, v1, v2);
            DrawLine(navMeshEdges, v2, v3);
            DrawLine(navMeshEdges, v3, v1);
        }
    }

    void DrawLine(GameObject parent, Vector3 start, Vector3 end)
    {
        GameObject line = new GameObject("NavMeshLine");
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { start, end });
        lr.material = new Material(Shader.Find("Sprites/Default")); // Use a simple material
        lr.startColor = Color.green;
        lr.endColor = Color.green;
        line.transform.SetParent(parent.transform);
    }

    void LogNavMeshData()
    {
        var triangulation = NavMesh.CalculateTriangulation();

        Debug.Log($"NavMesh Triangulation: {triangulation.vertices.Length} vertices, {triangulation.indices.Length / 3} triangles");

        for (int i = 0; i < triangulation.vertices.Length; i++)
        {
            Debug.Log($"Vertex {i}: {triangulation.vertices[i]}");
        }

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
            Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
            Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

            Debug.Log($"Triangle {i / 3}: {v1}, {v2}, {v3}");
        }
    }


    private GameObject FindMeshObject(GameObject obj)
    {
        // Check if the current object has MeshRenderer or MeshFilter
        if (obj.GetComponent<MeshRenderer>() != null || obj.GetComponent<MeshFilter>() != null)
        {
            return obj;
        }

        // Recursively check all children
        foreach (Transform child in obj.transform)
        {
            GameObject found = FindMeshObject(child.gameObject);
            if (found != null)
            {
                Debug.Log($"macs {found}");
                return found;
            }
        }

        // If nothing is found, return null
        return null;
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
        if (destinationPoints.TryGetValue(targetLabel, out Transform target))
        {
            ShowNavigationPath(target.position);
        }
        else
        {
            Debug.LogError($"Destination {targetLabel} not found.");
        }
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

        Debug.Log($"XR Origin Position: {startPosition}");
        Debug.Log($"Original Target Position: {targetPosition}");

        // Ensure target position is on the NavMesh
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"Target position {targetPosition} is NOT on the NavMesh. Finding nearest valid point...");
            return;
        }
        
        targetPosition = hit.position;  // Adjusted valid NavMesh position

        Debug.Log($"Adjusted Target Position (On NavMesh): {targetPosition}");

        // Ensure player (xrOrigin) position is also on the NavMesh
        if (!NavMesh.SamplePosition(startPosition, out NavMeshHit startHit, 5.0f, NavMesh.AllAreas))
        {
            Debug.LogError($"XR Origin position {startPosition} is NOT on the NavMesh.");
            return;
        }

        startPosition = startHit.position; // Adjusted valid start position

        Debug.Log($"Adjusted Start Position (On NavMesh): {startPosition}");

        // Calculate path
        if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            pathLine.positionCount = path.corners.Length;
            pathLine.SetPositions(path.corners);
            pathLine.startColor = Color.red;
            pathLine.endColor = Color.blue;
            Debug.Log("Navigation path updated.");
        } 
        else
        {
            Debug.LogError($"Path calculation failed: No valid path. {startPosition} → {targetPosition}");
            pathLine.positionCount = 0;
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
}
