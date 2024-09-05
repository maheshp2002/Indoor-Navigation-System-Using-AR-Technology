import 'package:firebase_core/firebase_core.dart';

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    return android;
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyBkgyxDvhEPljxmVRKuFnxK_HIKhtdHuFU',
    appId: '1:74030690702:android:90466f5dc8ed9d7877b4df',
    messagingSenderId: 'YOUR_ANDROID_MESSAGING_SENDER_ID',
    projectId: 'mac-tracker-ae2e1',
    storageBucket: 'mac-tracker-ae2e1.appspot.com',
  );
}
