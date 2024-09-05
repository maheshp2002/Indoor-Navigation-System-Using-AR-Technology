import 'package:firebase_core/firebase_core.dart';

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    return android;
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyBOx7pz_vIgiCzTv1znk0UhHTIa8JTS8cE',
    appId: '1:219644855653:android:7bad1f8e5f468437804d8c',
    messagingSenderId: 'YOUR_ANDROID_MESSAGING_SENDER_ID',
    projectId: 'directar-f764a',
    storageBucket: 'directar-f764a.appspot.com',
  );
}
