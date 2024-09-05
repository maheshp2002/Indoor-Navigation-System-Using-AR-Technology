import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'homepage.dart';
import 'sign.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  SplashScreenState createState() => SplashScreenState();
}

class SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkSignInStatus();
  }

  _checkSignInStatus() async {
    await Future.delayed(
        const Duration(milliseconds: 3000)); // Delay for 3 seconds

    User? user = FirebaseAuth.instance.currentUser;
    if (mounted) {
      // Check if the widget is still in the widget tree
      if (user != null) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(builder: (context) => HomePage()),
        );
      } else {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(builder: (context) => SignInPage()),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Image.asset(
          'assets/logo/logo-icon.png',
          width: 200,
        ), // Ensure you have a logo.png in the assets folder
      ),
    );
  }
}
