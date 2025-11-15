/// 날짜 포맷 유틸리티 클래스
class DateFormatter {
  DateFormatter._();

  /// DateTime을 yyMMdd 형식의 문자열로 변환
  ///
  /// 예: 2025-01-15 → "250115"
  static String toYyMMdd(DateTime date) {
    return '${date.year.toString().substring(2)}${date.month.toString().padLeft(2, '0')}${date.day.toString().padLeft(2, '0')}';
  }

  /// 오늘 날짜를 yyMMdd 형식으로 반환
  ///
  /// 예: "250115"
  static String todayYyMMdd() {
    return toYyMMdd(DateTime.now());
  }

  /// 특정 기간 전 날짜를 yyMMdd 형식으로 반환
  ///
  /// [days] 며칠 전 (양수)
  /// 예: daysAgoYyMMdd(7) → 7일 전 날짜
  static String daysAgoYyMMdd(int days) {
    final date = DateTime.now().subtract(Duration(days: days));
    return toYyMMdd(date);
  }

  /// 이번 주 시작일(월요일)을 yyMMdd 형식으로 반환
  static String weekStartYyMMdd() {
    final now = DateTime.now();
    final weekStart = now.subtract(Duration(days: now.weekday - 1));
    return toYyMMdd(weekStart);
  }

  /// 이번 달 1일을 yyMMdd 형식으로 반환
  static String monthStartYyMMdd() {
    final now = DateTime.now();
    final monthStart = DateTime(now.year, now.month, 1);
    return toYyMMdd(monthStart);
  }

  /// MM:SS 형식을 "X시간 Y분" 또는 "Y분" 형식으로 변환
  ///
  /// 예: "125:30" → "2시간 5분"
  ///     "45:30" → "45분"
  static String playtimeToReadable(String playtime) {
    final parts = playtime.split(':');
    if (parts.length != 2) return '0분';

    int totalMinutes = int.tryParse(parts[0]) ?? 0;
    int totalSeconds = int.tryParse(parts[1]) ?? 0;

    totalMinutes += totalSeconds ~/ 60;
    final hours = totalMinutes ~/ 60;
    final minutes = totalMinutes % 60;

    if (hours > 0) {
      return '$hours시간 $minutes분';
    } else {
      return '$minutes분';
    }
  }

  /// 여러 playtime을 합산하여 "X시간 Y분" 형식으로 반환
  ///
  /// [playtimes] MM:SS 형식의 문자열 리스트
  static String sumPlaytimes(List<String> playtimes) {
    int totalMinutes = 0;
    int totalSeconds = 0;

    for (final playtime in playtimes) {
      final parts = playtime.split(':');
      if (parts.length == 2) {
        totalMinutes += int.tryParse(parts[0]) ?? 0;
        totalSeconds += int.tryParse(parts[1]) ?? 0;
      }
    }

    totalMinutes += totalSeconds ~/ 60;
    totalSeconds = totalSeconds % 60;

    final hours = totalMinutes ~/ 60;
    final minutes = totalMinutes % 60;

    if (hours > 0) {
      return '$hours시간 $minutes분';
    } else {
      return '$minutes분';
    }
  }

  /// DateTime 리스트에서 연속 출석 일수 계산
  ///
  /// 오늘부터 역순으로 연속된 날짜를 카운트
  /// 오늘 출석하지 않았으면 0 반환
  static int calculateConsecutiveDays(List<DateTime> dates) {
    if (dates.isEmpty) return 0;

    final sortedDates = List<DateTime>.from(dates)
      ..sort((a, b) => b.compareTo(a)); // 최신순 정렬

    final today = DateTime.now();
    final todayDate = DateTime(today.year, today.month, today.day);
    final firstDate = DateTime(
      sortedDates.first.year,
      sortedDates.first.month,
      sortedDates.first.day,
    );

    // 오늘 출석하지 않았으면 0
    if (firstDate != todayDate) return 0;

    int consecutive = 1;

    // 연속 일수 계산
    for (int i = 0; i < sortedDates.length - 1; i++) {
      final current = DateTime(
        sortedDates[i].year,
        sortedDates[i].month,
        sortedDates[i].day,
      );
      final next = DateTime(
        sortedDates[i + 1].year,
        sortedDates[i + 1].month,
        sortedDates[i + 1].day,
      );

      final diff = current.difference(next).inDays;
      if (diff == 1) {
        consecutive++;
      } else {
        break;
      }
    }

    return consecutive;
  }

  /// 두 날짜가 같은 날인지 비교 (년월일만)
  static bool isSameDay(DateTime a, DateTime b) {
    return a.year == b.year && a.month == b.month && a.day == b.day;
  }
}
