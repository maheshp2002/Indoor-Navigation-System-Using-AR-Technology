import 'package:flutter/material.dart';
import 'components/commonAppBar.dart';
import 'components/navbar.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  HomePageState createState() => HomePageState();
}

class HomePageState extends State<HomePage> {
  @override
  Widget build(BuildContext context) {
    // final themeManager = Provider.of<ThemeManager>(context);
    // final themeMode = themeManager.themeMode;
    final theme = Theme.of(context);
    // final customTheme = theme.extension<AppThemeExtension>();
    return Scaffold(
      appBar: const CommonAppBar(
        title: 'DirectAr',
      ),
      drawer: const NavBar(),
      body: Center(
        child: Text(
          "HomePage",
          style: theme.textTheme.bodyLarge,
        ),
      ),
    );
  }
}
