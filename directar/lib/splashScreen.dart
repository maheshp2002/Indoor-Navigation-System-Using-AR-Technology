import 'adminPage.dart';
import 'home.dart';
import 'roleBasedNavigation.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'services/firebaseService.dart';
import 'sign.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  SplashScreenState createState() => SplashScreenState();
}

class SplashScreenState extends State<SplashScreen> {
  final FirebaseAuth _auth = FirebaseAuth.instance;
  final FirebaseService _firebaseService = FirebaseService();

  @override
  void initState() {
    super.initState();
    _handleNavigation();
  }

  Future<void> _handleNavigation() async {
    await Future.delayed(const Duration(milliseconds: 2000));

    final User? user = _auth.currentUser;

    if (user == null) {
      Navigator.pushReplacement(
          context, MaterialPageRoute(builder: (_) => SignInPage()));
    } else {
      final role = await _firebaseService.getUserRole(user.email!);

      if (role == 'Admin') {
        Navigator.pushReplacement(
            context, MaterialPageRoute(builder: (_) => const AdminPage()));
      } else if (role == 'User') {
        Navigator.pushReplacement(
            context, MaterialPageRoute(builder: (_) => const Home()));
      } else {
        Navigator.pushReplacement(context,
            MaterialPageRoute(builder: (_) => RoleBasedNavigation(user: user)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Image.asset(
          "assets/gifs/logo-splash-screen.gif",
          width: 900,
        ), // Ensure you have a logo.png in the assets folder
      ),
    );
  }
}
