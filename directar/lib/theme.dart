import 'package:flutter/material.dart';

class AppColors {
  static const Color backgroundLight = Color(0xFFFFFFFF);
  static const Color backgroundDark = Color(0xFF121212);
  static const Color gradientStart = Color(0xFF000000);
  static const Color gradientMiddle = Color(0xFF1A1A1A);
  static const Color gradientEnd = Color(0xFF333333);
  static const Color primaryColor = Color(0xFFF25C54);
  static const Color primaryVariantColor1 = Color(0xFFF27059);
  static const Color primaryVariantColor2 = Color(0xFFF4845F);
  static const Color accentColor = Color(0xFFF79D65);
  static const Color secondaryColor = Color(0xFFF7B267);
  static const Color filterButtonBlack = Color(0xFF1e202e);
  static const Color filterButtonWhite = Color(0xFFfbfbfb);
  static const Color filterButtonGreen = Color(0xFF7efa8b);
  static const Color linkColor = Colors.blue;
  static const Color danger = Color(0xFFED3B3B);
  static const Color warning = Color(0xFFED9A3B);
  static const Color white = Color(0xFFFFFFFF);
}

class AppTextStyles {
  static const TextStyle headline = TextStyle(
    fontSize: 32.0,
    fontWeight: FontWeight.bold,
    color: Colors.white,
  );

  static const TextStyle appBarTitle = TextStyle(
    fontSize: 25.0,
    color: Colors.white,
  );

  static const TextStyle bodyText = TextStyle(
    fontSize: 16.0,
    color: Colors.white,
  );

  static const TextStyle labelTextWhite = TextStyle(
    fontSize: 16.0,
    color: Colors.white,
  );

  static const TextStyle labelTextBlack = TextStyle(
    fontSize: 16.0,
    color: Colors.white,
  );
}

class AppThemeExtension extends ThemeExtension<AppThemeExtension> {
  final Color toggleButtonBorderColor;
  final Color toggleButtonFillColor;
  final Color toggleButtonSelectedColor;
  final Color toggleButtonTextColor;
  final Color toggleButtonBackgroundColor;
  final Color modalBackgroundColor;
  final Color chipBackgroundColor;
  final Color dropdownBorderColor;
  final Color dropdownIconColor;

  AppThemeExtension(
      {required this.toggleButtonBorderColor,
      required this.toggleButtonFillColor,
      required this.toggleButtonSelectedColor,
      required this.toggleButtonTextColor,
      required this.toggleButtonBackgroundColor,
      required this.modalBackgroundColor,
      required this.chipBackgroundColor,
      required this.dropdownBorderColor,
      required this.dropdownIconColor});

  @override
  AppThemeExtension copyWith(
      {Color? toggleButtonBorderColor,
      Color? toggleButtonFillColor,
      Color? toggleButtonSelectedColor,
      Color? toggleButtonTextColor,
      Color? toggleButtonBackgroundColor,
      Color? modalBackgroundColor,
      Color? chipBackgroundColor,
      Color? dropdownBorderColor,
      Color? dropdownIconColor}) {
    return AppThemeExtension(
      toggleButtonBorderColor:
          toggleButtonBorderColor ?? this.toggleButtonBorderColor,
      toggleButtonFillColor:
          toggleButtonFillColor ?? this.toggleButtonFillColor,
      toggleButtonSelectedColor:
          toggleButtonSelectedColor ?? this.toggleButtonSelectedColor,
      toggleButtonTextColor:
          toggleButtonTextColor ?? this.toggleButtonTextColor,
      toggleButtonBackgroundColor:
          toggleButtonBackgroundColor ?? this.toggleButtonBackgroundColor,
      modalBackgroundColor: modalBackgroundColor ?? this.modalBackgroundColor,
      chipBackgroundColor: chipBackgroundColor ?? this.chipBackgroundColor,
      dropdownBorderColor: dropdownBorderColor ?? this.dropdownBorderColor,
      dropdownIconColor: dropdownIconColor ?? this.dropdownIconColor,
    );
  }

  @override
  AppThemeExtension lerp(ThemeExtension<AppThemeExtension>? other, double t) {
    if (other is! AppThemeExtension) {
      return this;
    }
    return AppThemeExtension(
      toggleButtonBorderColor: Color.lerp(
          toggleButtonBorderColor, other.toggleButtonBorderColor, t)!,
      toggleButtonFillColor:
          Color.lerp(toggleButtonFillColor, other.toggleButtonFillColor, t)!,
      toggleButtonSelectedColor: Color.lerp(
          toggleButtonSelectedColor, other.toggleButtonSelectedColor, t)!,
      toggleButtonTextColor:
          Color.lerp(toggleButtonTextColor, other.toggleButtonTextColor, t)!,
      toggleButtonBackgroundColor: Color.lerp(
          toggleButtonBackgroundColor, other.toggleButtonBackgroundColor, t)!,
      modalBackgroundColor:
          Color.lerp(modalBackgroundColor, other.modalBackgroundColor, t)!,
      chipBackgroundColor:
          Color.lerp(chipBackgroundColor, other.chipBackgroundColor, t)!,
      dropdownBorderColor:
          Color.lerp(dropdownBorderColor, other.dropdownBorderColor, t)!,
      dropdownIconColor:
          Color.lerp(dropdownIconColor, other.dropdownIconColor, t)!,
    );
  }
}

class AppTheme {
  static final ThemeData lightTheme = ThemeData(
    extensions: <ThemeExtension<AppThemeExtension>>[
      AppThemeExtension(
          toggleButtonBorderColor: AppColors.primaryColor,
          toggleButtonFillColor: AppColors.secondaryColor,
          toggleButtonSelectedColor: Colors.white,
          toggleButtonTextColor: AppColors.primaryColor,
          toggleButtonBackgroundColor: Colors.transparent,
          modalBackgroundColor: Colors.white70,
          chipBackgroundColor: Colors.white60,
          dropdownBorderColor: AppColors.secondaryColor,
          dropdownIconColor: AppColors.secondaryColor),
    ],
    dropdownMenuTheme: const DropdownMenuThemeData(),
    dialogBackgroundColor: AppColors.backgroundLight,
    iconTheme: const IconThemeData(color: Colors.black87),
    primaryColor: AppColors.backgroundLight,
    scaffoldBackgroundColor: AppColors.backgroundLight,
    buttonTheme: const ButtonThemeData(buttonColor: AppColors.primaryColor),
    textTheme: TextTheme(
      displayLarge: AppTextStyles.headline.copyWith(color: Colors.black87),
      bodyLarge: AppTextStyles.bodyText.copyWith(color: Colors.black87),
      headlineLarge:
          AppTextStyles.bodyText.copyWith(color: Colors.black87, fontSize: 25),
      displayMedium: AppTextStyles.appBarTitle
          .copyWith(color: Colors.black87, fontWeight: FontWeight.bold),
      labelSmall: AppTextStyles.appBarTitle
          .copyWith(color: Colors.black87, fontSize: 15),
    ),
    drawerTheme: const DrawerThemeData(
      backgroundColor: AppColors.backgroundLight,
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: Colors.transparent,
    ),
  );

  static final ThemeData darkTheme = ThemeData(
    extensions: <ThemeExtension<AppThemeExtension>>[
      AppThemeExtension(
          toggleButtonBorderColor: Colors.black54,
          toggleButtonFillColor: Colors.grey[800]!,
          toggleButtonSelectedColor: Colors.white,
          toggleButtonTextColor: Colors.white,
          toggleButtonBackgroundColor: Colors.black54,
          modalBackgroundColor: Colors.black12,
          chipBackgroundColor: Colors.black87,
          dropdownBorderColor: Colors.white70,
          dropdownIconColor: Colors.white70),
    ],
    iconTheme: const IconThemeData(color: Colors.white),
    dialogBackgroundColor: AppColors.backgroundDark,
    buttonTheme: const ButtonThemeData(buttonColor: AppColors.primaryColor),
    primaryColor: AppColors.backgroundDark,
    scaffoldBackgroundColor: AppColors.backgroundDark,
    textTheme: TextTheme(
      displayLarge: AppTextStyles.headline.copyWith(color: Colors.white),
      bodyLarge: AppTextStyles.bodyText.copyWith(color: Colors.white),
      headlineLarge:
          AppTextStyles.bodyText.copyWith(color: Colors.white, fontSize: 25),
      displayMedium: AppTextStyles.appBarTitle
          .copyWith(color: Colors.white, fontWeight: FontWeight.bold),
      labelSmall:
          AppTextStyles.appBarTitle.copyWith(color: Colors.white, fontSize: 15),
    ),
    drawerTheme: const DrawerThemeData(
      backgroundColor: AppColors.backgroundDark,
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: Colors.transparent,
      elevation: 0,
    ),
  );

  static BoxDecoration getBackgroundDecoration(ThemeMode themeMode) {
    return BoxDecoration(
      gradient: themeMode == ThemeMode.dark
          ? const LinearGradient(
              colors: [
                AppColors.gradientStart,
                AppColors.gradientMiddle,
                AppColors.gradientEnd,
              ],
              stops: [0.0, 0.5, 1.0],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            )
          : const LinearGradient(
              colors: [Colors.white, Colors.white],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
    );
  }
}
