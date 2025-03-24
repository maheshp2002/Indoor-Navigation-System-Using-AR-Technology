import 'dart:developer';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:provider/provider.dart';
import 'package:qr_code_scanner/qr_code_scanner.dart';
import 'components/themeManager.dart';
import 'theme.dart';
import 'unity/userNavigation.dart';

class QRCodeScanning extends StatefulWidget {
  const QRCodeScanning({super.key});

  @override
  QRCodeScanningState createState() => QRCodeScanningState();
}

class QRCodeScanningState extends State<QRCodeScanning> {
  final GlobalKey qrKey = GlobalKey(debugLabel: 'QR');
  QRViewController? controller;
  String? result;
  bool isFlashOn = false;
  bool isFrontCamera = false;
  bool isScannerActive = true; 

  @override
  void reassemble() {
    super.reassemble();
    if (Platform.isAndroid) {
      controller!.pauseCamera();
    } else if (Platform.isIOS) {
      controller!.resumeCamera();
    }
  }

  Future<void> _updateFlashState() async {
    final status = await controller?.getFlashStatus();
    setState(() {
      isFlashOn = status ?? false;
    });
  }

  Future<void> _updateCameraInfo() async {
    final cameraInfo = await controller?.getCameraInfo();
    setState(() {
      isFrontCamera = cameraInfo == CameraFacing.front;
    });
  }

  void _onQRViewCreated(QRViewController controller) {
    this.controller = controller;

    // Update flash and camera state
    _updateFlashState();
    _updateCameraInfo();

    controller.scannedDataStream.listen((scanData) async {
      if (isScannerActive) {
        setState(() {
          isScannerActive = false;
          result = scanData.code.toString();
        });
        await _loadInitialSceneFromUrl(scanData.code.toString());
      }
    });
  }

  Future<void> _loadInitialSceneFromUrl(String mapsUrl) async {
      Navigator.pushReplacement(
          context,
          MaterialPageRoute(builder: (context) => UserNavigation(mapsUrl: mapsUrl)),
      );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final themeManager = Provider.of<ThemeManager>(context);

    return Scaffold(
      appBar: AppBar(
        title: Center(
          child: Text("ScanQR", style: theme.textTheme.displayMedium),
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
      ),
      body: Stack(
        children: [
          _buildQrView(context), // The QR code scanner view
          Positioned(
            bottom: 16.0,
            left: 16.0,
            right: 16.0,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                // Custom button for Flash toggle
                GestureDetector(
                  onTap: () async {
                    await controller?.toggleFlash();
                    _updateFlashState();
                  },
                  child: Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: AppColors.secondaryColor,
                      borderRadius: BorderRadius.circular(12),
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.backgroundDark.withOpacity(0.2),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Center(
                      child: Icon(
                        isFlashOn
                            ? FontAwesomeIcons.solidLightbulb
                            : FontAwesomeIcons.lightbulb,
                        size: 24,
                        color: Colors.black,
                      ),
                    ),
                  ),
                ),
                // Custom button for Camera flip
                GestureDetector(
                  onTap: () async {
                    await controller?.flipCamera();
                    _updateCameraInfo();
                  },
                  child: Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: AppColors.secondaryColor,
                      borderRadius: BorderRadius.circular(12),
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.backgroundDark.withOpacity(0.2),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Center(
                      child: Icon(
                        isFrontCamera
                            ? FontAwesomeIcons.cameraRotate
                            : FontAwesomeIcons.camera,
                        size: 24,
                        color: Colors.black,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQrView(BuildContext context) {
    var scanArea = (MediaQuery.of(context).size.width < 400 ||
            MediaQuery.of(context).size.height < 400)
        ? 150.0
        : 300.0;
    // To ensure the Scanner view is properly sizes after rotation
    // we need to listen for Flutter SizeChanged notification and update controller
    return QRView(
      key: qrKey,
      onQRViewCreated: _onQRViewCreated,
      overlay: QrScannerOverlayShape(
          borderColor: Colors.red,
          borderRadius: 10,
          borderLength: 30,
          borderWidth: 10,
          cutOutSize: scanArea),
      onPermissionSet: (ctrl, p) => _onPermissionSet(context, ctrl, p),
    );
  }

  void _onPermissionSet(BuildContext context, QRViewController ctrl, bool p) {
    log('${DateTime.now().toIso8601String()}_onPermissionSet $p');
    if (!p) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('no Permission')),
      );
    }
  }

  @override
  void dispose() {
    controller?.dispose();
    super.dispose();
  }
}
