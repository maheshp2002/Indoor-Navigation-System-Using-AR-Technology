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

  Future<void> updateMapDetails(
      String userEmail, String collectionName, String mapsDocumentId, String key, String value) async {
    await _firestore
        .collection('users')
        .doc(userEmail)
        .collection(collectionName)
        .doc(mapsDocumentId)
        .update({
          key: value
        });
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

  // Get user details
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
        final data = doc.data();
        if (data != null && data.containsKey('role')) {
          return data['role'] as String?;
        }
    }
    return null;
  }

    // Fetch recent items
  Future<List<Map<String, dynamic>>> fetchRecentItems(
      String userEmail, String collectionName) async {
    final snapshot = await _firestore
        .collection('users')
        .doc(userEmail)
        .collection(collectionName)
        .orderBy('last_opened_time', descending: true)
        .limit(5)
        .get();

    return snapshot.docs
        .map((doc) => {
              'id': doc.id,
              'date': doc['date'],
              'time': doc['time'],
              'url': doc['url'],
              'lastOpenedTime': doc['last_opened_time'],
            })
        .toList();
  }

  // Fetch all files
  Future<List<Map<String, dynamic>>> fetchAllFiles(
      String userEmail, String collectionName) async {
    final snapshot = await _firestore
        .collection('users')
        .doc(userEmail)
        .collection(collectionName)
        .get();

    return snapshot.docs
        .map((doc) => {
              'id': doc.id,
              'date': doc['date'],
              'url': doc['url'],
              'time': doc['time'],
            })
        .toList();
  }
}
