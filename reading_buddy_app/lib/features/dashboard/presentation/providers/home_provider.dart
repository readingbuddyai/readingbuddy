import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../../../core/constants/stage_constants.dart';
import '../../data/models/attendance_response.dart';
import '../../data/models/last_played_stage_response.dart';
import '../../data/models/stage_correct_rate_response.dart';

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
  final String? lastStageId;
  final String? lastStageName;
  final double? lastStageMastery;
  final double? lastStageCorrectRate;

  // 추천 학습 데이터
  final String? recommendedStageId;
  final String? recommendedStageName;
  final String? recommendedMessage;

  // 전체 학습 성과 데이터
  final double? averageMastery;
  final int completedStageCount;
  final int totalAttendDays;  // 최근 30일 총 출석 일수

  HomeState({
    this.isLoading = false,
    this.errorMessage,
    this.attendedToday = false,
    this.consecutiveDays = 0,
    this.todayPlaytime = '00:00',
    this.weeklyPlaytime = '0시간 0분',
    this.weeklyAttendDays = 0,
    this.lastStageId,
    this.lastStageName,
    this.lastStageMastery,
    this.lastStageCorrectRate,
    this.recommendedStageId,
    this.recommendedStageName,
    this.recommendedMessage,
    this.averageMastery,
    this.completedStageCount = 0,
    this.totalAttendDays = 0,
  });

  HomeState copyWith({
    bool? isLoading,
    String? errorMessage,
    bool? attendedToday,
    int? consecutiveDays,
    String? todayPlaytime,
    String? weeklyPlaytime,
    int? weeklyAttendDays,
    String? lastStageId,
    String? lastStageName,
    double? lastStageMastery,
    double? lastStageCorrectRate,
    String? recommendedStageId,
    String? recommendedStageName,
    String? recommendedMessage,
    double? averageMastery,
    int? completedStageCount,
    int? totalAttendDays,
  }) {
    return HomeState(
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
      attendedToday: attendedToday ?? this.attendedToday,
      consecutiveDays: consecutiveDays ?? this.consecutiveDays,
      todayPlaytime: todayPlaytime ?? this.todayPlaytime,
      weeklyPlaytime: weeklyPlaytime ?? this.weeklyPlaytime,
      weeklyAttendDays: weeklyAttendDays ?? this.weeklyAttendDays,
      lastStageId: lastStageId ?? this.lastStageId,
      lastStageName: lastStageName ?? this.lastStageName,
      lastStageMastery: lastStageMastery ?? this.lastStageMastery,
      lastStageCorrectRate: lastStageCorrectRate ?? this.lastStageCorrectRate,
      recommendedStageId: recommendedStageId ?? this.recommendedStageId,
      recommendedStageName: recommendedStageName ?? this.recommendedStageName,
      recommendedMessage: recommendedMessage ?? this.recommendedMessage,
      averageMastery: averageMastery ?? this.averageMastery,
      completedStageCount: completedStageCount ?? this.completedStageCount,
      totalAttendDays: totalAttendDays ?? this.totalAttendDays,
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
      final weekStartDate = '${weekStart.year.toString().substring(2)}${weekStart.month.toString().padLeft(2, '0')}${weekStart.day.toString().padLeft(2, '0')}';

      // 최근 30일 (연속 출석 계산용)
      final thirtyDaysAgo = now.subtract(const Duration(days: 30));
      final thirtyDaysStartDate = '${thirtyDaysAgo.year.toString().substring(2)}${thirtyDaysAgo.month.toString().padLeft(2, '0')}${thirtyDaysAgo.day.toString().padLeft(2, '0')}';

      // 1-3. 병렬 처리: 출석 데이터 + 마지막 플레이 스테이지 조회
      final results = await Future.wait([
        dashboardRepository.getAttendanceByDate(today),
        dashboardRepository.getAttendanceByPeriod(weekStartDate, today),
        dashboardRepository.getAttendanceByPeriod(thirtyDaysStartDate, today),
        dashboardRepository.getLastPlayedStage(),
      ]);

      final todayData = results[0] as AttendanceResponse?;
      final weekData = results[1] as AttendanceResponse?;
      final thirtyDaysData = results[2] as AttendanceResponse?;
      final lastPlayedStage = results[3] as LastPlayedStageResponse?;

      // 4. 마지막 플레이 스테이지의 숙련도 조회 (또는 기본 스테이지)
      final lastStageId = lastPlayedStage?.stage ?? '1.1.1';

      // 5. 전체 스테이지 숙련도 조회 (8개) - 병렬 처리!
      final allStages = StageConstants.allStages;
      final masteryFutures = allStages.map((stageConfig) {
        return dashboardRepository.getStageMastery(stageConfig.id);
      }).toList();

      // 마지막 플레이 스테이지 숙련도 & 정답률 함께 조회
      masteryFutures.add(dashboardRepository.getStageMastery(lastStageId));

      final correctRateFuture = dashboardRepository.getStageCorrectRate(lastStageId);

      final results2 = await Future.wait([
        Future.wait(masteryFutures),
        correctRateFuture,
      ]);

      final masteryResults = results2[0] as List;
      final lastStageCorrectRateData = results2[1] as StageCorrectRateResponse?;

      // 전체 스테이지 숙련도 계산
      final masteryList = <double>[];
      int completedCount = 0;

      for (int i = 0; i < allStages.length; i++) {
        final mastery = masteryResults[i];
        if (mastery != null && mastery.averageMastery != null) {
          final masteryPercent = mastery.averageMastery! * 100;
          masteryList.add(masteryPercent);

          // 70% 이상이면 완료로 간주
          if (masteryPercent >= 70) {
            completedCount++;
          }
        }
      }

      // 평균 숙련도 계산
      final averageMastery = masteryList.isNotEmpty
          ? masteryList.reduce((a, b) => a + b) / masteryList.length
          : 0.0;

      // 마지막 스테이지 숙련도 & 정답률
      final lastStageMastery = masteryResults.last;
      final lastStageCorrectRate = lastStageCorrectRateData?.correctRate;

      // 6. 똑똑한 추천 로직
      String? recommendedStageId;
      String? recommendedStageName;
      String? recommendedMessage;

      // 6-1. 순차 체크: 1.1.1부터 마지막 이전까지 낮은(70% 미만) 것 찾기
      final lastStageIndex = allStages.indexWhere((s) => s.id == lastStageId);
      String? firstLowMasteryStageId;

      for (int i = 0; i < allStages.length; i++) {
        // 마지막 스테이지 이전까지만 체크
        if (i >= lastStageIndex && lastStageIndex != -1) break;

        final mastery = masteryResults[i];
        if (mastery != null && mastery.averageMastery != null) {
          final masteryPercent = mastery.averageMastery! * 100;
          if (masteryPercent < 70) {
            firstLowMasteryStageId = allStages[i].id;
            break;
          }
        } else {
          // 데이터가 없으면 낮은 것으로 간주
          firstLowMasteryStageId = allStages[i].id;
          break;
        }
      }

      // 6-2. 추천 결정
      if (firstLowMasteryStageId != null) {
        // 이전에 낮은 게 있으면 → 그것 추천
        recommendedStageId = firstLowMasteryStageId;
        final stageConfig = StageConfig.findById(firstLowMasteryStageId);
        recommendedStageName = stageConfig?.displayName ?? firstLowMasteryStageId;
        recommendedMessage = '이전 단계를 먼저 완성해보세요!';
      } else {
        // 이전이 다 OK!
        final lastMasteryPercent = lastStageMastery?.masteryPercent ?? 0;
        final correctRate = lastStageCorrectRate ?? 0;

        if (correctRate >= 80 && lastMasteryPercent >= 70) {
          // 완벽! 다음 단계로
          final nextStageIndex = lastStageIndex + 1;
          if (nextStageIndex < allStages.length) {
            recommendedStageId = allStages[nextStageIndex].id;
            recommendedStageName = allStages[nextStageIndex].displayName;
            recommendedMessage = '완벽해요! 다음 단계로 가볼까요?';
          } else {
            // 마지막 스테이지까지 다 완료
            recommendedStageId = lastStageId;
            final stageConfig = StageConfig.findById(lastStageId);
            recommendedStageName = stageConfig?.displayName ?? lastStageId;
            recommendedMessage = '모든 단계를 완료했어요! 🎉';
          }
        } else if (correctRate >= 80 && lastMasteryPercent < 70) {
          // 방금 잘함! 조금만 더
          recommendedStageId = lastStageId;
          final stageConfig = StageConfig.findById(lastStageId);
          recommendedStageName = stageConfig?.displayName ?? lastStageId;
          recommendedMessage = '잘하셨어요! 조금만 더 연습하면 완성!';
        } else {
          // 나머지 → 다시 도전
          recommendedStageId = lastStageId;
          final stageConfig = StageConfig.findById(lastStageId);
          recommendedStageName = stageConfig?.displayName ?? lastStageId;
          recommendedMessage = '다시 도전해보세요!';
        }
      }

      // 오늘 출석 데이터
      final attendedToday = todayData?.dailyData?.attended ?? false;
      final todayPlaytime = todayData?.dailyData?.playtime ?? '00:00';

      // 주간 데이터
      final weeklyAttendDays = weekData?.periodData?.totalAttendDays ?? 0;
      final weeklyPlaytime = _calculateTotalPlaytime(weekData?.periodData?.attendDates ?? []);

      // 연속 출석 계산 (최근 30일 데이터 사용)
      final consecutiveDays = _calculateConsecutiveDays(thirtyDaysData?.periodData?.attendDates ?? []);

      // 전체 출석 일수 (최근 30일)
      final totalAttendDays = thirtyDaysData?.periodData?.totalAttendDays ?? 0;

      // 스테이지 이름 변환
      final stageConfig = StageConfig.findById(lastStageId);
      final lastStageName = stageConfig?.displayName ?? 'Stage $lastStageId';

      state = state.copyWith(
        isLoading: false,
        attendedToday: attendedToday,
        consecutiveDays: consecutiveDays,
        todayPlaytime: todayPlaytime,
        weeklyPlaytime: weeklyPlaytime,
        weeklyAttendDays: weeklyAttendDays,
        lastStageId: lastStageId,
        lastStageName: lastStageName,
        lastStageMastery: lastStageMastery?.masteryPercent,
        lastStageCorrectRate: lastStageCorrectRate,
        recommendedStageId: recommendedStageId,
        recommendedStageName: recommendedStageName,
        recommendedMessage: recommendedMessage,
        averageMastery: averageMastery,
        completedStageCount: completedCount,
        totalAttendDays: totalAttendDays,
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
