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

[Serializable]
public class SceneData
{
    public List<SceneObjectData> objects;
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

public class NavigationController : MonoBehaviour
{
    [SerializeField] private UnityMessageSender unityMessageSender;
    public GameObject xrOrigin; // XR Origin GameObject
    public GameObject navigationPointPrefab; // Prefab for navigation points (Source/Destination)
    public LineRenderer pathLine; // LineRenderer for navigation
    private OBJLoader objLoader = new OBJLoader();
    private Dictionary<string, Transform> destinationPoints = new Dictionary<string, Transform>();
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private bool isSceneLoaded = false;
    private string defaultDestination;
    public Camera ARCamera;

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
            navMeshAgent.stoppingDistance = 0.5f;
            navMeshAgent.autoTraverseOffMeshLink = true;
            navMeshAgent.updatePosition = true;
            navMeshAgent.updateRotation = false;
            navMeshAgent.height = 0.05f;   // Fixing agent height
            navMeshAgent.radius = 0.4f;    // Fixing agent radius
        #endif
    }

    void Update()
    {
        if (navMeshAgent != null && xrOrigin != null)
        {
            // Ensure NavMeshAgent follows XR Origin
            navMeshAgent.transform.position = xrOrigin.transform.position;

            // Optional: Update path dynamically if user moves
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
        string message = $"Ingu called ingi";
        unityMessageSender.SendMessageToFlutter(JsonUtility.ToJson(new { log = message }));
        try
        {
            Directory.CreateDirectory(tempFolder);
            ZipFile.ExtractToDirectory(zipFilePath, tempFolder);

            string jsonPath = Path.Combine(tempFolder, "sceneData.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("sceneData.json not found.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

            GameObject importedEnvironment = new GameObject("ImportedEnvironment");

            foreach (var objData in sceneData.objects)
            {
                if (objData.type == "NavigationLine" && objData.isSource)
                {
                    // Place XR Origin at the source position
                    xrOrigin.transform.position = objData.position;
                    xrOrigin.SetActive(true);
                }
                else if (objData.type == "NavigationLine" && objData.isDestination)
                {
                    // Instantiate destination markers
                    GameObject destination = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
                    // Adjust position for AR
                    destination.transform.position = ARCamera.transform.TransformPoint(objData.position);
                    
                    // destination.transform.localScale = objData.scale;
                    Bounds bounds = GetObjectBounds(destination);
                    float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    float scaleFactor = 1.0f / maxDimension; // Normalize scale
                    destination.transform.localScale = objData.scale * scaleFactor;

                    destination.tag = objData.type;
                    destination.name = objData.label;

                    // Ensure destination points are NOT children of xrOrigin
                    destination.transform.SetParent(null, true);

                    // Set label text
                    var textMesh = destination.GetComponentInChildren<TextMeshPro>();
                    textMesh.text = objData.label;

                    destination.isStatic = true;

                    // Store the destination for navigation
                    destinationPoints.Add(objData.label, destination.transform);
                }
                else
                {
                    // Import 3D objects from the zip folder
                    string modelPath = Path.Combine(tempFolder, $"{objData.type}.obj");
                    unityMessageSender.SendMessageToFlutter(JsonUtility.ToJson(new { log = $"Ingu: {modelPath}" }));
                    if (File.Exists(modelPath))
                    {
                        GameObject importedModel = objLoader.Load(modelPath);
                        unityMessageSender.SendMessageToFlutter(JsonUtility.ToJson(new { log = $"Ingu: {importedModel}" }));
                        if (importedModel != null)
                        {
                            // Find the object with MeshRenderer or MeshFilter
                            GameObject meshObject = FindMeshObject(importedModel);
                            unityMessageSender.SendMessageToFlutter(JsonUtility.ToJson(new { log = $"Ingu: {meshObject}" }));
                            if (meshObject != null)
                            {
                                    
                                Vector3 adjustedPosition = ARCamera.transform.TransformPoint(objData.position);
                                importedModel.transform.position = adjustedPosition;
                                importedModel.transform.rotation = objData.rotation;
                                importedModel.transform.localScale = objData.scale;
                                importedModel.tag = objData.type;
                                importedModel.name = objData.name;
                                importedModel.isStatic = true;
                                
                                // Hide only the mesh while keeping colliders active
                                // MeshRenderer[] meshRenderers = importedModel.GetComponentsInChildren<MeshRenderer>();
                                // foreach (MeshRenderer meshRenderer in meshRenderers)
                                // {
                                //     meshRenderer.enabled = false;
                                // }
                                importedModel.SetActive(true);

                                // Ensure the object has a collider
                                AddCollidersRecursively(meshObject);
                            }
                        }
                    }
                }
            }
            SendDestinationLabelsToFlutter();

            // Add NavMeshSurface to the root of the imported objects
            NavMeshSurface navMeshSurface = importedEnvironment.AddComponent<NavMeshSurface>();

            // Bake after a short delay to ensure all objects are included
            Invoke(nameof(BakeNavMesh), 1.0f);

            isSceneLoaded = true;

            // Set default navigation path after NavMesh is ready
            Invoke(nameof(SetDefaultNavigationPath), 2.0f);
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

    // Bake NavMesh for all imported objects
    void BakeNavMesh()
    {
        NavMeshSurface navMeshSurface = FindObjectOfType<NavMeshSurface>();
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baked successfully!");

            // Ensure NavMeshAgent can move on the surface
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
                navMeshAgent.enabled = true;
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

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        return new Bounds(Vector3.zero, Vector3.zero);
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
        }
        else
        {
            Debug.LogError("Failed to calculate path.");
        }
    }

    private GameObject LoadModelFromPath(string path)
    {
        // Use your preferred method to load models (e.g., ObjImporter or external library)
        return new GameObject(Path.GetFileNameWithoutExtension(path)); // Placeholder for demonstration
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
