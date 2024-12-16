import 'package:flutter/material.dart';
import '../theme.dart';

class GoogleSignInButton extends StatefulWidget {
  final VoidCallback onPressed;

  const GoogleSignInButton({super.key, required this.onPressed});

  @override
  _GoogleSignInButtonState createState() => _GoogleSignInButtonState();
}

class _GoogleSignInButtonState extends State<GoogleSignInButton> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: GestureDetector(
        onTap: widget.onPressed,
        child: Container(
          decoration: BoxDecoration(
            color: _isHovered ? AppColors.primaryColor : theme.primaryColor,
            border: Border.all(color: Colors.grey.shade300), // Border styling
            borderRadius: BorderRadius.circular(25.0), // Rounded corners
            boxShadow: const [
              BoxShadow(
                color: Colors.grey,
                spreadRadius: 0.5,
                blurRadius: 1,
              )
            ],
          ),
          padding: const EdgeInsets.symmetric(horizontal: 1.0, vertical: 1.0),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              // White circular background around the Google icon
              Container(
                padding: const EdgeInsets.all(4.0), // Padding for the circle
                decoration: const BoxDecoration(
                  color: Colors.white, // White background
                  shape: BoxShape.circle, // Circular shape
                ),
                child: Image.asset(
                  'assets/logo/google_logo.png', // Google logo image
                  height: 40.0,
                  width: 40.0,
                ),
              ),
              const SizedBox(width: 10.0),
              Text(
                'Sign in with Google',
                style: TextStyle(
                  color: _isHovered
                      ? Colors.white
                      : theme.textTheme.bodyLarge?.color, // Text color
                  fontSize: 20.0,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(width: 20.0)
            ],
          ),
        ),
      ),
    );
  }
}
