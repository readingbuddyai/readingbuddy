import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/utils/error_state.dart';
import '../../../../core/utils/date_formatter.dart';
import '../../../../core/constants/learning_constants.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../../../core/constants/stage_constants.dart';
import '../../data/models/attendance_response.dart';
import '../../data/models/last_played_stage_response.dart';
import '../../data/models/stage_mastery_response.dart';
import '../../data/models/all_kc_mastery_response.dart';
import '../../data/models/stage_correct_rate_response.dart';

/// 홈 화면 상태
class HomeState {
  final bool isLoading;
  final ErrorState? error;

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
    this.error,
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
    ErrorState? error,
    bool? clearError,  // 에러 제거용 플래그
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
      error: clearError == true ? null : (error ?? this.error),
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
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      // 날짜 계산
      final today = DateFormatter.todayYyMMdd();
      final weekStartDate = DateFormatter.weekStartYyMMdd();
      final thirtyDaysStartDate = DateFormatter.daysAgoYyMMdd(
        LearningConstants.consecutiveDaysCheckPeriod,
      );

      // 1-3. 병렬 처리: 출석 데이터 + 마지막 플레이 스테이지 조회
      final results = await Future.wait([
        dashboardRepository.getAttendanceByDate(today),
        dashboardRepository.getAttendanceByPeriod(weekStartDate, today),
        dashboardRepository.getAttendanceByPeriod(thirtyDaysStartDate, today),
        dashboardRepository.getLastPlayedStage(),
      ]);

      final todayData = results[0].isSuccess ? results[0].dataOrNull as AttendanceResponse? : null;
      final weekData = results[1].isSuccess ? results[1].dataOrNull as AttendanceResponse? : null;
      final thirtyDaysData = results[2].isSuccess ? results[2].dataOrNull as AttendanceResponse? : null;
      final lastPlayedStage = results[3].isSuccess ? results[3].dataOrNull as LastPlayedStageResponse? : null;

      // 4. 마지막 플레이 스테이지의 숙련도 조회 (또는 기본 스테이지)
      final lastStageId = lastPlayedStage?.stage ?? '1.1.1';

      // 5. 전체 KC 숙련도 조회 (단일 API 호출로 최적화!) + 마지막 스테이지 데이터
      const allStages = StageConstants.allStages;

      // 마지막 플레이 스테이지 숙련도 & 정답률 함께 조회
      final results2 = await Future.wait([
        dashboardRepository.getAllKcAverageMastery(),
        dashboardRepository.getStageMastery(lastStageId),
        dashboardRepository.getStageCorrectRate(lastStageId),
      ]);

      final allKcMasteryData = results2[0].isSuccess ? results2[0].dataOrNull as AllKcAverageMasteryResponse? : null;
      final lastStageMastery = results2[1].isSuccess ? results2[1].dataOrNull as StageMasteryResponse? : null;
      final lastStageCorrectRateData = results2[2].isSuccess ? results2[2].dataOrNull as StageCorrectRateResponse? : null;

      // 전체 스테이지 숙련도 계산 (KC 데이터를 스테이지별로 그룹화)
      final stageMasteryMap = <String, List<double>>{};

      final kcMasteries = allKcMasteryData?.kcMasteries;
      if (kcMasteries != null) {
        for (final kc in kcMasteries) {
          if (kc.stage != null && kc.pLearn != null) {
            stageMasteryMap.putIfAbsent(kc.stage!, () => []).add(kc.pLearn!);
          }
        }
      }

      // 각 스테이지별 평균 숙련도 계산
      final masteryResults = <StageMasteryResponse?>[];
      final masteryList = <double>[];
      int completedCount = 0;

      for (final stageConfig in allStages) {
        final stageId = stageConfig.id;
        final kcMasteryList = stageMasteryMap[stageId];

        if (kcMasteryList != null && kcMasteryList.isNotEmpty) {
          final avgMastery = kcMasteryList.reduce((a, b) => a + b) / kcMasteryList.length;
          final masteryPercent = avgMastery * 100;

          masteryList.add(masteryPercent);
          masteryResults.add(StageMasteryResponse(averageMastery: avgMastery));

          // 완료 임계값 이상이면 완료로 간주
          if (masteryPercent >= LearningConstants.masteryCompletionThreshold) {
            completedCount++;
          }
        } else {
          // KC 데이터가 없는 스테이지 (2, 1.1, 1.2 등)
          masteryResults.add(null);
        }
      }

      // 평균 숙련도 계산 (전체 KC 평균 사용)
      final overallMastery = allKcMasteryData?.overallAverageMastery;
      final averageMastery = overallMastery != null
          ? overallMastery * 100
          : (masteryList.isNotEmpty
              ? masteryList.reduce((a, b) => a + b) / masteryList.length
              : 0.0);

      // 마지막 스테이지 정답률
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
          if (masteryPercent < LearningConstants.masteryCompletionThreshold) {
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
        recommendedMessage = LearningConstants.recommendReviewPrevious;
      } else {
        // 이전이 다 OK!
        final lastMasteryPercent = lastStageMastery?.masteryPercent ?? 0;
        final correctRate = lastStageCorrectRate ?? 0;

        if (correctRate >= LearningConstants.correctRateExcellentThreshold &&
            lastMasteryPercent >= LearningConstants.masteryCompletionThreshold) {
          // 완벽! 다음 단계로
          final nextStageIndex = lastStageIndex + 1;
          if (nextStageIndex < allStages.length) {
            recommendedStageId = allStages[nextStageIndex].id;
            recommendedStageName = allStages[nextStageIndex].displayName;
            recommendedMessage = LearningConstants.recommendNextStage;
          } else {
            // 마지막 스테이지까지 다 완료
            recommendedStageId = lastStageId;
            final stageConfig = StageConfig.findById(lastStageId);
            recommendedStageName = stageConfig?.displayName ?? lastStageId;
            recommendedMessage = LearningConstants.congratsAllComplete;
          }
        } else if (correctRate >= LearningConstants.correctRateExcellentThreshold &&
            lastMasteryPercent < LearningConstants.masteryCompletionThreshold) {
          // 방금 잘함! 조금만 더
          recommendedStageId = lastStageId;
          final stageConfig = StageConfig.findById(lastStageId);
          recommendedStageName = stageConfig?.displayName ?? lastStageId;
          recommendedMessage = LearningConstants.recommendPracticeMore;
        } else {
          // 나머지 → 다시 도전
          recommendedStageId = lastStageId;
          final stageConfig = StageConfig.findById(lastStageId);
          recommendedStageName = stageConfig?.displayName ?? lastStageId;
          recommendedMessage = LearningConstants.recommendRetry;
        }
      }

      // 오늘 출석 데이터
      final attendedToday = todayData?.dailyData?.attended ?? false;
      final todayPlaytime = todayData?.dailyData?.playtime ?? '00:00';

      // 주간 데이터
      final weeklyAttendDays = weekData?.periodData?.totalAttendDays ?? 0;
      final weeklyPlaytime = DateFormatter.sumPlaytimes(
        weekData?.periodData?.attendDates.map((e) => e.playtime).toList() ?? [],
      );

      // 연속 출석 계산 (최근 30일 데이터 사용)
      final consecutiveDays = DateFormatter.calculateConsecutiveDays(
        thirtyDaysData?.periodData?.attendDates.map((e) => DateTime.parse(e.attendDate)).toList() ?? [],
      );

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
        error: ErrorState.unknown('데이터를 불러오는데 실패했습니다.'),
      );
    }
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
