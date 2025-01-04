import 'package:directar/theme.dart';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'QrCodeScanning.dart';
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
    final theme = Theme.of(context);
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
        floatingActionButtonLocation: FloatingActionButtonLocation.centerDocked,
        floatingActionButton: FloatingActionButton(
          backgroundColor: AppColors.secondaryColor,
          child: const Icon(FontAwesomeIcons.qrcode),
          onPressed: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (context) => const QRCodeScanning()),
            );
          },
        ));
  }
}
