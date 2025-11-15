/// Auth Repository 인터페이스
abstract class AuthRepository {
  /// 로그인
  Future<bool> login(String email, String password);

  /// 회원가입
  Future<bool> signup(String email, String password, String nickname);

  /// 로그아웃
  Future<void> logout();

  /// 로그인 상태 확인
  Future<bool> isLoggedIn();

  /// VR 기기 인증 (Device Code 입력)
  /// 성공 시 null 반환, 실패 시 에러 메시지 반환
  Future<String?> authorizeDeviceCode(String deviceAuthCode);
}
