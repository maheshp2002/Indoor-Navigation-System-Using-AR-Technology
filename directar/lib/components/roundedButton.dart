import 'package:flutter/material.dart';
import '../theme.dart';

class RoundedButton extends StatelessWidget {
  final String text;
  final VoidCallback onPressed;

  const RoundedButton({super.key, required this.text, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.accentColor,
      ),
      child: Text(
        text,
        style: const TextStyle(color: AppColors.white),
      ),
    );
  }
}
