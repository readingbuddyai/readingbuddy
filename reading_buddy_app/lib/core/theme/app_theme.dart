import 'package:flutter/material.dart';

/// 앱 테마 설정
class AppTheme {
  // 테마 모드 옵션
  static const String light = 'light'; // 라이트 모드
  static const String dark = 'dark'; // 다크 모드

  // 앱 고유 색상 (테마 무관)
  static const successColor = Color(0xFF4CAF50); // 성공 (녹색)
  static const warningColor = Color(0xFFFF9800); // 경고 (주황색)
  static const errorColor = Color(0xFFF44336); // 에러 (빨간색)

  /// 라이트 모드 테마
  static ThemeData lightTheme() {
    const primaryColor = Color(0xFF2196F3); // Blue
    const secondaryColor = Color(0xFF03A9F4); // Light Blue
    const backgroundColor = Color(0xFFF5F5F5);
    const surfaceColor = Colors.white;
    const textPrimaryColor = Color(0xFF212121);
    const textSecondaryColor = Color(0xFF757575);

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      colorScheme: const ColorScheme.light(
        primary: primaryColor,
        secondary: secondaryColor,
        surface: surfaceColor,
        error: errorColor,
        onPrimary: Colors.white,
        onSecondary: Colors.white,
        onSurface: textPrimaryColor,
        onError: Colors.white,
      ),
      scaffoldBackgroundColor: backgroundColor,
      appBarTheme: const AppBarTheme(
        elevation: 0,
        centerTitle: true,
        backgroundColor: surfaceColor,
        foregroundColor: textPrimaryColor,
        iconTheme: IconThemeData(color: textPrimaryColor),
      ),
      cardTheme: CardThemeData(
        elevation: 1,
        color: surfaceColor,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
        ),
        filled: true,
        fillColor: surfaceColor,
      ),
      textTheme: const TextTheme(
        bodyLarge: TextStyle(color: textPrimaryColor),
        bodyMedium: TextStyle(color: textSecondaryColor),
        bodySmall: TextStyle(color: textSecondaryColor),
      ),
      iconTheme: const IconThemeData(color: textPrimaryColor),
      dividerColor: const Color(0xFFE0E0E0),
    );
  }

  /// 다크 모드 테마
  static ThemeData darkTheme() {
    const primaryColor = Color(0xFF42A5F5); // Lighter Blue for dark mode
    const secondaryColor = Color(0xFF29B6F6); // Lighter Blue
    const backgroundColor = Color(0xFF121212);
    const surfaceColor = Color(0xFF1E1E1E);
    const textPrimaryColor = Color(0xFFE0E0E0);
    const textSecondaryColor = Color(0xFF9E9E9E);

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: const ColorScheme.dark(
        primary: primaryColor,
        secondary: secondaryColor,
        surface: surfaceColor,
        error: errorColor,
        onPrimary: backgroundColor,
        onSecondary: backgroundColor,
        onSurface: textPrimaryColor,
        onError: backgroundColor,
      ),
      scaffoldBackgroundColor: backgroundColor,
      appBarTheme: const AppBarTheme(
        elevation: 0,
        centerTitle: true,
        backgroundColor: surfaceColor,
        foregroundColor: textPrimaryColor,
        iconTheme: IconThemeData(color: textPrimaryColor),
      ),
      cardTheme: CardThemeData(
        elevation: 1,
        color: surfaceColor,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
        ),
        filled: true,
        fillColor: surfaceColor,
      ),
      textTheme: const TextTheme(
        bodyLarge: TextStyle(color: textPrimaryColor),
        bodyMedium: TextStyle(color: textSecondaryColor),
        bodySmall: TextStyle(color: textSecondaryColor),
      ),
      iconTheme: const IconThemeData(color: textPrimaryColor),
      dividerColor: const Color(0xFF424242),
    );
  }

  /// 선택된 테마 모드에 따라 ThemeData 반환
  static ThemeData getTheme(String themeMode) {
    switch (themeMode) {
      case dark:
        return darkTheme();
      case light:
      default:
        return lightTheme();
    }
  }

  /// 정답률에 따른 색상 반환 (테마 무관)
  static Color getScoreColor(double score) {
    if (score >= 80) return successColor;
    if (score >= 60) return warningColor;
    return errorColor;
  }

  /// 출석 상태 색상 (테마 무관)
  static Color getAttendanceColor(bool attended) {
    return attended ? successColor : const Color(0xFF9E9E9E);
  }
}
