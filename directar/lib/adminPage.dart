import 'package:directar/components/commonAppBar.dart';
import 'package:directar/components/navbar.dart';
import 'package:directar/theme.dart';
import 'package:directar/unity/mapEditor.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '3DModel.dart';
import 'QR_Code.dart';
import 'sign.dart';
import './DetailsPage.dart';

class AdminPage extends StatelessWidget {
  const AdminPage({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final recentItems = [
      {
        'image': 'assets/images/object.png',
        'description': 'hallway-14-12-2024-09-10-00'
      },
      {
        'image': 'assets/images/object.png',
        'description': 'roomway-15-12-2024-07-11-00'
      },
      {
        'image': 'assets/images/object.png',
        'description': 'kitchen-16-12-2024-10-08-00'
      },
    ];

    return Scaffold(
      appBar: const CommonAppBar(
        title: 'Admin Page',
      ),
      drawer: NavBar(),
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
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: recentItems.map((item) {
                  return GestureDetector(
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => DetailPage(
                            image: item['image']!,
                            description: item['description']!,
                          ),
                        ),
                      );
                    },
                    // child: Card(
                    //   margin: const EdgeInsets.symmetric(horizontal: 8.0),
                    //   child: Column(
                    //     crossAxisAlignment: CrossAxisAlignment.start,
                    //     children: [
                    //       Image.asset(
                    //         item['image']!,
                    //         fit: BoxFit.cover,
                    //         height: 100,
                    //         width: 200,
                    //       ),

                    //       Padding(
                    //         padding: const EdgeInsets.all(8),
                    //         child: Text(
                    //           item['description']!,
                    //           style: const TextStyle(
                    //             fontSize: 12,
                    //             fontWeight: FontWeight.bold,
                    //           ),
                    //         ),
                    //       ),
                    //     ],
                    //   ),
                    // ),
                    child: Card(
                      margin: const EdgeInsets.symmetric(horizontal: 8.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Image.asset(
                            item['image'] ??
                                '', // Provide a fallback if the key is null
                            fit: BoxFit.cover,
                            height: 100,
                            width: 200,
                          ),
                          Padding(
                            padding: const EdgeInsets.all(0),
                            child: Container(
                              color: AppColors
                                  .secondaryColor, // Set background color
                              padding: const EdgeInsets.all(
                                  4.0), // Optional padding for better spacing
                              child: Text(
                                item['description'] ??
                                    'No description available', // Fallback text
                                style: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),
            const SizedBox(height: 20),
            // All Files Section
            const Text(
              'All Files',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            Expanded(
              child: ListView.builder(
                itemCount: 10,
                itemBuilder: (context, index) {
                  return ListTile(
                    leading: Icon(FontAwesomeIcons.file,
                        color: theme.iconTheme.color),
                    title: Text('File ${index + 1}',
                        style: theme.textTheme.bodyLarge),
                    subtitle: Text('Details about file ${index + 1}.',
                        style: theme.textTheme.bodyLarge),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => DetailPage(
                            image: 'assets/images/object.png',
                            description: 'Details about File ${index + 1}',
                          ),
                        ),
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
          //  Navigator.push(
          //         context,
          //         MaterialPageRoute(builder: (context) => const MapEditor(mapsDocumentId: null, mapsUrl: null, isEditMode: false)),
          //       );
        },
        child: const Icon(Icons.add),
        backgroundColor: AppColors.secondaryColor,
      ),
    );
  }
}
