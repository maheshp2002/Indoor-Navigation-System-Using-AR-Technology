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
    public LineRenderer pathLine; // LineRenderer for navigation
    private OBJLoader objLoader = new OBJLoader();
    private Dictionary<string, Transform> destinationPoints = new Dictionary<string, Transform>();
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private string defaultDestination;
    public Camera ARCamera;
    public GameObject navigationController; 

    void Start()
    {
        #if UNITY_ANDROID
            Camera.main.clearFlags = CameraClearFlags.Depth; // No background, keeps AR view
            Camera.main.cullingMask = LayerMask.GetMask("AR Content", "UI"); // Show only AR layers
    
            // Ensure the XR Origin has a NavMeshAgent
            navMeshAgent = xrOrigin.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = xrOrigin.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }

            navMeshAgent.stoppingDistance = 0f;
            navMeshAgent.autoTraverseOffMeshLink = true;
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = false;
            navMeshAgent.radius = 0.14f; 
            navMeshAgent.angularSpeed = 120;
            navMeshAgent.speed = 0;

            Input.multiTouchEnabled = false;
        #endif
    }

    void Update()
    {
        if (navMeshAgent != null && xrOrigin != null)
        {
            // Ensure NavMeshAgent follows XR Origin
            navMeshAgent.transform.position = xrOrigin.transform.position;

            // Update path dynamically if user moves
            if (!string.IsNullOrEmpty(defaultDestination) && destinationPoints.TryGetValue(defaultDestination, out Transform target))
            {
                ShowNavigationPath(target.position);
            }
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
        ARCamera = Camera.main;

        try
        {
            // Create the temporary directory
            Directory.CreateDirectory(tempFolder);

            // Extract the zip file
            ZipFile.ExtractToDirectory(zipFilePath, tempFolder);

            // Check for sceneData.json
            string jsonPath = Path.Combine(tempFolder, "sceneData.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError("sceneData.json not found.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

            if (sceneData.objects == null || sceneData.objects.Count == 0)
            {
                Debug.LogError("No objects found in the scene metadata.");
                return;
            }

            // Apply camera data
            if (sceneData.cameraData != null)
            {
                ARCamera.transform.position = sceneData.cameraData.position;
                ARCamera.transform.rotation = sceneData.cameraData.rotation;
                ARCamera.fieldOfView = 60f;
            }

            // Create a parent GameObject for the imported environment
            GameObject importedEnvironment = new GameObject("ImportedEnvironment");

            foreach (var objData in sceneData.objects)
            {
                if (objData.type == "NavigationLine" && objData.isSource == true)
                {
                    GameObject navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
                    navPoint.transform.localScale = objData.scale;
                    var textMesh = navPoint.GetComponentInChildren<TextMeshPro>();
                    textMesh.text = objData.label;
                    navPoint.tag = objData.type;
                    navPoint.name = objData.name;
                    navPoint.isStatic = true;

                    // Move NavigationController to source position
                    if (navigationController != null)
                    {
                        navigationController.transform.position = objData.position;
                        navigationController.transform.rotation = objData.rotation;
                    }

                    xrOrigin.transform.position = objData.position;
                    xrOrigin.SetActive(true);
                }
                else if (objData.type == "NavigationLine" && objData.isDestination == true)
                {
                    GameObject navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
                    navPoint.transform.localScale = objData.scale;
                    var textMesh = navPoint.GetComponentInChildren<TextMeshPro>();
                    textMesh.text = objData.label;
                    navPoint.tag = objData.type;
                    navPoint.name = objData.name;
                    navPoint.isStatic = true;

                    // Store the destination for navigation
                    destinationPoints.Add(objData.label, navPoint.transform);
                }
                else
                {
                    // Import 3D objects from the zip folder
                    string modelPath = Path.Combine(tempFolder, $"{objData.name}.obj");

                    if (File.Exists(modelPath))
                    {
                        GameObject importedModel = objLoader.Load(modelPath);

                        if (importedModel != null)
                        {
                            // Find the object with MeshRenderer or MeshFilter
                            GameObject meshObject = FindMeshObject(importedModel);

                            if (meshObject != null)
                            {                                
                                importedModel.transform.position = objData.position;
                                importedModel.transform.rotation = objData.rotation;
                                importedModel.transform.localScale = objData.scale;
                                importedModel.tag = objData.type;
                                importedModel.name = objData.name;
                                importedModel.layer = LayerMask.NameToLayer("Walkable");
                                importedModel.isStatic = true;
                                AddCollidersRecursively(meshObject);
                                AddNavMeshSurface(importedModel);

                                // Assign shader to walkable surfaces
                                Shader walkableShader = Shader.Find("Custom/NavMeshVisualizer");

                                if (walkableShader != null)
                                {
                                    Renderer objRenderer = meshObject.GetComponent<Renderer>();
                                    if (objRenderer != null)
                                    {
                                        objRenderer.material = new Material(walkableShader);
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Walkable area shader not found!");
                                }

                                // Ensure the object has a collider
                            }
                            else
                            {
                                Debug.LogError("Mesh object not found in the imported model.");
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to load 3D model.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"3D model file not found: {modelPath}");
                    }
                }
            }

            SendDestinationLabelsToFlutter();

            // Add NavMeshSurface to the root of the imported objects
            NavMeshSurface navMeshSurface = importedEnvironment.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.layerMask = LayerMask.GetMask("Walkable");

            // Bake after a short delay to ensure all objects are included
            Invoke(nameof(BakeNavMesh), 1.0f);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error importing scene: {ex.Message}");
        }
        finally
        {
            SetDefaultNavigationPath();
            Directory.Delete(tempFolder, true);
        }
    }

    void AddNavMeshSurface(GameObject importedObject)
    {
        // Find the root parent to attach the NavMeshSurface
        GameObject rootParent = importedObject.transform.root.gameObject;

        // Check if a NavMeshSurface component is already attached
        NavMeshSurface surface = rootParent.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = rootParent.AddComponent<NavMeshSurface>();
        }

        // Configure the NavMeshSurface properties
        surface.collectObjects = CollectObjects.Children; // Include all children
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = LayerMask.GetMask("Walkable");
        
        // Rebuild NavMesh after adding object
        surface.BuildNavMesh();

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
            surface.BuildNavMesh();
        }

        Debug.Log("NavMesh successfully baked!");

        // Ensure NavMeshAgent is enabled after NavMesh is built
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = true;
        }

        VisualizeNavMesh();
    }


    void VisualizeNavMesh()
    {
        Mesh navMesh = new Mesh();
        UnityEngine.AI.NavMeshTriangulation triangulatedNavMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();

        navMesh.vertices = triangulatedNavMesh.vertices;
        navMesh.triangles = triangulatedNavMesh.indices;

        GameObject navMeshObject = new GameObject("NavMeshVisualization");
        MeshFilter meshFilter = navMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = navMeshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = navMesh;

        // Create a material using the custom shader
        Material navMeshMaterial = new Material(Shader.Find("Custom/NavMeshDebug"));
        meshRenderer.material = navMeshMaterial;

        Debug.Log($"macs NavMeshSurface added successfully {meshRenderer}");
        DrawNavMeshEdges();

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
        PlaceAgentOnNavMesh(navMeshAgent); 
        SendNavMeshDataToFlutter();
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

        Debug.Log($"macdevils NavMeshSurface added successfully {line}");
    }

    void SendNavMeshDataToFlutter()
    {
        var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
        List<Vector3> vertices = new List<Vector3>(triangulation.vertices);
        List<int> indices = new List<int>(triangulation.indices);

        var navMeshData = new
        {
            vertices = vertices.Select(v => new { v.x, v.y, v.z }),
            indices
        };

        string json = JsonConvert.SerializeObject(navMeshData);
        Debug.Log($"macdevils {json}");
        unityMessageSender.SendMessageToFlutter(json);
    }

    void PlaceAgentOnNavMesh(UnityEngine.AI.NavMeshAgent agent)
    {
        UnityEngine.AI.NavMeshHit hit;

        if (UnityEngine.AI.NavMesh.SamplePosition(agent.transform.position, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            Debug.Log($"macsNavMeshAgent placed successfully at {hit.position}");
        }
        else
        {
            Debug.LogError("No valid NavMesh position found for the agent! Trying alternative methods...");
            Debug.Log($"Agent's current position: {agent.transform.position}");
            
            NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0)
            {
                Debug.LogError("NavMesh appears to be empty. Make sure NavMesh was built properly.");
            }
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
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();

        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogError("NavMeshAgent is not on a valid NavMesh!");
            return;
        }

        if (navMeshAgent.CalculatePath(targetPosition, path))
        {
            pathLine.positionCount = path.corners.Length;
            pathLine.SetPositions(path.corners);
            Debug.Log("navmesh added");
        }
        else
        {
            Debug.LogError("Failed to calculate path.");
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
