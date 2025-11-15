import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../storage/token_storage.dart';
import '../theme/app_theme.dart';
import 'providers.dart';

/// 테마 상태 관리 Provider
class ThemeNotifier extends StateNotifier<String> {
  final TokenStorage _tokenStorage;

  ThemeNotifier(this._tokenStorage) : super(AppTheme.light) {
    _loadSavedTheme();
  }

  /// 저장된 테마 불러오기
  Future<void> _loadSavedTheme() async {
    final savedTheme = _tokenStorage.getSelectedTheme();
    if (savedTheme != null) {
      // 기존 테마 이름을 새 테마 모드로 마이그레이션
      if (savedTheme == 'warm' || savedTheme == 'cool' || savedTheme == 'green') {
        state = AppTheme.light;
      } else {
        state = savedTheme;
      }
    }
  }

  /// 테마 모드 변경
  Future<void> setTheme(String themeMode) async {
    state = themeMode;
    await _tokenStorage.saveSelectedTheme(themeMode);
  }

  /// 다크 모드 토글
  Future<void> toggleTheme() async {
    final newTheme = state == AppTheme.light ? AppTheme.dark : AppTheme.light;
    await setTheme(newTheme);
  }

  /// 현재 다크 모드 여부
  bool get isDarkMode => state == AppTheme.dark;
}

/// 테마 Provider
final themeProvider = StateNotifierProvider<ThemeNotifier, String>((ref) {
  final tokenStorage = ref.watch(tokenStorageProvider);
  return ThemeNotifier(tokenStorage);
});
