import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// One file of the school album: an image or a video
class AlbumImage {
  const AlbumImage({required this.id, required this.url, required this.fileName, this.contentType = ''});

  final String id;

  /// Full address of the file (through the gateway)
  final String url;
  final String fileName;

  /// e.g. "image/jpeg", "video/mp4"
  final String contentType;

  bool get isVideo =>
      contentType.toLowerCase().startsWith('video/') ||
      RegExp(r'\.(mp4|webm|mov)$', caseSensitive: false).hasMatch(url);

  factory AlbumImage.fromJson(Map<String, dynamic> json) {
    final mediaType = (json['mediaType'] as String?) ?? '';
    final contentType = (json['contentType'] as String?) ?? '';
    return AlbumImage(
      id: '${json['id'] ?? ''}',
      url: fullUrl((json['url'] as String?) ?? ''),
      fileName: (json['fileName'] as String?) ?? '',
      contentType: contentType.isEmpty && mediaType == 'video' ? 'video/mp4' : contentType,
    );
  }

  /// "/upload/school-album/x.jpg" -> "http://localhost:53607/upload/school-album/x.jpg"
  static String fullUrl(String url) {
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    final base = AppConfig.apiBaseUrl.replaceAll(RegExp(r'/+$'), '');
    return '$base/${url.replaceAll(RegExp(r'^/+'), '')}';
  }
}

/// School information (GET /api/school-info/get)
class SchoolInfo {
  const SchoolInfo({
    required this.nameAr,
    required this.nameEn,
    this.address,
    this.phone,
    this.email,
    this.logo,
    this.latitude,
    this.longitude,
    this.album = const [],
  });

  final String nameAr;
  final String nameEn;
  final String? address;
  final String? phone;
  final String? email;

  /// Logo bytes, or null when the school has none
  final Uint8List? logo;

  /// School location on the map (degrees); both set or both null
  final double? latitude;
  final double? longitude;

  bool get hasLocation => latitude != null && longitude != null;
  final List<AlbumImage> album;

  /// Name in the app language (the other one when it is missing)
  String get name => pickLang(nameAr, nameEn);

  /// The name in the other language, for a second line (empty when the same or missing)
  String get otherName {
    final other = AppLanguage.instance.isEnglish ? nameAr : nameEn;
    return other.trim().isNotEmpty && other != name ? other : '';
  }

  factory SchoolInfo.fromJson(Map<String, dynamic> json) => SchoolInfo(
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        address: _text(json['address']),
        phone: _text(json['phone']),
        email: _text(json['email']),
        logo: _bytes(json['logo']),
        latitude: _coordinate(json['latitude'], 90),
        longitude: _coordinate(json['longitude'], 180),
        album: (json['album'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(AlbumImage.fromJson)
            .where((a) => a.url.isNotEmpty)
            .toList(),
      );

  /// A coordinate within ±[limit], or null
  static double? _coordinate(Object? value, double limit) {
    final number = value is num ? value.toDouble() : double.tryParse('${value ?? ''}');
    return number != null && number.isFinite && number.abs() <= limit ? number : null;
  }

  static String? _text(Object? value) {
    final text = value is String ? value.trim() : '';
    return text.isEmpty ? null : text;
  }

  /// The API sends byte[] as base64 (a data URL is accepted too)
  static Uint8List? _bytes(Object? value) {
    if (value is! String || value.isEmpty) return null;
    try {
      final data = value.startsWith('data:') ? value.substring(value.indexOf(',') + 1) : value;
      final bytes = base64Decode(data);
      return bytes.isEmpty ? null : bytes;
    } catch (_) {
      return null;
    }
  }
}

/// Loading the school information failed; [message] is ready to show (Arabic)
class SchoolInfoException implements Exception {
  const SchoolInfoException(this.message);
  final String message;

  @override
  String toString() => message;
}

class SchoolInfoService {
  SchoolInfoService._();
  static final instance = SchoolInfoService._();

  /// Null when the school has not entered its information yet
  Future<SchoolInfo?> getSchoolInfo() async {
    final http.Response response;
    try {
      response = await http
          .get(Uri.parse('${AppConfig.apiBaseUrl}/api/school-info/get'), headers: {'Accept': 'application/json'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw SchoolInfoException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw SchoolInfoException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    if (response.statusCode != 200) {
      throw SchoolInfoException(tr('تعذر تحميل بيانات المدرسة.', 'Could not load the school information.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      return data is Map<String, dynamic> ? SchoolInfo.fromJson(data) : null;
    } catch (_) {
      throw SchoolInfoException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}
