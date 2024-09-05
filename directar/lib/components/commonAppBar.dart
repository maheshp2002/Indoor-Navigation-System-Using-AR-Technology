import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'themeManager.dart';

class CommonAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;

  const CommonAppBar({super.key, required this.title});

  @override
  Widget build(BuildContext context) {
    final themeManager = Provider.of<ThemeManager>(context);
    final theme = Theme.of(context);

    return AppBar(
      title: Center(
        child: Text(title, style: theme.textTheme.displayMedium),
      ),
      iconTheme: theme.iconTheme,
      actions: [
        IconButton(
          icon: Icon(
            color: theme.iconTheme.color,
            themeManager.themeMode == ThemeMode.dark
                ? Icons.dark_mode
                : Icons.light_mode,
          ),
          onPressed: () {
            themeManager.toggleTheme();
          },
        ),
      ],
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}
