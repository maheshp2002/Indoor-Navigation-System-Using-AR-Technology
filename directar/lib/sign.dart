import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:flutter/material.dart';
import 'adminPage.dart';
import 'components/googleSignInButton.dart';
import 'home.dart';
import 'roleBasedNavigation.dart';
import 'services/firebaseService.dart';

class SignInPage extends StatelessWidget {
  final GoogleSignIn _googleSignIn = GoogleSignIn();
  final FirebaseAuth _auth = FirebaseAuth.instance;
  final FirebaseService _firebaseService = FirebaseService();

  SignInPage({super.key});

  Future<void> _signInWithGoogle(BuildContext context) async {
    try {
      User? user;

      if (kIsWeb) {
        // Web sign-in logic
        final GoogleAuthProvider googleProvider = GoogleAuthProvider();
        final UserCredential userCredential =
            await _auth.signInWithPopup(googleProvider);
        user = userCredential.user;
      } else {
        // Mobile sign-in logic
        final GoogleSignInAccount? googleUser = await _googleSignIn.signIn();
        final GoogleSignInAuthentication googleAuth =
            await googleUser!.authentication;

        final AuthCredential credential = GoogleAuthProvider.credential(
          accessToken: googleAuth.accessToken,
          idToken: googleAuth.idToken,
        );

        final UserCredential userCredential =
            await _auth.signInWithCredential(credential);
        user = userCredential.user;
      }

      if (user != null) {
        final String email = user.email!;
        final role = await _firebaseService.getUserRole(email);

        if (role != null) {
          if (role == 'Admin') {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const AdminPage(),
              ),
            );
          } else if (role == 'User') {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const Home(),
              ),
            );
          }
        } else {
          // Document does not exist; navigate to RoleBasedNavigation
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (context) => RoleBasedNavigation(user: user!)),
          );
        }
      }
    } catch (e) {
      print('Error signing in with Google: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error signing in: $e')),
      );
    }
  }


  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Padding(
                padding: EdgeInsets.only(left: 10, right: 10),
                child: Image(
                    image:
                        AssetImage('assets/logo/logo-shadow-transparent.png'))),
            const SizedBox(
              height: 20,
            ),
            GoogleSignInButton(
              onPressed: () => _signInWithGoogle(context),
            ),
          ],
        ),
      ),
    );
  }
}
