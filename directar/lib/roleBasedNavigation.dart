import 'package:directar/components/roundedButton.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'adminPage.dart';
import 'home.dart';
import 'services/firebaseService.dart';

class RoleBasedNavigation extends StatefulWidget {
  final User user;
  const RoleBasedNavigation({super.key, required this.user});

  @override
  _RoleBasedNavigationState createState() => _RoleBasedNavigationState();
}

class _RoleBasedNavigationState extends State<RoleBasedNavigation> {
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  final TextEditingController _organizationController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  String? selectedRole;
  final FirebaseService _firebaseService = FirebaseService();

  Future<void> submitRole() async {
    if (_formKey.currentState!.validate()) {
      final Map<String, dynamic> userData = {
        'email': widget.user.email,
        'name': _nameController.text,
        'role': selectedRole,
        'organization':
            selectedRole == 'Admin' ? _organizationController.text : null,
      };

      await _firebaseService.saveUserDetails(widget.user.email!, userData);

      if (selectedRole == 'Admin') {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => const AdminPage(),
          ),
        );
      } else if (selectedRole == 'User') {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => const Home(),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Login',
                style: TextStyle(
                  fontSize: 35,
                  color: Colors.orange,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 20),
              Form(
                key: _formKey,
                child: Column(
                  children: [
                    DropdownButtonFormField<String>(
                      value: selectedRole,
                      decoration: const InputDecoration(
                        hintText: "Select Role",
                        prefixIcon: Icon(FontAwesomeIcons.user),
                        border: OutlineInputBorder(),
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: 'Admin',
                          child: Text('Admin'),
                        ),
                        DropdownMenuItem(
                          value: 'User',
                          child: Text('User'),
                        ),
                      ],
                      onChanged: (value) {
                        setState(() {
                          selectedRole = value;
                        });
                      },
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return 'Please select a role';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 20),
                    TextFormField(
                      controller: _nameController,
                      decoration: const InputDecoration(
                        hintText: "Name",
                        prefixIcon: Icon(FontAwesomeIcons.signature),
                        border: OutlineInputBorder(),
                      ),
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return 'Please enter your name';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 20),
                    if (selectedRole == 'Admin')
                      TextFormField(
                        controller: _organizationController,
                        decoration: const InputDecoration(
                          hintText: "Organization Name",
                          prefixIcon: Icon(FontAwesomeIcons.building),
                          border: OutlineInputBorder(),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Please enter your organization name';
                          }
                          return null;
                        },
                      ),
                    const SizedBox(height: 20),
                    RoundedButton(
                      text: "Submit",
                      onPressed: () {
                        if (_formKey.currentState!.validate()) {
                          submitRole();
                        }
                      },
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
