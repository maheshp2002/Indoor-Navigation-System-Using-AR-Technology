import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:archive/archive.dart';
import 'package:archive/archive_io.dart';
import 'package:path_provider/path_provider.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:provider/provider.dart';
import 'dart:io';
import 'components/Toast.dart';
import 'components/themeManager.dart';
import 'theme.dart';  
import 'package:http/http.dart' as http;
import 'dart:convert';

class ThreeDModel extends StatefulWidget {
  const ThreeDModel({super.key});

  @override
  State<ThreeDModel> createState() => ThreeDModelState();
}

class ThreeDModelState extends State<ThreeDModel> {
  List<File> _images = []; // Stores all captured images
  File? _zipFile; // Stores the created ZIP file

  @override
  void initState() {
    super.initState();
    _checkStoragePermission();
  }

  Future<void> _checkStoragePermission() async {
    var status = await Permission.storage.status;
    if (!status.isGranted) {
      await Permission.storage.request();
    }
  }

  Future<void> cameraOpen() async {
    final picker = ImagePicker();
    final pickedFile = await picker.pickImage(source: ImageSource.camera);

    if (pickedFile != null) {
      setState(() {
        _images.add(File(pickedFile.path)); // Add new image to the list
      });
    }
  }

  void _deleteImage(int index) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text("Delete Image"),
        content: const Text("Are you sure you want to delete this image?"),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text("Cancel"),
          ),
          TextButton(
            onPressed: () {
              setState(() {
                _images.removeAt(index);
              });
              Navigator.pop(context);
            },
            child: const Text("Delete",
                style: TextStyle(color: AppColors.primaryColor)),
          ),
        ],
      ),
    );
  }

  Future<void> _convertToZip() async {
    try {
      final archive = Archive();
      for (var image in _images) {
        List<int> imageBytes = await image.readAsBytes();
        archive.addFile(ArchiveFile(
            image.path.split('/').last, imageBytes.length, imageBytes));
      }

      final zipData = ZipEncoder().encode(archive);

      final directory = await getApplicationDocumentsDirectory();
      final zipFilePath = '${directory.path}/images.zip';
      final zipFile = File(zipFilePath);
      await zipFile.writeAsBytes(zipData);

      setState(() {
        _images.clear(); // Remove all images
        _zipFile = zipFile; // Store ZIP file reference
      });

      showToast("ZIP file saved at: $zipFilePath");

      _uploadZip();
    } catch (e) {
      print("Error creating ZIP: $e");
    }
  }
  
  Future<void> _uploadZip() async {
    if (_zipFile == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("No ZIP file found.")),
      );
      return;
    }

    var uri = Uri.parse("https://d7e6-103-154-37-247.ngrok-free.app/upload_and_process");
    var request = http.MultipartRequest("POST", uri);
    request.files.add(await http.MultipartFile.fromPath("file", _zipFile!.path));

    try {
      var response = await request.send();
      if (response.statusCode == 200) {
        var responseData = await response.stream.bytesToString();
        var jsonResponse = jsonDecode(responseData);

        if (jsonResponse.containsKey("zip_base64")) {
          String base64String = jsonResponse["zip_base64"];

          // Decode Base64 string to bytes
          List<int> zipBytes = base64Decode(base64String);

          // Get the Downloads folder
          Directory? downloadsDir = Directory('/storage/emulated/0/Download');
          if (!downloadsDir.existsSync()) {
            showToast("Download folder not found.");
            return;
          }

          String filePath = "${downloadsDir.path}/3D_Model.zip";
          File zipFile = File(filePath);

          // Write bytes to file
          await zipFile.writeAsBytes(zipBytes);

          showToast("File saved: $filePath");
        } else {
          showToast("Invalid response format.");
        }
      } else {
        showToast("Failed to generate 3D model.");
      }
    } catch (e) {
      showToast("Failed to generate 3D model.");
      print("Error: $e");
    }
  }

  Future<bool> _requestStoragePermission() async {
    var status = await Permission.storage.request();
    return status.isGranted;
  }
  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final themeManager = Provider.of<ThemeManager>(context);

    return Scaffold(
      appBar: AppBar(
        title: Center(
          child: Text("3D Model", style: theme.textTheme.displayMedium),
        ),
                iconTheme: theme.iconTheme,
        actions: [
          IconButton(
            icon: Icon(
              color: theme.iconTheme.color,
              themeManager.themeMode == ThemeMode.dark
                  ? Icons.dark_mode
                  : Icons.light_mode,
            ),
            onPressed: () {
              themeManager.toggleTheme();
            },
          ),
        ],
      ),
      body: Center(
        child: _zipFile != null
            ? Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.folder_zip_rounded,
                      size: 80, color: AppColors.linkColor),
                  const SizedBox(height: 10),
                  Text(
                    "ZIP File: ${_zipFile!.path.split('/').last}",
                    style: const TextStyle(
                        fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ],
              )
            : _images.isNotEmpty
                ? GridView.builder(
                    padding: const EdgeInsets.all(8),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 4,
                      crossAxisSpacing: 8,
                      mainAxisSpacing: 8,
                    ),
                    itemCount: _images.length,
                    itemBuilder: (context, index) {
                      return GestureDetector(
                        onLongPress: () => _deleteImage(index),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Stack(
                            children: [
                              Positioned.fill(
                                child: Image.file(_images[index],
                                    fit: BoxFit.cover),
                              ),
                              Positioned(
                                right: 4,
                                top: 4,
                                child: IconButton(
                                  icon: const Icon(Icons.delete,
                                      color: AppColors.primaryColor),
                                  onPressed: () => _deleteImage(index),
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  )
                : Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Image.asset("assets/gifs/3d-scan-env.gif", width: 150),
                    Text("Take multiple photos of you environment",
                      style: theme.textTheme.labelSmall,
                      textAlign: TextAlign.center
                    )
                  ]),
      ),
      floatingActionButton: Container(
        margin: const EdgeInsets.symmetric(horizontal: 16),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.end,
          children: [
            FloatingActionButton(
              onPressed: cameraOpen,
              tooltip: 'Open Camera',
              child: const Icon(Icons.camera_alt_rounded),
            ),
            const SizedBox(width: 10),
            if (_images.isNotEmpty)
              FloatingActionButton(
                onPressed: _convertToZip,
                tooltip: 'Convert to ZIP',
                child: const Icon(Icons.folder_zip_rounded),
              ),
          ],
        ),
      ),
    );
  }
}
