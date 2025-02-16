import 'dart:convert';
import 'package:directar/components/Toast.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import '../components/themeManager.dart';

class UserNavigation extends StatefulWidget {
  final String? mapsUrl;

  const UserNavigation({super.key, required this.mapsUrl});

  @override
  UserNavigationState createState() => UserNavigationState();
}

class UserNavigationState extends State<UserNavigation> {
  late UnityWidgetController _unityController;
  String? email = FirebaseAuth.instance.currentUser?.email;
  String? _mapsUrl;
  List<String> destinationList = [];
  String? selectedDestination;

  @override
  void initState() {
    super.initState();
    _mapsUrl = widget.mapsUrl;
  }

  Future<void> _loadInitialSceneFromUrl(String mapsUrl) async {
    try {
      // Use `http` to fetch the file via the download URL
      final response = await http.get(Uri.parse(mapsUrl));
      if (response.statusCode == 200) {
        final base64String = base64Encode(response.bodyBytes);
        _unityController.postMessage(
            'NavigationController', 'ImportSceneFromBase64ForNavigationLine', base64String);
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
    final theme = Theme.of(context);
    final themeManager = Provider.of<ThemeManager>(context);

    return Scaffold(
      appBar: AppBar(
        title: Center(
          child: Text("AR Navigation", style: theme.textTheme.displayMedium),
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
      body: UnityWidget(
        onUnityCreated: onUnityCreated,
        onUnityMessage: onUnityMessage,
      ),
      bottomNavigationBar: Container(
        color: Colors.blueGrey[50], // Background color for the footer
        padding: const EdgeInsets.all(8.0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text(
              "Select Destination: ",
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
            ),
            const SizedBox(width: 8),
            DropdownButton<String>(
              value: selectedDestination,
              onChanged: (String? newValue) {
                setState(() {
                  selectedDestination = newValue!;
                });

                // Send the selected destination to Unity
                _unityController.postMessage(
                  'NavigationController',
                  'SetDestination',
                  selectedDestination!,
                );
              },
              items: destinationList.map<DropdownMenuItem<String>>((String value) {
                return DropdownMenuItem<String>(
                  value: value,
                  child: Text(value),
                );
              }).toList(),
            )
          ],
        ),
      ),
    );
  }

  void initUnity(bool isAdmin) {
    _unityController.postMessage(
        "CanvasManager", "SetMode", isAdmin.toString());
  }

  void onUnityCreated(UnityWidgetController controller) {
    _unityController = controller;
    initUnity(false);
    if (_mapsUrl != null && _mapsUrl != null) {
      _loadInitialSceneFromUrl(widget.mapsUrl!);
    }
  }

  Future<void> onUnityMessage(dynamic message) async {
    print('Received message from Unity: $message');
    try {
      // Parse the JSON message from Unity
      final Map<String, dynamic> data = jsonDecode(message);
      if (data.containsKey('destinations')) {
        setState(() {
          destinationList = List<String>.from(data['destinations']);
          if (destinationList.isNotEmpty) {
            selectedDestination = destinationList.first;
          }
        });
      }
    } catch (e) {
      print('Error processing Unity message: $e');
    }
  }
}
