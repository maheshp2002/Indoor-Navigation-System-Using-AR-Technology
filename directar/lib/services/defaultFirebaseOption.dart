import 'package:firebase_core/firebase_core.dart';

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    return android;
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: "AIzaSyDJ2OtekzAKeFm1SoDtwPqTwAf1620EHsM",
    authDomain: "directar-f764a.firebaseapp.com",
    projectId: "directar-f764a",
    storageBucket: "directar-f764a.appspot.com",
    messagingSenderId: "219644855653",
    appId: "1:219644855653:web:b3c83c7873dadf37804d8c",
    measurementId: "G-8KE7LQR4YP"
  );
}
