using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using TMPro;
using Dummiesman;
using FlutterUnityIntegration;
using System.IO.Compression;
using UnityEngine.EventSystems;
using System.Linq; 

public class SceneController : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial; 
    public GameObject navigationPointPrefab;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Camera mainCamera;
    private GameObject selectedObject;
    private GameObject lastSelectedObject;
    private OBJLoader objLoader = new OBJLoader();
    public GameObject canvasUI;
    // Variable to lock inputs
    private bool isInputLocked = false;
    // Assign in the inspector
    [SerializeField] private Camera miniCamera;
    // Create and assign in Unity Editor
    [SerializeField] private RenderTexture miniCameraRenderTexture; 

    void Start()
    {
        mainCamera = Camera.main;
        SetupMiniCamera();
    }

    void Update()
    {
        UpdateMiniCameraView();
        // Check if inputs are locked or UI is active
        if (isInputLocked || EventSystem.current.currentSelectedGameObject != null)
        {
            return;
        }

        HandleObjectSelection();
        HandleObjectManipulation();
        HandleCameraControls();
    }

    private void SetupMiniCamera()
    {
        if (miniCamera != null)
        {
            // Set the RenderTexture to mini camera
            miniCamera.targetTexture = miniCameraRenderTexture;
        }
        else
        {
            Debug.LogError("Mini camera is not assigned!");
        }
    }

    private void UpdateMiniCameraView()
    {
        if (miniCamera != null && mainCamera != null)
        {
            // Optional: Match the mini camera's position and rotation with the main camera
            miniCamera.transform.position = mainCamera.transform.position;
            miniCamera.transform.rotation = mainCamera.transform.rotation;
        }
    }

    public void LockInput(bool lockInput)
    {
        isInputLocked = lockInput;
    }

    // Method to add a navigation point dynamically
    public void CreateNavigationPoint(string label, bool isSource, bool isDestination)
    {
        try
        {
            // Instantiate the navigation point prefab at the current camera position
            Vector3 spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * 2;
            GameObject newPoint = Instantiate(navigationPointPrefab, spawnPosition, Quaternion.identity);
            newPoint.tag = "NavigationLine";

            // Set metadata using the NavigationPoint script
            NavigationPoint navPoint = newPoint.GetComponent<NavigationPoint>();
            navPoint.SetData(label, isSource, isDestination);

            // Update the label text
            TextMeshPro labelComponent = newPoint.transform.Find("LabelText").GetComponent<TextMeshPro>();
            labelComponent.text = label;

            // Align the text above the navigation point
            labelComponent.transform.localPosition = new Vector3(0, 1.0f, 0);

            // Add to the list of spawned objects
            spawnedObjects.Add(newPoint);
            newPoint.name = label;

            Debug.Log($"Added Navigation Point: {label} (Source: {isSource}, Destination: {isDestination})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create navigation point: {ex.Message}");
        }
    }

    public void ShowUnityUI()
    {
        if (canvasUI != null)
        {
            canvasUI.SetActive(true);
        }
    }

    private void HandleObjectSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {   
                if (selectedObject != null)
                {
                    DeselectObject(selectedObject);
                }

                selectedObject = hit.collider.gameObject;
                SelectObject(selectedObject);

                // Ensure the selected object is the root by climbing up the parent chain
                while (selectedObject.transform.parent != null)
                {
                    selectedObject = selectedObject.transform.parent.gameObject;
                }

                if (selectedObject != lastSelectedObject)
                {
                    lastSelectedObject = selectedObject;
                }
            }
        }
    }

    private void SelectObject(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow; // Highlight selected object
        }
    }

    private void DeselectObject(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white; // Reset color
        }
    }

    private void HandleObjectManipulation()
    {
        if (selectedObject == null) return;

        float rotationSpeed = 100.0f;
        float scaleSpeed = 0.01f;
        float moveSpeed = 0.1f;
        bool transformationOccurred = false;

        // ----- Rotation -----
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Input.GetKey(KeyCode.Z))
        {
            // Rotate around Y-axis
            selectedObject.transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
            transformationOccurred = true;
        }
        if (Input.GetKey(KeyCode.X))
        {
            // Rotate around X-axis
            selectedObject.transform.Rotate(Vector3.right, mouseY * rotationSpeed * Time.deltaTime, Space.World);
            transformationOccurred = true;
        }
        if (Input.GetKey(KeyCode.C))
        {
            // Rotate around Z-axis
            selectedObject.transform.Rotate(Vector3.forward, mouseX * rotationSpeed * Time.deltaTime, Space.World);
            transformationOccurred = true;
        }

        // ----- Flipping -----
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // Flip along X-axis
            selectedObject.transform.localScale = new Vector3(
                -selectedObject.transform.localScale.x,
                selectedObject.transform.localScale.y,
                selectedObject.transform.localScale.z
            );
            transformationOccurred = true;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            // Flip along Y-axis
            selectedObject.transform.localScale = new Vector3(
                selectedObject.transform.localScale.x,
                -selectedObject.transform.localScale.y,
                selectedObject.transform.localScale.z
            );
            transformationOccurred = true;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            // Flip along Z-axis
            selectedObject.transform.localScale = new Vector3(
                selectedObject.transform.localScale.x,
                selectedObject.transform.localScale.y,
                -selectedObject.transform.localScale.z
            );
            transformationOccurred = true;
        }


        // ----- Scaling -----
        // Uniform scale
        if (Input.GetKey(KeyCode.U))
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                selectedObject.transform.localScale += new Vector3(scaleSpeed, scaleSpeed, scaleSpeed);
                transformationOccurred = true;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                selectedObject.transform.localScale -= new Vector3(scaleSpeed, scaleSpeed, scaleSpeed);
                transformationOccurred = true;
            }
        }
        else
        {
            // Individual axis scaling
            // Scale X-axis
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.UpArrow))
            {
                selectedObject.transform.localScale += new Vector3(scaleSpeed, 0, 0);
                transformationOccurred = true;
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.DownArrow))
            {
                selectedObject.transform.localScale -= new Vector3(scaleSpeed, 0, 0);
                transformationOccurred = true;
            }

            // Scale Y-axis
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKey(KeyCode.UpArrow))
            {
                selectedObject.transform.localScale += new Vector3(0, scaleSpeed, 0);
                transformationOccurred = true;
            }
            else if (Input.GetKey(KeyCode.RightControl) && Input.GetKey(KeyCode.DownArrow))
            {
                selectedObject.transform.localScale -= new Vector3(0, scaleSpeed, 0);
                transformationOccurred = true;
            }

            // Scale Z-axis
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.RightArrow))
            {
                selectedObject.transform.localScale += new Vector3(0, 0, scaleSpeed);
                transformationOccurred = true;
            }
            else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftArrow))
            {
                selectedObject.transform.localScale -= new Vector3(0, 0, scaleSpeed);
                transformationOccurred = true;
            }
        }
        // Prevent negative or too small scaling
        selectedObject.transform.localScale = new Vector3(
            Mathf.Max(selectedObject.transform.localScale.x, 0.1f),
            Mathf.Max(selectedObject.transform.localScale.y, 0.1f),
            Mathf.Max(selectedObject.transform.localScale.z, 0.1f)
        );

        // ----- Movement -----
        if (Input.GetMouseButton(1))
        {
            float horizontal = Input.GetAxis("Mouse X") * moveSpeed;
            float vertical = Input.GetAxis("Mouse Y") * moveSpeed;
            float upDown = 0f;

            // Use Q and E keys to move up and down
            if (Input.GetKey(KeyCode.Q))
            {
                upDown = moveSpeed;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                upDown = -moveSpeed;
            }

            // Move object along X, Y, and Z axes
            if (Input.GetKey(KeyCode.M)) // Use M key to move up along Y-axis
            {
                selectedObject.transform.Translate(Vector3.up * moveSpeed, Space.World);
            }
            else if (Input.GetKey(KeyCode.N)) // Use N key to move down along Y-axis
            {
                selectedObject.transform.Translate(Vector3.down * moveSpeed, Space.World);
            }

            transformationOccurred = true;
            selectedObject.transform.Translate(new Vector3(horizontal, upDown, vertical), Space.World);
        }

        // ----- Delete Object -----
        if (Input.GetKeyDown(KeyCode.Delete) && selectedObject != null)
        {
            spawnedObjects.Remove(selectedObject); // Remove from list
            Destroy(selectedObject); // Remove from scene
            selectedObject = null; // Reset selected object
            transformationOccurred = true;
        }

        // After transformations, directly update the corresponding object in the spawnedObjects list
        if (transformationOccurred)
        {
            UpdateObjectTransform(selectedObject);
        }
    }

    private void UpdateObjectTransform(GameObject obj)
    {
        // Assuming each object has a unique name or identifier for comparison
        GameObject foundObj = spawnedObjects.FirstOrDefault(o => o.name == obj.name);
        if (foundObj != null)
        {
            // Update the transformation directly in the spawnedObjects list
            foundObj.transform.position = obj.transform.position;
            foundObj.transform.rotation = obj.transform.rotation;
            foundObj.transform.localScale = obj.transform.localScale;
            Debug.Log($"obj.transform.position: {obj.transform.position},\n obj.transform.rotation {obj.transform.rotation},\n {obj.transform.localScale}");
        }
        else
        {
            Debug.LogError("Object not found in spawnedObjects list.");
        }
    }

    private void HandleCameraControls()
    {
        // Camera view controls
        if (Input.GetKeyDown(KeyCode.T)) // Top view
        {
            mainCamera.transform.position = new Vector3(0, 10, 0);
            mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.B)) // Bottom view
        {
            mainCamera.transform.position = new Vector3(0, -10, 0);
            mainCamera.transform.rotation = Quaternion.Euler(-90, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.F)) // Front view
        {
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.L)) // Left view
        {
            mainCamera.transform.position = new Vector3(-10, 0, 0);
            mainCamera.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (Input.GetKeyDown(KeyCode.R)) // Right view
        {
            mainCamera.transform.position = new Vector3(10, 0, 0);
            mainCamera.transform.rotation = Quaternion.Euler(0, -90, 0);
        }

        // Camera movement controls
        if (Input.GetKey(KeyCode.W)) mainCamera.transform.Translate(Vector3.forward * Time.deltaTime);
        if (Input.GetKey(KeyCode.S)) mainCamera.transform.Translate(Vector3.back * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) mainCamera.transform.Translate(Vector3.left * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) mainCamera.transform.Translate(Vector3.right * Time.deltaTime);
        if (Input.GetKey(KeyCode.Q)) mainCamera.transform.Rotate(Vector3.up, -1);
        if (Input.GetKey(KeyCode.E)) mainCamera.transform.Rotate(Vector3.up, 1);
        if (Input.GetKey(KeyCode.Space)) mainCamera.transform.Translate(Vector3.up * Time.deltaTime);
        if (Input.GetKey(KeyCode.LeftShift)) mainCamera.transform.Translate(Vector3.down * Time.deltaTime);
    }

    // Update the LoadModelFromUrl to handle base64 string
    public void LoadModelFromBase64(string base64String)
    {
        #if UNITY_WEBGL
        StartCoroutine(DownloadAndLoadModelFromBase64(base64String));
        #endif
    }

    private IEnumerator DownloadAndLoadModelFromBase64(string base64String)
    {
        byte[] modelData = Convert.FromBase64String(base64String);

        using (MemoryStream stream = new MemoryStream(modelData))
        {
            GameObject importedModel = objLoader.Load(stream);

            if (importedModel != null)
            {
                // Assign a unique name to the imported object
                string uniqueName = Guid.NewGuid().ToString();
                importedModel.name = uniqueName;
                
                AssignDefaultShader(importedModel);
                importedModel.transform.position = Vector3.zero;

                // Send scale and rotation information back to Flutter
                string logMessages = $"Model Scale: {importedModel.transform.localScale}, Rotation: {importedModel.transform.rotation.eulerAngles}";
                UnityMessageManager.Instance.SendMessageToFlutter(logMessages); 

                #if UNITY_WEBGL
                    // Flip the model along the X-axis to correct the mirroring issue
                    importedModel.transform.localScale = new Vector3(
                        -importedModel.transform.localScale.x,
                        importedModel.transform.localScale.y,
                        importedModel.transform.localScale.z
                    );
                #endif

                #if UNITY_WEBGL
                    importedModel.transform.Rotate(0, 180, 0);
                #endif

                // Fit the imported model into the camera view
                FitObjectToCamera(importedModel);

                AddCollidersRecursively(importedModel);

                importedModel.tag = "ImportedObject";
                spawnedObjects.Add(importedModel);
                // Send scale and rotation information back to Flutter
                string logMessage = $"Model Scale: {importedModel.transform.localScale}, Rotation: {importedModel.transform.rotation.eulerAngles}";
                UnityMessageManager.Instance.SendMessageToFlutter(logMessage); 
            }
            else
            {
                Debug.LogError("Failed to load the 3D model .");
            }
        }
        yield return null;
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

    private void FitObjectToCamera(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = objectSize / (2f * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));

        mainCamera.transform.position = bounds.center - distance * mainCamera.transform.forward;
        mainCamera.nearClipPlane = Mathf.Max(0.01f, distance - objectSize * 1.5f);
        mainCamera.farClipPlane = distance + objectSize * 2f;
    }

    // Helper method to check if an object has any negative scale values
    private bool HasNegativeScale(GameObject obj)
    {
        Vector3 scale = obj.transform.localScale;
        return scale.x < 0 || scale.y < 0 || scale.z < 0;
    }

    // Helper method to calculate the bounds of the model for the BoxCollider
    private Vector3 CalculateBounds(GameObject model)
    {
        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Use absolute values to avoid negative collider sizes
        return new Vector3(Mathf.Abs(bounds.size.x), Mathf.Abs(bounds.size.y), Mathf.Abs(bounds.size.z));
    }

    // Assigns a default white color shader to the imported model if it lacks one
    private void AssignDefaultShader(GameObject obj)
    {
        // Ensure default material is set
        if (defaultMaterial == null)
        {
            // Attempt to load the Standard shader if defaultMaterial is missing
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                defaultMaterial = new Material(standardShader);
            }
            else
            {
                Debug.LogError("Standard shader not found. Please assign a default material.");
                return;
            }
        }

        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            if (renderer.material == null)
            {
                renderer.material = defaultMaterial;
            }
        }
    }

    public void HideMeshRenderer()
    {
        if (selectedObject != null)
        {
            MeshRenderer renderer = selectedObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = !renderer.enabled;
            }
        }
    }

    public void ExportScene()
    {
        try
        {
            // Prepare the scene data
            SceneData sceneData = new SceneData();
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj.CompareTag("NavigationLine"))
                {
                    // Handle Navigation Points
                    NavigationPoint navPoint = obj.GetComponent<NavigationPoint>();
                    SceneObjectData navData = new SceneObjectData
                    {
                        position = obj.transform.position,
                        rotation = obj.transform.rotation,
                        scale = obj.transform.localScale,
                        type = "NavigationLine",
                        label = obj.GetComponentInChildren<TextMeshPro>()?.text ?? "",
                        isSource = obj.GetComponent<NavigationPoint>()?.IsSource ?? false,
                        isDestination = obj.GetComponent<NavigationPoint>()?.IsDestination ?? false
                    };
                    sceneData.objects.Add(navData);
                }
                else if (obj.CompareTag("ImportedObject"))
                {
                    // Handle Imported 3D Objects
                    SceneObjectData objData = new SceneObjectData
                    {
                        position = obj.transform.position,
                        rotation = obj.transform.rotation,
                        scale = obj.transform.localScale,
                        type = obj.name,
                        isSource = false,
                        isDestination = false
                    };
                    Debug.Log("Exported Scene Data: " + objData);
                    sceneData.objects.Add(objData);
                }
            }

            // Serialize the scene data to JSON
            string json = JsonUtility.ToJson(sceneData);

            // Byte array to store the zip data
            byte[] zipData;

            // Create the ZIP archive
            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    // Add the JSON metadata file
                    ZipArchiveEntry sceneDataEntry = archive.CreateEntry("sceneData.json");
                    using (StreamWriter writer = new StreamWriter(sceneDataEntry.Open()))
                    {
                        writer.Write(json);
                    }

                    // Add 3D model files (OBJ)
                    foreach (GameObject obj in spawnedObjects)
                    {
                        if (obj.CompareTag("ImportedObject"))
                        {
                            string objFileName = $"{obj.name}.obj";
                            ZipArchiveEntry objEntry = archive.CreateEntry(objFileName);
                            using (StreamWriter writer = new StreamWriter(objEntry.Open()))
                            {
                                OBJExporter.Export(obj, writer);
                            }
                        }
                    }
                }

                // Save the zip data to the byte array
                zipData = zipStream.ToArray();
            }

            // Trigger the browser download
            #if UNITY_WEBGL
            string base64Zip = Convert.ToBase64String(zipData);
            Application.ExternalEval($@"
                var blob = new Blob([Uint8Array.from(atob('{base64Zip}').split('').map(c => c.charCodeAt(0)))], {{ type: 'application/zip' }});
                var link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                link.download = 'SceneExport.zip';
                link.click();
            ");
            #endif

            Debug.Log("Scene exported successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to export scene: {ex.Message}");
        }
    }

    public void ImportSceneFromBase64(string base64String)
    {
        try
        {
            ClearTempFolder();
            
            // Decode the base64 string into a byte array
            byte[] zipBytes = Convert.FromBase64String(base64String);

            // Write the byte array to a temporary zip file
            string tempZipPath = Path.Combine(Application.persistentDataPath, "TempScene.zip");
            File.WriteAllBytes(tempZipPath, zipBytes);

            // Extract and import the scene
            ImportScene(tempZipPath);

            // Clean up the temporary file
            File.Delete(tempZipPath);

            Debug.Log("Scene imported successfully from base64.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import scene from base64: {ex.Message}");
        }
    }

    public void ImportScene(string zipFilePath)
    {
        try
        {
            // Generate a unique folder name
            string tempFolder = Path.Combine(Application.persistentDataPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            // Extract the ZIP file
            ZipFile.ExtractToDirectory(zipFilePath, tempFolder);
            // Read the scene metadata
            string jsonPath = Path.Combine(tempFolder, "sceneData.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError("Scene metadata not found.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

            if (sceneData.objects == null || sceneData.objects.Count == 0)
            {
                Debug.LogError("No objects found in the scene metadata.");
                return;
            }

            // Load objects from metadata
            foreach (var objData in sceneData.objects)
            {
                if (objData.type == "NavigationLine")
                {
                    GameObject navPoint = Instantiate(navigationPointPrefab, objData.position, objData.rotation);
                    navPoint.transform.localScale = objData.scale;
                    var textMesh = navPoint.GetComponentInChildren<TextMeshPro>();
                    textMesh.text = objData.label;
                }
                else
                {
                    string modelPath = Path.Combine(tempFolder, $"{objData.type}.obj");

                    if (File.Exists(modelPath))
                    {
                        GameObject importedModel = objLoader.Load(modelPath);

                        if (importedModel != null)
                        {
                            // Find the object with MeshRenderer or MeshFilter
                            GameObject meshObject = FindMeshObject(importedModel);

                            if (meshObject != null)
                            {
                                // Apply transformations to the correct object
                                importedModel.transform.position = objData.position;
                                importedModel.transform.rotation = objData.rotation;
                                importedModel.transform.localScale = objData.scale;
                                importedModel.tag = "ImportedObject";
                                importedModel.name = objData.type;

                                // Ensure the object has a collider
                                AddCollidersRecursively(meshObject);
                                spawnedObjects.Add(importedModel);
                            }
                            else
                            {
                                Debug.LogError($"No MeshRenderer or MeshFilter found in the hierarchy of {importedModel.name}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to load model from path: {modelPath}");
                        }
                    }
                }
            }

            // Cleanup temporary folder
            Directory.Delete(tempFolder, true);
            Debug.Log("Scene imported successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to import scene: {ex.Message}");
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

    public void ClearTempFolder()
    {
        string tempFolder = Path.Combine(Application.persistentDataPath, "TempSceneImport");
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true); // Deletes all files and subdirectories
            Debug.Log("Temporary folder cleared.");
        }
    }

    private void SaveModelAsObj(GameObject obj, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            OBJExporter.Export(obj, writer);
        }
    }

    [System.Serializable]
    public class PointData
    {
        public string label;
        public bool isSource;
        public bool isDestination;
        public float[] position;
    }

    [System.Serializable]
    public class SceneData
    {
        public List<SceneObjectData> objects = new List<SceneObjectData>();
    }

    [System.Serializable]
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
}

public static class OBJExporter
{
    public static void Export(GameObject obj, StreamWriter writer)
    {
        // Recursively find all MeshFilters in the hierarchy
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length == 0)
        {
            Debug.LogError($"Failed to export {obj.name}: No MeshFilter or sharedMesh found in hierarchy.");
            return;
        }

        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                Debug.LogWarning($"Skipping {meshFilter.gameObject.name}: Missing sharedMesh.");
                continue;
            }

            // Write mesh data
            foreach (Vector3 v in mesh.vertices)
            {
                writer.WriteLine($"v {v.x} {v.y} {v.z}");
            }

            foreach (Vector3 n in mesh.normals)
            {
                writer.WriteLine($"vn {n.x} {n.y} {n.z}");
            }

            foreach (Vector2 uv in mesh.uv)
            {
                writer.WriteLine($"vt {uv.x} {uv.y}");
            }

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int[] triangles = mesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    writer.WriteLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
                }
            }
        }
    }
}
