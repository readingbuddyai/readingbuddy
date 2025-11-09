import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../../../core/providers/providers.dart';

/// 로그인 상태 Provider
final authStateProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  final authRepository = ref.watch(authRepositoryProvider);
  return AuthNotifier(authRepository);
});

/// 로그인 상태
class AuthState {
  final bool isLoading;
  final bool isLoggedIn;
  final String? errorMessage;

  AuthState({
    this.isLoading = false,
    this.isLoggedIn = false,
    this.errorMessage,
  });

  AuthState copyWith({
    bool? isLoading,
    bool? isLoggedIn,
    String? errorMessage,
  }) {
    return AuthState(
      isLoading: isLoading ?? this.isLoading,
      isLoggedIn: isLoggedIn ?? this.isLoggedIn,
      errorMessage: errorMessage,
    );
  }
}

/// Auth Notifier
class AuthNotifier extends StateNotifier<AuthState> {
  final AuthRepository authRepository;

  AuthNotifier(this.authRepository) : super(AuthState()) {
    _checkLoginStatus();
  }

  /// 초기 로그인 상태 확인
  Future<void> _checkLoginStatus() async {
    final isLoggedIn = await authRepository.isLoggedIn();
    state = state.copyWith(isLoggedIn: isLoggedIn);
  }

  /// 로그인
  Future<bool> login(String email, String password) async {
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      final success = await authRepository.login(email, password);

      if (success) {
        state = state.copyWith(isLoading: false, isLoggedIn: true);
        return true;
      } else {
        state = state.copyWith(
          isLoading: false,
          errorMessage: '이메일 또는 비밀번호가 올바르지 않습니다.',
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: '로그인 중 오류가 발생했습니다.',
      );
      return false;
    }
  }

  /// 회원가입
  Future<bool> signup(String email, String password, String nickname) async {
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      final success = await authRepository.signup(email, password, nickname);

      if (success) {
        state = state.copyWith(isLoading: false);
        return true;
      } else {
        state = state.copyWith(
          isLoading: false,
          errorMessage: '회원가입에 실패했습니다.',
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: '회원가입 중 오류가 발생했습니다.',
      );
      return false;
    }
  }

  /// 로그아웃
  Future<void> logout() async {
    await authRepository.logout();
    state = state.copyWith(isLoggedIn: false);
  }

  /// VR 기기 인증
  Future<String?> authorizeDevice(String deviceCode) async {
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      final errorMessage = await authRepository.authorizeDeviceCode(deviceCode);
      state = state.copyWith(isLoading: false);
      return errorMessage; // null이면 성공
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'VR 기기 인증 중 오류가 발생했습니다.',
      );
      return 'VR 기기 인증 중 오류가 발생했습니다.';
    }
  }
}
