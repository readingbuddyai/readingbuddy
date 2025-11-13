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

  LearningTrendState({
    this.isLoading = false,
    this.error,
    this.period = TrendPeriod.week,
    this.trendData = const {},
  });

  LearningTrendState copyWith({
    bool? isLoading,
    ErrorState? error,
    bool? clearError,
    TrendPeriod? period,
    Map<String, StageKcMasteryTrendResponse>? trendData,
  }) {
    return LearningTrendState(
      isLoading: isLoading ?? this.isLoading,
      error: clearError == true ? null : (error ?? this.error),
      period: period ?? this.period,
      trendData: trendData ?? this.trendData,
    );
  }
}

/// 학습 추이 Provider
class LearningTrendNotifier extends StateNotifier<LearningTrendState> {
  final DashboardRepository dashboardRepository;

  LearningTrendNotifier(this.dashboardRepository)
      : super(LearningTrendState()) {
    _loadTrendData();
  }

  /// 추이 데이터 로드
  Future<void> _loadTrendData() async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      // 날짜 계산
      final endDate = DateFormatter.todayYyMMdd();
      final startDate = DateFormatter.daysAgoYyMMdd(state.period.days);

      // 모든 스테이지 데이터 병렬 조회
      final futures = StageConstants.kcEnabledStages.map((stage) async {
        try {
          final result = await dashboardRepository.getStageKcMasteryTrend(
            stage,
            startDate,
            endDate,
          );
          return MapEntry(stage, result.dataOrNull);
        } catch (e) {
          return MapEntry<String, StageKcMasteryTrendResponse?>(stage, null);
        }
      }).toList();

      final results = await Future.wait(futures);

      // null이 아닌 데이터만 맵에 추가
      final trendMap = <String, StageKcMasteryTrendResponse>{};
      for (final entry in results) {
        if (entry.value != null) {
          trendMap[entry.key] = entry.value!;
        }
      }

      state = state.copyWith(
        isLoading: false,
        trendData: trendMap,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('학습 추이 데이터를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 기간 변경
  Future<void> setPeriod(TrendPeriod period) async {
    if (state.period == period) return;

    state = state.copyWith(period: period);
    await _loadTrendData();
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadTrendData();
  }
}

/// 학습 추이 Provider
final learningTrendProvider =
    StateNotifierProvider<LearningTrendNotifier, LearningTrendState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return LearningTrendNotifier(dashboardRepository);
});
