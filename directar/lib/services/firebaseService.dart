import 'package:cloud_firestore/cloud_firestore.dart';

class FirebaseService {
  final FirebaseFirestore _firestore = FirebaseFirestore.instance;

  // Save or update user details
  Future<void> saveUserDetails(String userEmail, Map<String, dynamic> userData) async {
    await _firestore.collection('users').doc(userEmail).set(userData, SetOptions(merge: true));
  }

  // Fetch user role
  Future<String?> getUserRole(String userEmail) async {
    final doc = await _firestore.collection('users').doc(userEmail).get();
    if (doc.exists) {
      return doc['role'] as String?;
    }
    return null;
  }
}
