import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:directar/components/commonAppBar.dart';
import 'package:directar/components/navbar.dart';
import 'package:directar/theme.dart';
import 'package:directar/unity/mapEditor.dart';
import 'package:directar/services/firebaseService.dart';
import 'config/constants.dart';
import 'package:timeago/timeago.dart' as timeago;

class AdminPage extends StatefulWidget {
  const AdminPage({super.key});

  @override
  AdminPageState createState() => AdminPageState();
}

class AdminPageState extends State<AdminPage> {
  late Future<List<Map<String, dynamic>>> _recentItemsFuture;
  late Future<List<Map<String, dynamic>>> _allFilesFuture;

  @override
  void initState() {
    super.initState();
    // Fetch initial data for the recent items and all files.
    _fetchData();
  }

  void _fetchData() {
    final userEmail = FirebaseAuth.instance.currentUser?.email;
    final firebaseService = FirebaseService();

    _recentItemsFuture = firebaseService.fetchRecentItems(
        userEmail!, FirebaseConstants.mapsCollection);
    _allFilesFuture = firebaseService.fetchAllFiles(
        userEmail, FirebaseConstants.mapsCollection);
  }

  void openMapEditor(String id, String url) async {
    final userEmail = FirebaseAuth.instance.currentUser?.email;
    final firebaseService = FirebaseService();

    await firebaseService.updateMapDetails(
        userEmail!,
        FirebaseConstants.mapsCollection,
        id,
        'last_opened_time',
        DateTime.now().toIso8601String());

    // After updating the map, refresh the data when coming back.
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => MapEditor(
          mapsDocumentId: id,
          mapsUrl: url,
          isEditMode: true,
        ),
      ),
    ).then((_) {
      // Trigger a refresh after returning from the MapEditor.
      setState(() {
        _fetchData(); // Reload data after coming back from the MapEditor.
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: const CommonAppBar(
        title: 'Admin Page',
      ),
      drawer: const NavBar(),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Recent Items',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            FutureBuilder<List<Map<String, dynamic>>>(
              future: _recentItemsFuture,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(child: CircularProgressIndicator());
                }

                if (snapshot.hasError) {
                  return const Text('Error fetching recent items');
                }

                final recentItems = snapshot.data ?? [];
                return SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: recentItems.map((item) {
                      return _HoverableCard(
                        item: item,
                        onTap: () {
                          openMapEditor(item['id'], item['url']);
                        },
                      );
                    }).toList(),
                  ),
                );
              },
            ),
            const SizedBox(height: 20),
            const Text(
              'All Files',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            Expanded(
              child: FutureBuilder<List<Map<String, dynamic>>>(
                future: _allFilesFuture,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: CircularProgressIndicator());
                  }

                  if (snapshot.hasError) {
                    return const Text('Error fetching all files');
                  }

                  final allFiles = snapshot.data ?? [];
                  return ListView.builder(
                    itemCount: allFiles.length,
                    itemBuilder: (context, index) {
                      final file = allFiles[index];
                      return ListTile(
                        leading: const Icon(
                          FontAwesomeIcons.file,
                          color: AppColors.primaryColor,
                        ),
                        subtitle: Text(
                          "Created Date: ${file['date']}",
                          style: theme.textTheme.bodyLarge,
                        ),
                        title: Text(
                          "Created Time: ${file['time']}",
                          style: theme.textTheme.bodyMedium,
                        ),
                        onTap: () {
                          openMapEditor(file['id'], file['url']);
                        },
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => const MapEditor(
                mapsDocumentId: null,
                mapsUrl: null,
                isEditMode: false,
              ),
            ),
          ).then((_) {
            // Refresh the data after returning from adding a new map
            setState(() {
              _fetchData(); // Reload data after coming back.
            });
          });
        },
        backgroundColor: AppColors.secondaryColor,
        child: const Icon(Icons.add),
      ),
    );
  }
}

class _HoverableCard extends StatefulWidget {
  final Map<String, dynamic> item;
  final VoidCallback onTap;

  const _HoverableCard({
    required this.item,
    required this.onTap,
    super.key,
  });

  @override
  State<_HoverableCard> createState() => _HoverableCardState();
}

class _HoverableCardState extends State<_HoverableCard> {
  bool isHovered = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final appThemeExtension = Theme.of(context).extension<AppThemeExtension>();

    return GestureDetector(
      onTap: widget.onTap,
      child: MouseRegion(
        onEnter: (_) {
          setState(() => isHovered = true);
        },
        onExit: (_) {
          setState(() => isHovered = false);
        },
        cursor: SystemMouseCursors
            .click, // Add this line to change the cursor to a hand
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          margin: const EdgeInsets.symmetric(horizontal: 8.0),
          width: 150,
          height: 120,
          decoration: BoxDecoration(
            color:
                appThemeExtension?.boxDecorationColor ?? AppColors.transparent,
            border: Border.all(
              color: isHovered
                  ? appThemeExtension?.borderColor ?? AppColors.secondaryColor
                  : Colors.transparent,
              width: 2.0,
            ),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(
                FontAwesomeIcons.unity,
                size: 50,
                color: AppColors.primaryColor,
              ),
              const SizedBox(height: 8.0),
              Text(
                widget.item['date'] ?? 'No date',
                style: theme.textTheme.bodyLarge,
              ),
              Text(
                widget.item['lastOpenedTime'] != null
                    ? timeago
                        .format(DateTime.parse(widget.item['lastOpenedTime']))
                    : 'Not opened yet',
                style: theme.textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
