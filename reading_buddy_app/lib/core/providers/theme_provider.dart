import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../storage/token_storage.dart';
import '../theme/app_theme.dart';
import 'providers.dart';

/// 테마 상태 관리 Provider
class ThemeNotifier extends StateNotifier<String> {
  final TokenStorage _tokenStorage;

  ThemeNotifier(this._tokenStorage) : super(AppTheme.warm) {
    _loadSavedTheme();
  }

  /// 저장된 테마 불러오기
  Future<void> _loadSavedTheme() async {
    final savedTheme = _tokenStorage.getSelectedTheme();
    if (savedTheme != null) {
      state = savedTheme;
    }
  }

  /// 테마 변경
  Future<void> setTheme(String themeName) async {
    state = themeName;
    await _tokenStorage.saveSelectedTheme(themeName);
  }
}

/// 테마 Provider
final themeProvider = StateNotifierProvider<ThemeNotifier, String>((ref) {
  final tokenStorage = ref.watch(tokenStorageProvider);
  return ThemeNotifier(tokenStorage);
});
