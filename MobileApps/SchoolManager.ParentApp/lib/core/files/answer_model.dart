import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../config.dart';
import '../i18n/app_language.dart';

/// Full address of an exam's answer model: an uploaded file (/exam-files/…) is served through the API, a link is kept
Uri answerModelUri(String url) {
  final value = url.trim();
  if (RegExp(r'^https?://', caseSensitive: false).hasMatch(value)) return Uri.parse(value);
  return Uri.parse('${AppConfig.apiBaseUrl}${value.startsWith('/') ? '' : '/'}$value');
}

/// Opens the answer model (PDF / picture / link) in the phone's viewer or browser
Future<void> openAnswerModel(BuildContext context, String url) async {
  var opened = false;
  try {
    opened = await launchUrl(answerModelUri(url), mode: LaunchMode.externalApplication);
  } catch (_) {}
  if (!opened && context.mounted) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(tr('تعذر فتح نموذج الإجابة.', 'The answer model could not be opened.'))),
    );
  }
}
