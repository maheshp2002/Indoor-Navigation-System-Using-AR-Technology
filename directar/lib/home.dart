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
  final List<Map<String, String>> steps = [
    {
      "imagePath": "assets/images/scan-qr.png",
      "description": "Step 1: Scan the QR code to proceed."
    },
    {
      "imagePath": "assets/images/load-unity.png",
      "description": "Step 2: Wait till the unity loads the scene."
    },
    {
      "imagePath": "assets/images/navigation.png",
      "description":
          "Step 3: Select the destination from drop down and follow the navigation path to reach destination."
    },
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: const CommonAppBar(title: 'DirectAr'),
      drawer: const NavBar(),
      body: Padding(
        padding: const EdgeInsets.all(16.0), // Added padding around the column
        child: Column(
          crossAxisAlignment:
              CrossAxisAlignment.start, // Ensures all text aligns left
          children: [
            const SizedBox(
              height: 10,
            ),
            Text(
              "Getting Started with DirectAr",
              style: TextStyle(
                  fontSize: theme.textTheme.headlineMedium!.fontSize,
                  color: AppColors.secondaryColor,
                  fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            ...steps
                .map((step) => InstructionStep(
                      imagePath: step["imagePath"]!,
                      description: step["description"]!,
                    ))
                .toList(),
          ],
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
      ),
    );
  }
}

class InstructionStep extends StatelessWidget {
  final String imagePath;
  final String description;

  const InstructionStep(
      {super.key, required this.imagePath, required this.description});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Image.asset(imagePath, width: 200),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerLeft,
          child: Text(
            description,
            style: theme.textTheme.labelSmall,
            textAlign: TextAlign.start,
          ),
        ),
        const SizedBox(height: 16),
      ],
    );
  }
}
