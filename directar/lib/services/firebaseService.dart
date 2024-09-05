import 'package:cloud_firestore/cloud_firestore.dart';

import '../config/constants.dart';

class FirebaseService {
  final FirebaseFirestore _firestore = FirebaseFirestore.instance;

  Stream<Map<String, dynamic>> streamBankData() {
    return _firestore
        .collection(FirebaseConstants.mastersCollection)
        .doc(FirebaseConstants.banksCollection)
        .collection(FirebaseConstants.banksCollection)
        .snapshots()
        .map((snapshot) {
      // Convert snapshot to a map of documents with their ID as keys
      snapshot.docs.map((doc) => doc.data()).toList();
      return {for (var doc in snapshot.docs) doc.id: doc.data()};
    });
  }

  Future<void> addData(String userEmail, String documentId,
      Map<String, dynamic> expenseData, String collectionName) async {
    // Reference to the user's expense collection
    CollectionReference expenseCollection = _firestore
        .collection(FirebaseConstants.usersCollection)
        .doc(userEmail)
        .collection(collectionName);

    // Now add the actual expense document
    await expenseCollection.doc(documentId).set(expenseData);
  }

  Stream<Map<String, dynamic>> streamGetAllData(
      String userEmail, String collectionName) {
    return _firestore
        .collection(FirebaseConstants.usersCollection)
        .doc(userEmail)
        .collection(collectionName)
        .snapshots()
        .map((snapshot) {
      // Convert snapshot to a map of documents with their ID as keys
      snapshot.docs.map((doc) => doc.data()).toList();
      return {for (var doc in snapshot.docs) doc.id: doc.data()};
    });
  }

  Stream<List<Map<String, dynamic>>> streamUserBankData(
      String userEmail, String collectionName) {
    return _firestore
        .collection(FirebaseConstants.usersCollection)
        .doc(userEmail)
        .collection(collectionName)
        .snapshots()
        .map((snapshot) {
      return snapshot.docs.map((doc) => doc.data()).toList();
    });
  }

  // Update the salary amount in the salary document
  Future<void> updateSalaryAmount(
      String? userEmail, String documentId, double newAmount) async {
    await FirebaseFirestore.instance
        .collection(FirebaseConstants.usersCollection)
        .doc(userEmail)
        .collection(FirebaseConstants.salaryCollection)
        .doc(documentId)
        .update({'currentAmount': newAmount});
  }
}
