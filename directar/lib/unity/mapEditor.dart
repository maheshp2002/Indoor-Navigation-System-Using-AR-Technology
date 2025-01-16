import 'dart:convert';
import 'dart:typed_data';
import 'package:directar/components/Toast.dart';
import 'package:directar/config/constants.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:file_picker/file_picker.dart';
import 'package:firebase_storage/firebase_storage.dart';
import 'package:ulid/ulid.dart';
import '../services/firebaseService.dart';
import 'package:http/http.dart' as http;

class MapEditor extends StatefulWidget {
  final String? mapsDocumentId;
  final String? mapsUrl;
  final bool isEditMode;

  const MapEditor(
      {super.key,
      required this.mapsDocumentId,
      required this.mapsUrl,
      required this.isEditMode});

  @override
  MapEditorState createState() => MapEditorState();
}

class MapEditorState extends State<MapEditor> {
  late UnityWidgetController _unityController;
  String? email = FirebaseAuth.instance.currentUser?.email;
  final FirebaseService _firebaseService = FirebaseService();
  bool _isUnityReady = false;
  bool _isHelpVisible = false;
  String? _mapsDocumentId;
  String? _mapsUrl;

  final List<String> controls = [
    'W: Move Camera Forward',
    'S: Move Camera Backward',
    'A: Move Camera Left',
    'D: Move Camera Right',
    'Q: Rotate Camera Left',
    'E: Rotate Camera Right',
    'Space: Move Camera Up',
    'Left Shift: Move Camera Down',
    'Mouse Left Button: Select Object',
    'Mouse Right Button: Move Selected Object',
    'Z: Rotate Selected Object Around Y-axis',
    'X: Rotate Selected Object Around X-axis',
    'C: Rotate Selected Object Around Z-axis',
    'U + Up Arrow: Uniform Scale Up',
    'U + Down Arrow: Uniform Scale Down',
    'Ctrl + Up Arrow: Scale Up Along X-axis',
    'Ctrl + Down Arrow: Scale Down Along X-axis',
    'Right Ctrl + Up Arrow: Scale Up Along Y-axis',
    'Right Ctrl + Down Arrow: Scale Down Along Y-axis',
    'Alt + Right Arrow: Scale Up Along Z-axis',
    'Alt + Left Arrow: Scale Down Along Z-axis',
    'F1: Flip Object Along X-axis',
    'F2: Flip Object Along Y-axis',
    'F3: Flip Object Along Z-axis',
    'M: Move Object Up Along Y-axis',
    'N: Move Object Down Along Y-axis',
    'Delete: Delete Selected Object',
    'T: Top View',
    'B: Bottom View',
    'F: Front View',
    'L: Left View',
    'R: Right View',
    'Back: Back View',
  ];

  @override
  void initState() {
    super.initState();
    _mapsDocumentId = widget.mapsDocumentId;
    _mapsUrl = widget.mapsUrl;
  }

  Future<void> _loadInitialSceneFromUrl(String mapsUrl) async {
    try {
      // Use `http` to fetch the file via the download URL
      final response = await http.get(Uri.parse(mapsUrl));

      if (response.statusCode == 200) {
        final base64String = base64Encode(response.bodyBytes);
        _unityController.postMessage(
            'SceneController', 'ImportSceneFromBase64', base64String);
      } else {
        showToast('Internal Server Error', isSuccess: false);
      }
    } catch (e) {
      print('Error loading initial scene: $e');
      showToast('Internal Server Error', isSuccess: false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Map 3D Editor'),
      ),
      body: Row(
        children: [
          Expanded(
            child: UnityWidget(
              onUnityCreated: onUnityCreated,
              onUnityMessage: onUnityMessage,
            ),
          ),
          AnimatedContainer(
            duration: const Duration(milliseconds: 300),
            width: _isHelpVisible ? 300 : 50,
            child: Column(
              children: [
                IconButton(
                  icon: Icon(
                    _isHelpVisible ? Icons.expand_less : Icons.expand_more,
                  ),
                  onPressed: () {
                    setState(() {
                      _isHelpVisible = !_isHelpVisible;
                    });
                  },
                ),
                if (_isHelpVisible)
                  Expanded(
                    child: Container(
                      padding: const EdgeInsets.all(10),
                      color: Colors.grey[200],
                      child: ListView.builder(
                        itemCount: controls.length,
                        itemBuilder: (context, index) {
                          return Text(controls[index]);
                        },
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
      bottomNavigationBar: Row(
        mainAxisAlignment: MainAxisAlignment.spaceAround,
        children: [
          ElevatedButton(
            onPressed: _importObject,
            child: Text("Import Object"),
          ),
          ElevatedButton(
            onPressed: _showUnityUI,
            child: Text("Add Navigation Point"),
          ),
          ElevatedButton(
            onPressed: _hideObject,
            child: Text("Hide Object"),
          ),
          ElevatedButton(
            onPressed: _deleteObject,
            child: Text("Delete Object"),
          ),
          ElevatedButton(
            onPressed: _exportScene,
            child: Text("Export Scene"),
          ),
          ElevatedButton(
            onPressed: _importScene,
            child: Text("Import Scene"),
          ),
          ElevatedButton(
            onPressed: _generateQr,
            child: Text("Generate QR"),
          ),
        ],
      ),
    );
  }

  void onUnityCreated(UnityWidgetController controller) {
    _unityController = controller;
    _isUnityReady = true;

    if (widget.isEditMode && _mapsUrl != null && _mapsUrl != null) {
      _loadInitialSceneFromUrl(widget.mapsUrl!);
    }
  }

  Future<void> onUnityMessage(dynamic message) async {
    print('Received message from Unity: $message');
    var messageMap = jsonDecode(message);
    if (email != null) {
      if (messageMap['type'] == 'ExportScene') {
        String zipBase64 = messageMap['data'];
        // Upload to Firebase
        await uploadToFirebase(
          email: email!,
          base64Data: zipBase64,
          storagePath: FirebaseConstants.mapsCollection,
          fileExtension: 'zip',
          firestoreCollection: FirebaseConstants.mapsCollection,
        );
      }
    }
  }

  void _importObject() async {
    if (!_isUnityReady) {
      showToast("Unity is not ready.");
      return;
    }

    FilePickerResult? result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['obj', 'fbx', 'glb'],
    );

    if (result != null) {
      Uint8List? fileBytes = result.files.first.bytes;

      if (fileBytes != null) {
        String base64String = base64Encode(fileBytes);
        print('File picked: ${result.files.first.name}');
        _unityController.postMessage(
            'SceneController', 'LoadModelFromBase64', base64String);
      } else {
        print('Failed to retrieve file bytes.');
      }
    } else {
      showToast('File picking was canceled.');
    }
  }

  void _showUnityUI() {
    if (_isUnityReady) {
      _unityController.postMessage('SceneController', 'ShowUnityUI', '');
    } else {
      showToast("Unity is not ready.");
    }
  }

  void _hideObject() {
    if (_isUnityReady) {
      _unityController.postMessage('SceneController', 'HideMeshRenderer', '');
    } else {
      showToast("Unity is not ready.");
    }
  }

  void _deleteObject() {
    if (_isUnityReady) {
      _unityController.postMessage(
          'SceneController', 'DeleteSelectedObject', '');
    } else {
      showToast("Unity is not ready.");
    }
  }

  void _exportScene() {
    if (!_isUnityReady) {
      showToast("Unity is not ready.");
      return;
    }

    _unityController.postMessage('SceneController', 'ExportScene', '');
  }

  void _importScene() async {
    if (!_isUnityReady) {
      showToast("Unity is not ready.");
      return;
    }

    FilePickerResult? result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['zip'],
    );

    if (result != null) {
      Uint8List? fileBytes = result.files.first.bytes;

      if (fileBytes != null) {
        // Convert the file bytes to base64
        String base64String = base64Encode(fileBytes);

        // Send the base64 string to Unity
        _unityController.postMessage(
            'SceneController', 'ImportSceneFromBase64', base64String);
      } else {
        print("Failed to retrieve file bytes.");
      }
    } else {
      showToast("No file selected.");
    }
  }

  Future<String?> _getMapZipUrl() async {
    if (_mapsDocumentId != null) {
      try {
        final doc = await _firebaseService.getMapDetails(
          email!,
          'maps',
          _mapsDocumentId!,
        );
        if (doc.exists) {
          return doc.data()?['url'] as String?;
        }
      } catch (e) {
        print('Error fetching map details: $e');
      }
    }
    return null;
  }

  void _generateQr() async {
    if (_isUnityReady) {
      String? sceneAccessURL = await _getMapZipUrl();
      if (sceneAccessURL != null) {
        _unityController.postMessage(
            'SceneController', 'ExportSceneAndGenerateQR', sceneAccessURL);
      }
    } else {
      showToast("Unity is not ready.");
    }
  }

  Future<void> uploadToFirebase({
    required String email,
    required String base64Data,
    required String storagePath,
    required String fileExtension,
    required String firestoreCollection,
  }) async {
    if (_mapsUrl != null) {
      // Delete existing file
      final fileRef = FirebaseStorage.instance.refFromURL(_mapsUrl!);
      await fileRef.delete();
    }

    // Generate a unique file name
    String uniqueFileName = "${Ulid()}.$fileExtension";

    // Decode the base64 data
    Uint8List fileBytes = base64Decode(base64Data);

    // Upload the file to Firebase Storage
    final fileRef =
        FirebaseStorage.instance.ref('$storagePath/$email/$uniqueFileName');
    final uploadTask = await fileRef.putData(fileBytes);
    final fileUrl = await uploadTask.ref.getDownloadURL();

    // Save the file details to Firestore
    final now = DateTime.now();
    final date =
        "${now.year}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}";
    final time =
        "${now.hour % 12 == 0 ? 12 : now.hour % 12}:${now.minute.toString().padLeft(2, '0')} ${now.hour >= 12 ? 'PM' : 'AM'}";

    if (!widget.isEditMode && _mapsDocumentId == null && _mapsUrl == null) {
      final Map<String, dynamic> data = {
        'url': fileUrl,
        'date': date,
        'time': time,
        'last_opened_time': now.toIso8601String(),
      };

      final docRef = await _firebaseService.saveMapDetails(
          email, firestoreCollection, data);
      final documentId = docRef.id;

      // Store the documentId in the state
      setState(() {
        _mapsDocumentId = documentId;
      });
    } else {
      await _firebaseService.updateMapDetails(
          email, firestoreCollection, _mapsDocumentId!, 'url', fileUrl);
    }

    // Store the fileUrl in the state
    setState(() {
      _mapsUrl = fileUrl;
    });
  }
}
