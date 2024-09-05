import 'package:bot_toast/bot_toast.dart';
import 'package:flutter/material.dart';

import '../theme.dart';

void showToast(String content, {bool isSuccess = true}) {
  BotToast.showText(
    text: content,
    duration: const Duration(seconds: 3),
    contentColor: isSuccess ? AppColors.secondaryColor : AppColors.danger,
    textStyle: const TextStyle(color: AppColors.white),
    borderRadius: BorderRadius.circular(10),
  );
}
