import 'package:logger/logger.dart';
import 'package:jwt_decode/jwt_decode.dart';
import '../../domain/repositories/auth_repository.dart';
import '../models/login_request.dart';
import '../models/signup_request.dart';
import '../models/device_code_request.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/storage/token_storage.dart';

/// Auth Repository 구현체
class AuthRepositoryImpl implements AuthRepository {
  final ApiClient _apiClient;
  final TokenStorage _tokenStorage;
  final Logger _logger = Logger();

  AuthRepositoryImpl(this._apiClient, this._tokenStorage);

  @override
  Future<bool> login(String email, String password) async {
    try {
      final request = LoginRequest(email: email, password: password);
      final response = await _apiClient.login(request);

      // ApiResponse 형식으로 변경되어 data 필드에서 토큰 정보 추출
      if (response.isSuccess && response.data != null) {
        // 토큰 저장
        await _tokenStorage.saveAccessToken(response.data!.accessToken);
        await _tokenStorage.saveRefreshToken(response.data!.refreshToken);
        await _tokenStorage.setLoggedIn(true);

        // JWT에서 사용자 정보 추출 및 저장
        try {
          final decodedToken = Jwt.parseJwt(response.data!.accessToken);
          // JWT 필드명은 user_id, user_email, user_nickname (스네이크 케이스)
          final userIdStr = decodedToken['user_id'] as String?;
          final userId = userIdStr != null ? int.tryParse(userIdStr) : null;
          final userEmail = decodedToken['user_email'] as String?;
          final nickname = decodedToken['user_nickname'] as String?;

          if (userId != null) {
            await _tokenStorage.saveUserId(userId);
          }
          if (userEmail != null) {
            await _tokenStorage.saveUserEmail(userEmail);
          }
          if (nickname != null) {
            await _tokenStorage.saveUserNickname(nickname);
          }

          _logger.i('사용자 정보 저장 완료: userId=$userId, email=$userEmail, nickname=$nickname');
        } catch (e) {
          _logger.w('JWT 디코딩 실패: $e');
          // JWT 디코딩 실패해도 로그인은 성공으로 처리
        }

        _logger.i('로그인 성공: ${response.message}');
        return true;
      } else {
        _logger.e('로그인 실패: ${response.message}');
        return false;
      }
    } catch (e) {
      _logger.e('로그인 실패: $e');
      return false;
    }
  }

  @override
  Future<bool> signup(String email, String password, String nickname) async {
    try {
      final request = SignupRequest(
        email: email,
        password: password,
        nickname: nickname,
      );
      final response = await _apiClient.signup(request);

      // ApiResponse 형식으로 변경됨
      if (response.isSuccess) {
        _logger.i('회원가입 성공: ${response.message}');
        return true;
      } else {
        _logger.e('회원가입 실패: ${response.message}');
        return false;
      }
    } catch (e) {
      _logger.e('회원가입 실패: $e');
      return false;
    }
  }

  @override
  Future<void> logout() async {
    try {
      await _tokenStorage.clearAll();
      _logger.i('로그아웃 완료');
    } catch (e) {
      _logger.e('로그아웃 실패: $e');
      rethrow;
    }
  }

  @override
  Future<bool> isLoggedIn() async {
    try {
      return _tokenStorage.isLoggedIn();
    } catch (e) {
      _logger.e('로그인 상태 확인 실패: $e');
      return false;
    }
  }

  @override
  Future<String?> authorizeDeviceCode(String deviceAuthCode) async {
    try {
      final request = DeviceCodeRequest(deviceAuthCode: deviceAuthCode);
      final response = await _apiClient.authorizeDevice(request);

      if (response.isSuccess) {
        _logger.i('VR 기기 인증 성공');
        return null; // 성공 시 null 반환
      } else {
        return response.message;
      }
    } catch (e) {
      _logger.e('VR 기기 인증 실패: $e');
      return '기기 인증 중 오류가 발생했습니다.';
    }
  }
}
