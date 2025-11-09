import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';

/// 출석 화면 상태
class AttendanceState {
  final bool isLoading;
  final String? errorMessage;

  // 출석 데이터
  final Set<DateTime> attendedDates;
  final Map<DateTime, String> playtimeMap; // 날짜별 학습 시간
  final int consecutiveDays;
  final int monthlyAttendDays;

  AttendanceState({
    this.isLoading = false,
    this.errorMessage,
    this.attendedDates = const {},
    this.playtimeMap = const {},
    this.consecutiveDays = 0,
    this.monthlyAttendDays = 0,
  });

  AttendanceState copyWith({
    bool? isLoading,
    String? errorMessage,
    Set<DateTime>? attendedDates,
    Map<DateTime, String>? playtimeMap,
    int? consecutiveDays,
    int? monthlyAttendDays,
  }) {
    return AttendanceState(
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
      attendedDates: attendedDates ?? this.attendedDates,
      playtimeMap: playtimeMap ?? this.playtimeMap,
      consecutiveDays: consecutiveDays ?? this.consecutiveDays,
      monthlyAttendDays: monthlyAttendDays ?? this.monthlyAttendDays,
    );
  }
}

/// 출석 Provider
class AttendanceNotifier extends StateNotifier<AttendanceState> {
  final DashboardRepository dashboardRepository;

  AttendanceNotifier(this.dashboardRepository) : super(AttendanceState()) {
    _loadAttendanceData();
  }

  /// 출석 데이터 로드
  Future<void> _loadAttendanceData() async {
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      // 현재 날짜
      final now = DateTime.now();
      final today = '${now.year.toString().substring(2)}${now.month.toString().padLeft(2, '0')}${now.day.toString().padLeft(2, '0')}';

      // 이번 달 1일
      final monthStart = DateTime(now.year, now.month, 1);
      final startDate = '${monthStart.year.toString().substring(2)}${monthStart.month.toString().padLeft(2, '0')}${monthStart.day.toString().padLeft(2, '0')}';

      // 실제 API 호출 (이번 달 출석 데이터)
      final monthData = await dashboardRepository.getAttendanceByPeriod(startDate, today);

      // 출석 데이터 처리
      final attendedDates = <DateTime>{};
      final playtimeMap = <DateTime, String>{};

      for (final item in monthData?.periodData?.attendDates ?? []) {
        final date = DateTime.parse(item.attendDate);
        attendedDates.add(date);
        playtimeMap[date] = item.playtime;
      }

      // 월간 출석일수
      final monthlyAttendDays = monthData?.periodData?.totalAttendDays ?? 0;

      // 연속 출석 계산
      final consecutiveDays = _calculateConsecutiveDays(monthData?.periodData?.attendDates ?? []);

      state = state.copyWith(
        isLoading: false,
        attendedDates: attendedDates,
        playtimeMap: playtimeMap,
        consecutiveDays: consecutiveDays,
        monthlyAttendDays: monthlyAttendDays,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: '출석 데이터를 불러오는데 실패했습니다.',
      );
    }
  }

  /// 연속 출석 일수 계산
  int _calculateConsecutiveDays(List attendDates) {
    if (attendDates.isEmpty) return 0;

    final dates = attendDates
        .map((item) => DateTime.parse(item.attendDate))
        .toList()
      ..sort((a, b) => b.compareTo(a));

    int consecutive = 1;
    final today = DateTime.now();

    if (dates.first.day != today.day ||
        dates.first.month != today.month ||
        dates.first.year != today.year) {
      return 0;
    }

    for (int i = 0; i < dates.length - 1; i++) {
      final diff = dates[i].difference(dates[i + 1]).inDays;
      if (diff == 1) {
        consecutive++;
      } else {
        break;
      }
    }

    return consecutive;
  }

  /// 특정 날짜의 출석 여부 확인
  bool isAttended(DateTime date) {
    return state.attendedDates.any((d) =>
        d.year == date.year && d.month == date.month && d.day == date.day);
  }

  /// 특정 날짜의 학습 시간 조회
  String? getPlaytime(DateTime date) {
    final key = state.playtimeMap.keys.firstWhere(
      (d) => d.year == date.year && d.month == date.month && d.day == date.day,
      orElse: () => DateTime(0),
    );
    return state.playtimeMap[key];
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadAttendanceData();
  }
}

/// 출석 Provider
final attendanceProvider =
    StateNotifierProvider<AttendanceNotifier, AttendanceState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return AttendanceNotifier(dashboardRepository);
});
