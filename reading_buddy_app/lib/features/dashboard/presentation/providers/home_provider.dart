import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../data/models/attendance_response.dart';

/// 홈 화면 상태
class HomeState {
  final bool isLoading;
  final String? errorMessage;

  // 출석 데이터
  final bool attendedToday;
  final int consecutiveDays;
  final String todayPlaytime;

  // 학습 시간 데이터
  final String weeklyPlaytime;
  final int weeklyAttendDays;

  // 최근 스테이지 데이터
  final String? lastStage;
  final double? lastCorrectRate;

  HomeState({
    this.isLoading = false,
    this.errorMessage,
    this.attendedToday = false,
    this.consecutiveDays = 0,
    this.todayPlaytime = '00:00',
    this.weeklyPlaytime = '0시간 0분',
    this.weeklyAttendDays = 0,
    this.lastStage,
    this.lastCorrectRate,
  });

  HomeState copyWith({
    bool? isLoading,
    String? errorMessage,
    bool? attendedToday,
    int? consecutiveDays,
    String? todayPlaytime,
    String? weeklyPlaytime,
    int? weeklyAttendDays,
    String? lastStage,
    double? lastCorrectRate,
  }) {
    return HomeState(
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
      attendedToday: attendedToday ?? this.attendedToday,
      consecutiveDays: consecutiveDays ?? this.consecutiveDays,
      todayPlaytime: todayPlaytime ?? this.todayPlaytime,
      weeklyPlaytime: weeklyPlaytime ?? this.weeklyPlaytime,
      weeklyAttendDays: weeklyAttendDays ?? this.weeklyAttendDays,
      lastStage: lastStage ?? this.lastStage,
      lastCorrectRate: lastCorrectRate ?? this.lastCorrectRate,
    );
  }
}

/// 홈 화면 Provider
class HomeNotifier extends StateNotifier<HomeState> {
  final DashboardRepository dashboardRepository;

  HomeNotifier(this.dashboardRepository) : super(HomeState()) {
    _loadHomeData();
  }

  /// 홈 화면 데이터 로드
  Future<void> _loadHomeData() async {
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      // 오늘 날짜 (yyMMdd 형식)
      final now = DateTime.now();
      final today = '${now.year.toString().substring(2)}${now.month.toString().padLeft(2, '0')}${now.day.toString().padLeft(2, '0')}';

      // 이번 주 시작일 (월요일)
      final weekStart = now.subtract(Duration(days: now.weekday - 1));
      final startDate = '${weekStart.year.toString().substring(2)}${weekStart.month.toString().padLeft(2, '0')}${weekStart.day.toString().padLeft(2, '0')}';

      // 실제 API 호출
      final todayData = await dashboardRepository.getAttendanceByDate(today);
      final weekData = await dashboardRepository.getAttendanceByPeriod(startDate, today);

      // 최근 스테이지 정답률 (기본: Stage 1.1.1)
      final correctRateData = await dashboardRepository.getStageCorrectRate('1.1.1');

      // 오늘 출석 데이터
      final attendedToday = todayData?.dailyData?.attended ?? false;
      final todayPlaytime = todayData?.dailyData?.playtime ?? '00:00';

      // 주간 데이터
      final weeklyAttendDays = weekData?.periodData?.totalAttendDays ?? 0;
      final weeklyPlaytime = _calculateTotalPlaytime(weekData?.periodData?.attendDates ?? []);

      // 연속 출석 계산 (간단 버전 - 실제로는 백엔드에서 계산하는게 좋음)
      final consecutiveDays = _calculateConsecutiveDays(weekData?.periodData?.attendDates ?? []);

      state = state.copyWith(
        isLoading: false,
        attendedToday: attendedToday,
        consecutiveDays: consecutiveDays,
        todayPlaytime: todayPlaytime,
        weeklyPlaytime: weeklyPlaytime,
        weeklyAttendDays: weeklyAttendDays,
        lastStage: correctRateData != null ? 'Stage ${correctRateData.stage}' : null,
        lastCorrectRate: correctRateData?.correctRate,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: '데이터를 불러오는데 실패했습니다.',
      );
    }
  }

  /// 총 학습 시간 계산 (분:초를 시간 분 형식으로)
  String _calculateTotalPlaytime(List<AttendDateInfo> attendDates) {
    int totalMinutes = 0;
    int totalSeconds = 0;

    for (final item in attendDates) {
      final parts = item.playtime.split(':');
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

  /// 연속 출석 일수 계산
  int _calculateConsecutiveDays(List<AttendDateInfo> attendDates) {
    if (attendDates.isEmpty) return 0;

    // 날짜를 DateTime으로 변환하고 정렬
    final dates = attendDates
        .map((item) => DateTime.parse(item.attendDate))
        .toList()
      ..sort((a, b) => b.compareTo(a)); // 최신순 정렬

    int consecutive = 1;
    final today = DateTime.now();

    // 오늘 출석하지 않았으면 0
    if (dates.first.day != today.day ||
        dates.first.month != today.month ||
        dates.first.year != today.year) {
      return 0;
    }

    // 연속 일수 계산
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

  /// 새로고침
  Future<void> refresh() async {
    await _loadHomeData();
  }

  /// 출석 체크
  Future<bool> checkAttendance() async {
    try {
      // TODO: 실제 API 호출
      await Future.delayed(const Duration(milliseconds: 500));

      state = state.copyWith(
        attendedToday: true,
        consecutiveDays: state.consecutiveDays + 1,
      );

      return true;
    } catch (e) {
      return false;
    }
  }
}

/// 홈 화면 Provider
final homeProvider = StateNotifierProvider<HomeNotifier, HomeState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return HomeNotifier(dashboardRepository);
});
