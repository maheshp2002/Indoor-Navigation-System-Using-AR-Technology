import 'package:directar/3DModel.dart';
import 'package:directar/QR_Code.dart';
import 'package:directar/adminPage.dart';
import 'package:directar/config/constants.dart';
import 'package:directar/services/firebaseService.dart';
import 'package:flutter/material.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:google_sign_in/google_sign_in.dart';
import '../home.dart';
import '../sign.dart';

class NavBar extends StatefulWidget {
  const NavBar({super.key});

  @override
  NavBarState createState() => NavBarState();
}

class NavBarState extends State<NavBar> {
  String? email = FirebaseAuth.instance.currentUser?.email;
  String? userRole;
  final FirebaseService _firebaseService = FirebaseService();

  @override
  void initState() {
    super.initState();
    checkRole();
  }

  Future<void> _signOut(BuildContext context) async {
    await FirebaseAuth.instance.signOut();
    await GoogleSignIn().signOut();
    if (context.mounted) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (context) => SignInPage()),
      );
    }
  }

  void checkRole() async {
    var role = await _firebaseService.getUserRole(email!);
    setState(() {
      userRole = role;
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Drawer(
      backgroundColor: theme.drawerTheme.backgroundColor,
      child: ListView(
        padding: EdgeInsets.zero,
        children: <Widget>[
          DrawerHeader(
            decoration: BoxDecoration(
              color: theme.primaryColor, // Use theme's primary color
            ),
            child: Text(
              'Menu',
              style: theme.textTheme.displayLarge,
            ),
          ),
          if (userRole != null && userRole != AppConstants.adminRole) ...[
            ListTile(
              leading: Icon(FontAwesomeIcons.houseChimney,
                  color: theme.iconTheme.color),
              title: Text('Home', style: theme.textTheme.bodyLarge),
              onTap: () => Navigator.pushReplacement(
                context,
                MaterialPageRoute(builder: (context) => const Home()),
              ),
            ),
          ],
          if (userRole != null && userRole == AppConstants.adminRole) ...[
            ListTile(
              leading: Icon(FontAwesomeIcons.map, color: theme.iconTheme.color),
              title: Text('Map Editor', style: theme.textTheme.bodyLarge),
              onTap: () => Navigator.pushReplacement(
                context,
                MaterialPageRoute(builder: (context) => const AdminPage()),
              ),
            ),
            ListTile(
              leading:
                  Icon(FontAwesomeIcons.cubes, color: theme.iconTheme.color),
              title: Text('3D Model', style: theme.textTheme.bodyLarge),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const ThreeDModel()),
                );
              },
            ),
            ListTile(
              leading:
                  Icon(FontAwesomeIcons.qrcode, color: theme.iconTheme.color),
              title: Text('Scan QR Code', style: theme.textTheme.bodyLarge),
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const QrCode()),
                );
              },
            ),
          ],
          ListTile(
            leading: Icon(FontAwesomeIcons.rightFromBracket,
                color: theme.iconTheme.color),
            title: Text('Sign Out', style: theme.textTheme.bodyLarge),
            onTap: () => _signOut(context),
          ),
        ],
      ),
    );
  }
}
