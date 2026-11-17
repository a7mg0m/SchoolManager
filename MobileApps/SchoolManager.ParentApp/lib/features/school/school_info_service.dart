import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/config.dart';

/// One image of the school album
class AlbumImage {
  const AlbumImage({required this.id, required this.url, required this.fileName});

  final String id;

  /// Full address of the image (through the gateway)
  final String url;
  final String fileName;

  factory AlbumImage.fromJson(Map<String, dynamic> json) => AlbumImage(
        id: '${json['id'] ?? ''}',
        url: fullUrl((json['url'] as String?) ?? ''),
        fileName: (json['fileName'] as String?) ?? '',
      );

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
    this.album = const [],
  });

  final String nameAr;
  final String nameEn;
  final String? address;
  final String? phone;
  final String? email;

  /// Logo bytes, or null when the school has none
  final Uint8List? logo;
  final List<AlbumImage> album;

  /// Arabic name (the app is Arabic), English when there is none
  String get name => nameAr.trim().isNotEmpty ? nameAr : nameEn;

  factory SchoolInfo.fromJson(Map<String, dynamic> json) => SchoolInfo(
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        address: _text(json['address']),
        phone: _text(json['phone']),
        email: _text(json['email']),
        logo: _bytes(json['logo']),
        album: (json['album'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(AlbumImage.fromJson)
            .where((a) => a.url.isNotEmpty)
            .toList(),
      );

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
      throw const SchoolInfoException('انتهت مهلة الاتصال بالخادم.');
    } catch (_) {
      throw const SchoolInfoException('تعذر الاتصال بالخادم.');
    }

    if (response.statusCode != 200) {
      throw const SchoolInfoException('تعذر تحميل بيانات المدرسة.');
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      return data is Map<String, dynamic> ? SchoolInfo.fromJson(data) : null;
    } catch (_) {
      throw const SchoolInfoException('استجابة غير متوقعة من الخادم.');
    }
  }
}
