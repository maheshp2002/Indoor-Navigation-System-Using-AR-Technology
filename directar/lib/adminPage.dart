import 'package:flutter/material.dart';
import '3DModel.dart';
import 'QR_Code.dart';
import 'sign.dart';

class AdminPage extends StatelessWidget {
  const AdminPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Admin Page')),
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            const DrawerHeader(
              decoration: BoxDecoration(
                color: Colors.orange,
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.admin_panel_settings,
                      size: 60, color: Colors.white),
                  SizedBox(height: 10),
                  Text(
                    'Admin Options',
                    style: TextStyle(color: Colors.white, fontSize: 20),
                  ),
                ],
              ),
            ),
            // ListTile(
            //   leading: const Icon(Icons.home),
            //   title: const Text('Home'),
            //   onTap: () {
            //     Navigator.push(
            //       context,
            //       MaterialPageRoute(
            //         builder: (context) => const Home(),
            //       ),
            //     ); // Navigate to the Home page
            //   },
            // ),
            ListTile(
              leading: const Icon(Icons.view_in_ar),
              title: const Text('3D Model'),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const ThreeDModel(),
                  ),
                ); // Navigate to the 3D Model page
              },
            ),
            ListTile(
              leading: const Icon(Icons.qr_code),
              title: const Text('QR Codes'),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const QrCode(),
                  ),
                ); // Navigate to the QR Codes page
              },
            ),
            ListTile(
              leading: const Icon(Icons.logout),
              title: const Text('Sign Out'),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => SignInPage(),
                  ),
                );
              },
            ),
          ],
        ),
      ),
      body: const Center(child: Text('Welcome to the Admin Page!')),
    );
  }
}
