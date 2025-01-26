using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using FlutterUnityIntegration;
using TMPro;

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
}

public class NavigationController : MonoBehaviour
{
    [SerializeField] private UnityMessageSender unityMessageSender;
    public GameObject xrOrigin; // XR Origin GameObject
    public GameObject navigationPointPrefab; // Prefab for navigation points (Source/Destination)
    public LineRenderer pathLine; // LineRenderer for navigation

    private Dictionary<string, Transform> destinationPoints = new Dictionary<string, Transform>();
    private UnityEngine.AI.NavMeshAgent navMeshAgent;

    void Start()
    {
        // Ensure the XR Origin has a NavMeshAgent
        navMeshAgent = xrOrigin.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent == null)
        {
            navMeshAgent = xrOrigin.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navMeshAgent.stoppingDistance = 0.5f; // Optional: Adjust as needed
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

            // Root GameObject for the environment
            GameObject environmentRoot = new GameObject("Environment");

            foreach (var objData in sceneData.objects)
            {
                if (objData.isSource)
                {
                    // Place XR Origin at the source position
                    xrOrigin.transform.position = objData.position;
                    xrOrigin.transform.rotation = objData.rotation;
                    xrOrigin.transform.localScale = objData.scale;
                }
                else if (objData.isDestination)
                {
                    // Instantiate destination markers
                    GameObject destination = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
                    destination.transform.localScale = objData.scale;
                    destination.tag = objData.type;
                    destination.name = objData.label;
                    var textMesh = destination.GetComponentInChildren<TextMeshPro>();
                    textMesh.text = objData.label;
                    destinationPoints.Add(objData.label, destination.transform);
                    Debug.Log($"destination {objData.label}, {destination.tag}");
                }
                else
                {
                    // Import 3D objects from the zip folder
                    string modelPath = Path.Combine(tempFolder, $"{objData.type}.obj");
                    if (File.Exists(modelPath))
                    {
                        GameObject importedModel = LoadModelFromPath(modelPath); // Replace with your model loader
                        importedModel.transform.position = objData.position;
                        importedModel.transform.rotation = objData.rotation;
                        importedModel.transform.localScale = objData.scale;
                        importedModel.SetActive(false); // Hide the object in AR
                        importedModel.transform.SetParent(environmentRoot.transform); // Add to environment root
                    }
                }
            }
            SendDestinationLabelsToFlutter();

            // Add NavMeshSurface to the root of the imported environment
            NavMeshSurface navMeshSurface = environmentRoot.AddComponent<NavMeshSurface>();
            navMeshSurface.BuildNavMesh();
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

    private void ShowNavigationPath(Vector3 targetPosition)
    {
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
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
        List<string> destinationLabels = destinationPoints.Keys.ToList();

        // Convert the list to JSON
        string jsonLabels = JsonUtility.ToJson(new { destinations = destinationLabels });
        Debug.Log($"Destination Points: {string.Join(", ", destinationPoints.Keys)}, \n {jsonLabels}");

        unityMessageSender.SendMessageToFlutter(jsonLabels);

    }
}
