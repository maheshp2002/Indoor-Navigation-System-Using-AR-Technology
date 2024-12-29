// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using Firebase.Storage;

// public class SceneExporter : MonoBehaviour
// {
    // public List<GameObject> spawnedObjects = new List<GameObject>(); // List of all spawned objects
    // public string firebaseStorageBucket = "your-firebase-app-id.appspot.com"; // Firebase Storage bucket URL

    // [System.Serializable]
    // public class SceneObjectData
    // {
    //     public string label; // Object name or label
    //     public Vector3 position; // Position of the object
    //     public Quaternion rotation; // Rotation of the object
    //     public Vector3 scale; // Scale of the object
    //     public string type; // Object type (e.g., Source, Destination, 3D model)
    //     public string modelUrl; // Firebase URL to the .obj file (if applicable)
    // }

    // [System.Serializable]
    // public class SceneData
    // {
    //     public List<SceneObjectData> objects = new List<SceneObjectData>();
    // }

    // public void ExportScene()
    // {
    //     StartCoroutine(ExportAndUpload());
    // }

    // private IEnumerator ExportAndUpload()
    // {
    //     try
    //     {
    //         // Prepare metadata for the scene
    //         SceneData sceneData = new SceneData();
    //         string sceneFolder = $"scenes/{System.Guid.NewGuid()}";

    //         foreach (GameObject obj in spawnedObjects)
    //         {
    //             string modelUrl = string.Empty;

    //             // If the object has a mesh and needs to save its model
    //             if (obj.CompareTag("3DModel"))
    //             {
    //                 string localPath = SaveModelAsObj(obj, sceneFolder);
    //                 yield return StartCoroutine(UploadFileToFirebase(localPath, $"{sceneFolder}/{obj.name}.obj", url => modelUrl = url));
    //             }

    //             SceneObjectData data = new SceneObjectData
    //             {
    //                 label = obj.name,
    //                 position = obj.transform.position,
    //                 rotation = obj.transform.rotation,
    //                 scale = obj.transform.localScale,
    //                 type = obj.tag, // Example: "Source", "Destination", "3DModel"
    //                 modelUrl = modelUrl
    //             };

    //             sceneData.objects.Add(data);
    //         }

    //         // Serialize scene data to JSON
    //         string json = JsonUtility.ToJson(sceneData, true);
    //         string metadataPath = Path.Combine(Application.persistentDataPath, "sceneData.json");
    //         File.WriteAllText(metadataPath, json);

    //         // Upload metadata JSON to Firebase
    //         yield return StartCoroutine(UploadFileToFirebase(metadataPath, $"{sceneFolder}/sceneData.json", url =>
    //         {
    //             if (!string.IsNullOrEmpty(url))
    //                 Debug.Log($"Scene metadata uploaded: {url}");
    //         }));

    //         Debug.Log($"Scene exported and uploaded successfully: {sceneFolder}");
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"Failed to export scene: {ex.Message}");
    //     }
    // }

    // private string SaveModelAsObj(GameObject obj, string sceneFolder)
    // {
    //     // Use a 3rd-party library or custom logic to export the mesh as an .obj file
    //     string localPath = Path.Combine(Application.persistentDataPath, $"{obj.name}.obj");
    //     using (StreamWriter writer = new StreamWriter(localPath))
    //     {
    //         // Example of using Dummiesman OBJExporter
    //         OBJExporter.Export(obj, writer);
    //     }
    //     Debug.Log($"Model saved locally: {localPath}");
    //     return localPath;
    // }

    // private IEnumerator UploadFileToFirebase(string localPath, string cloudPath, System.Action<string> onComplete)
    // {
    //     FirebaseStorage storage = FirebaseStorage.DefaultInstance;
    //     StorageReference storageRef = storage.GetReferenceFromUrl($"gs://{firebaseStorageBucket}");
    //     StorageReference fileRef = storageRef.Child(cloudPath);

    //     var uploadTask = fileRef.PutFileAsync(localPath);
    //     yield return new WaitUntil(() => uploadTask.IsCompleted);

    //     if (uploadTask.Exception != null)
    //     {
    //         Debug.LogError($"Failed to upload file to Firebase: {uploadTask.Exception}");
    //         onComplete?.Invoke(null);
    //         yield break;
    //     }

    //     var getUrlTask = fileRef.GetDownloadUrlAsync();
    //     yield return new WaitUntil(() => getUrlTask.IsCompleted);

    //     if (getUrlTask.Exception == null)
    //     {
    //         string downloadUrl = getUrlTask.Result.ToString();
    //         Debug.Log($"File uploaded: {downloadUrl}");
    //         onComplete?.Invoke(downloadUrl);
    //     }
    //     else
    //     {
    //         Debug.LogError($"Failed to get file URL: {getUrlTask.Exception}");
    //         onComplete?.Invoke(null);
    //     }
    // }
// }
