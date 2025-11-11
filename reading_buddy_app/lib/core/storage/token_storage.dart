import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../constants/storage_constants.dart';

/// 토큰 및 사용자 정보 저장소
class TokenStorage {
  final FlutterSecureStorage _secureStorage;
  final SharedPreferences _prefs;

  TokenStorage(this._secureStorage, this._prefs);

  // ==================== Secure Storage (민감 정보) ====================

  /// Access Token 저장
  Future<void> saveAccessToken(String token) async {
    await _secureStorage.write(
      key: StorageConstants.accessToken,
      value: token,
    );
  }

  /// Access Token 조회
  Future<String?> getAccessToken() async {
    return await _secureStorage.read(key: StorageConstants.accessToken);
  }

  /// Refresh Token 저장
  Future<void> saveRefreshToken(String token) async {
    await _secureStorage.write(
      key: StorageConstants.refreshToken,
      value: token,
    );
  }

  /// Refresh Token 조회
  Future<String?> getRefreshToken() async {
    return await _secureStorage.read(key: StorageConstants.refreshToken);
  }

  // ==================== Shared Preferences (일반 정보) ====================

  /// 사용자 ID 저장
  Future<void> saveUserId(int userId) async {
    await _prefs.setInt(StorageConstants.userId, userId);
  }

  /// 사용자 ID 조회
  Future<int?> getUserId() async {
    return _prefs.getInt(StorageConstants.userId);
  }

  /// 사용자 이메일 저장
  Future<void> saveUserEmail(String email) async {
    await _prefs.setString(StorageConstants.userEmail, email);
  }

  /// 사용자 이메일 조회
  Future<String?> getUserEmail() async {
    return _prefs.getString(StorageConstants.userEmail);
  }

  /// 사용자 닉네임 저장
  Future<void> saveUserNickname(String nickname) async {
    await _prefs.setString(StorageConstants.userNickname, nickname);
  }

  /// 사용자 닉네임 조회
  Future<String?> getUserNickname() async {
    return _prefs.getString(StorageConstants.userNickname);
  }

  /// 로그인 상태 저장
  Future<void> setLoggedIn(bool isLoggedIn) async {
    await _prefs.setBool(StorageConstants.isLoggedIn, isLoggedIn);
  }

  /// 로그인 상태 조회
  bool isLoggedIn() {
    return _prefs.getBool(StorageConstants.isLoggedIn) ?? false;
  }

  /// 선택된 테마 저장
  Future<void> saveSelectedTheme(String theme) async {
    await _prefs.setString(StorageConstants.selectedTheme, theme);
  }

  /// 선택된 테마 조회
  String? getSelectedTheme() {
    return _prefs.getString(StorageConstants.selectedTheme);
  }

  // ==================== 로그인 설정 ====================

  /// 저장된 이메일 저장 (아이디 저장용)
  Future<void> saveSavedEmail(String email) async {
    await _prefs.setString(StorageConstants.savedEmail, email);
  }

  /// 저장된 이메일 조회
  String? getSavedEmail() {
    return _prefs.getString(StorageConstants.savedEmail);
  }

  /// 저장된 이메일 삭제
  Future<void> clearSavedEmail() async {
    await _prefs.remove(StorageConstants.savedEmail);
  }

  /// 저장된 비밀번호 저장 (자동 로그인용, Secure Storage)
  Future<void> saveSavedPassword(String password) async {
    await _secureStorage.write(
      key: StorageConstants.savedPassword,
      value: password,
    );
  }

  /// 저장된 비밀번호 조회
  Future<String?> getSavedPassword() async {
    return await _secureStorage.read(key: StorageConstants.savedPassword);
  }

  /// 저장된 비밀번호 삭제
  Future<void> clearSavedPassword() async {
    await _secureStorage.delete(key: StorageConstants.savedPassword);
  }

  /// 아이디 저장 설정 저장
  Future<void> setRememberEmail(bool remember) async {
    await _prefs.setBool(StorageConstants.rememberEmail, remember);
  }

  /// 아이디 저장 설정 조회
  bool isRememberEmail() {
    return _prefs.getBool(StorageConstants.rememberEmail) ?? false;
  }

  /// 자동 로그인 설정 저장
  Future<void> setAutoLogin(bool autoLogin) async {
    await _prefs.setBool(StorageConstants.autoLogin, autoLogin);
  }

  /// 자동 로그인 설정 조회
  bool isAutoLogin() {
    return _prefs.getBool(StorageConstants.autoLogin) ?? false;
  }

  // ==================== Utility ====================

  /// 모든 데이터 삭제 (로그아웃 시)
  /// 아이디 저장, 자동 로그인 설정은 유지
  Future<void> clearAll() async {
    // 로그인 설정 백업
    final rememberEmail = isRememberEmail();
    final savedEmail = getSavedEmail();
    final autoLogin = isAutoLogin();
    final selectedTheme = getSelectedTheme();

    // 모든 데이터 삭제
    await _secureStorage.deleteAll();
    await _prefs.clear();

    // 로그인 설정 복원
    if (rememberEmail && savedEmail != null) {
      await setRememberEmail(true);
      await saveSavedEmail(savedEmail);
    }
    await setAutoLogin(autoLogin);
    if (selectedTheme != null) {
      await saveSelectedTheme(selectedTheme);
    }
  }

  /// 토큰만 삭제
  Future<void> clearTokens() async {
    await _secureStorage.delete(key: StorageConstants.accessToken);
    await _secureStorage.delete(key: StorageConstants.refreshToken);
  }
}
