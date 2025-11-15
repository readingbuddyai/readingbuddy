/// 로컬 저장소 키 상수
class StorageConstants {
  // Secure Storage Keys (민감한 정보)
  static const String accessToken = 'access_token';
  static const String refreshToken = 'refresh_token';
  static const String savedPassword = 'saved_password'; // 자동 로그인용 비밀번호

  // Shared Preferences Keys
  static const String userId = 'user_id';
  static const String userEmail = 'user_email';
  static const String userNickname = 'user_nickname';
  static const String isLoggedIn = 'is_logged_in';
  static const String selectedTheme = 'selected_theme';

  // 로그인 설정
  static const String savedEmail = 'saved_email'; // 아이디 저장용 이메일
  static const String rememberEmail = 'remember_email'; // 아이디 저장 여부
  static const String autoLogin = 'auto_login'; // 자동 로그인 여부
}
