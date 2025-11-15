import 'package:flutter/foundation.dart' show kIsWeb;
import 'dart:html' as html;

class TokenStorage {
  static String? _token;
  static String? _refreshToken;

  static Future<void> saveToken(String token) async {
    _token = token;
    if (kIsWeb) {
      html.window.localStorage['accessToken'] = token;
    }
  }

  static Future<String?> getToken() async {
    if (_token != null) return _token;
    if (kIsWeb) {
      return html.window.localStorage['accessToken'];
    }
    return _token;
  }

  static Future<void> saveRefreshToken(String refreshToken) async {
    _refreshToken = refreshToken;
    if (kIsWeb) {
      html.window.localStorage['refreshToken'] = refreshToken;
    }
  }

  static Future<String?> getRefreshToken() async {
    if (_refreshToken != null) return _refreshToken;
    if (kIsWeb) {
      return html.window.localStorage['refreshToken'];
    }
    return _refreshToken;
  }

  static Future<void> deleteToken() async {
    _token = null;
    _refreshToken = null;
    if (kIsWeb) {
      html.window.localStorage.remove('accessToken');
      html.window.localStorage.remove('refreshToken');
    }
  }

  static Future<bool> isLoggedIn() async {
    final token = await getToken();
    return token != null && token.isNotEmpty;
  }
}
