import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/utils/error_state.dart';
import '../../../../core/utils/date_formatter.dart';
import '../../../../core/constants/stage_constants.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../data/models/stage_kc_mastery_trend_response.dart';

/// 학습 추이 기간
enum TrendPeriod {
  week('1주일', 7),
  month('1개월', 30);

  final String label;
  final int days;

  const TrendPeriod(this.label, this.days);
}

/// 학습 추이 상태
class LearningTrendState {
  final bool isLoading;
  final ErrorState? error;
  final TrendPeriod period;
  final Map<String, StageKcMasteryTrendResponse> trendData; // stage -> trend data

  // 기간별 캐싱된 데이터
  final Map<String, StageKcMasteryTrendResponse> weekTrendData; // 1주일 데이터 캐시
  final Map<String, StageKcMasteryTrendResponse> monthTrendData; // 1개월 데이터 캐시

  LearningTrendState({
    this.isLoading = false,
    this.error,
    this.period = TrendPeriod.week,
    this.trendData = const {},
    this.weekTrendData = const {},
    this.monthTrendData = const {},
  });

  LearningTrendState copyWith({
    bool? isLoading,
    ErrorState? error,
    bool? clearError,
    TrendPeriod? period,
    Map<String, StageKcMasteryTrendResponse>? trendData,
    Map<String, StageKcMasteryTrendResponse>? weekTrendData,
    Map<String, StageKcMasteryTrendResponse>? monthTrendData,
  }) {
    return LearningTrendState(
      isLoading: isLoading ?? this.isLoading,
      error: clearError == true ? null : (error ?? this.error),
      period: period ?? this.period,
      trendData: trendData ?? this.trendData,
      weekTrendData: weekTrendData ?? this.weekTrendData,
      monthTrendData: monthTrendData ?? this.monthTrendData,
    );
  }
}

/// 학습 추이 Provider
class LearningTrendNotifier extends StateNotifier<LearningTrendState> {
  final DashboardRepository dashboardRepository;

  LearningTrendNotifier(this.dashboardRepository)
      : super(LearningTrendState()) {
    _loadAllTrendData();
  }

  /// 모든 기간의 추이 데이터 로드 (1주일 + 1개월 동시 로드)
  Future<void> _loadAllTrendData() async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      final endDate = DateFormatter.todayYyMMdd();

      // 1주일과 1개월 데이터를 동시에 로드
      final weekStartDate = DateFormatter.daysAgoYyMMdd(TrendPeriod.week.days);
      final monthStartDate = DateFormatter.daysAgoYyMMdd(TrendPeriod.month.days);

      // 모든 스테이지의 1주일 데이터 병렬 조회
      final weekFutures = StageConstants.kcEnabledStages.map((stage) async {
        try {
          final result = await dashboardRepository.getStageKcMasteryTrend(
            stage,
            weekStartDate,
            endDate,
          );
          return MapEntry(stage, result.dataOrNull);
        } catch (e) {
          return MapEntry<String, StageKcMasteryTrendResponse?>(stage, null);
        }
      }).toList();

      // 모든 스테이지의 1개월 데이터 병렬 조회
      final monthFutures = StageConstants.kcEnabledStages.map((stage) async {
        try {
          final result = await dashboardRepository.getStageKcMasteryTrend(
            stage,
            monthStartDate,
            endDate,
          );
          return MapEntry(stage, result.dataOrNull);
        } catch (e) {
          return MapEntry<String, StageKcMasteryTrendResponse?>(stage, null);
        }
      }).toList();

      // 1주일과 1개월 데이터를 동시에 기다림
      final results = await Future.wait([
        Future.wait(weekFutures),
        Future.wait(monthFutures),
      ]);

      final weekResults = results[0];
      final monthResults = results[1];

      // 1주일 데이터 맵 생성
      final weekTrendMap = <String, StageKcMasteryTrendResponse>{};
      for (final entry in weekResults) {
        if (entry.value != null) {
          weekTrendMap[entry.key] = entry.value!;
        }
      }

      // 1개월 데이터 맵 생성
      final monthTrendMap = <String, StageKcMasteryTrendResponse>{};
      for (final entry in monthResults) {
        if (entry.value != null) {
          monthTrendMap[entry.key] = entry.value!;
        }
      }

      // 현재 선택된 기간에 맞는 데이터를 trendData에 설정
      final currentTrendData = state.period == TrendPeriod.week
          ? weekTrendMap
          : monthTrendMap;

      state = state.copyWith(
        isLoading: false,
        weekTrendData: weekTrendMap,
        monthTrendData: monthTrendMap,
        trendData: currentTrendData,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('학습 추이 데이터를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 기간 변경 (캐시된 데이터 사용)
  void setPeriod(TrendPeriod period) {
    if (state.period == period) return;

    // 캐시된 데이터에서 즉시 전환
    final newTrendData = period == TrendPeriod.week
        ? state.weekTrendData
        : state.monthTrendData;

    state = state.copyWith(
      period: period,
      trendData: newTrendData,
    );
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadAllTrendData();
  }
}

/// 학습 추이 Provider
final learningTrendProvider =
    StateNotifierProvider<LearningTrendNotifier, LearningTrendState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return LearningTrendNotifier(dashboardRepository);
});
