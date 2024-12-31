import 'package:cloud_firestore/cloud_firestore.dart';

class FirebaseService {
  final FirebaseFirestore _firestore = FirebaseFirestore.instance;

  // Save or update user details
  Future<void> saveUserDetails(
      String userEmail, Map<String, dynamic> userData) async {
    await _firestore
        .collection('users')
        .doc(userEmail)
        .set(userData, SetOptions(merge: true));
  }

  // Save or update user details
  Future<DocumentReference> saveMapDetails(
      String userEmail, String collectionName, Map<String, dynamic> userData) async {
    return await _firestore
        .collection('users')
        .doc(userEmail)
        .collection(collectionName)
        .add(userData);
  }

  // Get or update user details
  Future<DocumentSnapshot<Map<String, dynamic>>> getMapDetails(
    String userEmail,
    String collectionName,
    String mapsDocumentId,
  ) async {
    return await _firestore
        .collection('users')
        .doc(userEmail)
        .collection(collectionName)
        .doc(mapsDocumentId)
        .get();
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
