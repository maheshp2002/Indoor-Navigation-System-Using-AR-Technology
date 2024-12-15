import 'package:flutter/material.dart';
import 'components/commonAppBar.dart';
import 'components/navbar.dart';

class Home extends StatefulWidget {
  const Home({super.key});

  @override
  HomeState createState() => HomeState();
}

class HomeState extends State<Home> {
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
          "Home",
          style: theme.textTheme.bodyLarge,
        ),
      ),
    );
  }
}
