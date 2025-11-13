import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/utils/error_state.dart';
import '../../../../core/constants/stage_constants.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../data/models/stage_info_response.dart';
import '../../data/models/stage_try_avg_response.dart';
import '../../data/models/stage_correct_rate_response.dart';
import '../../data/models/stage_mastery_response.dart';
import '../../data/models/phoneme_rank_response.dart';

/// 학습 분석 화면 상태
class AnalysisState {
  final bool isLoading;
  final ErrorState? error;
  final String selectedStage;

  // 스테이지 통계
  final StageInfoResponse? stageInfo;
  final StageTryAvgResponse? tryAvg;
  final StageCorrectRateResponse? correctRate;
  final StageMasteryResponse? mastery;

  // 음소 랭킹
  final List<PhonemeRankResponse>? weakPhonemes;
  final List<PhonemeRankResponse>? practicedPhonemes;

  AnalysisState({
    this.isLoading = false,
    this.error,
    this.selectedStage = '1.1.1',
    this.stageInfo,
    this.tryAvg,
    this.correctRate,
    this.mastery,
    this.weakPhonemes,
    this.practicedPhonemes,
  });

  AnalysisState copyWith({
    bool? isLoading,
    ErrorState? error,
    bool? clearError,
    String? selectedStage,
    StageInfoResponse? stageInfo,
    StageTryAvgResponse? tryAvg,
    StageCorrectRateResponse? correctRate,
    StageMasteryResponse? mastery,
    List<PhonemeRankResponse>? weakPhonemes,
    List<PhonemeRankResponse>? practicedPhonemes,
  }) {
    return AnalysisState(
      isLoading: isLoading ?? this.isLoading,
      error: clearError == true ? null : (error ?? this.error),
      selectedStage: selectedStage ?? this.selectedStage,
      stageInfo: stageInfo ?? this.stageInfo,
      tryAvg: tryAvg ?? this.tryAvg,
      correctRate: correctRate ?? this.correctRate,
      mastery: mastery ?? this.mastery,
      weakPhonemes: weakPhonemes ?? this.weakPhonemes,
      practicedPhonemes: practicedPhonemes ?? this.practicedPhonemes,
    );
  }
}

/// 학습 분석 Provider
class AnalysisNotifier extends StateNotifier<AnalysisState> {
  final DashboardRepository dashboardRepository;

  AnalysisNotifier(this.dashboardRepository) : super(AnalysisState()) {
    _loadAnalysisData();
  }

  /// 학습 분석 데이터 로드
  Future<void> _loadAnalysisData() async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      debugPrint('=== Analysis Data Load Start ===');
      debugPrint('Selected Stage: ${state.selectedStage}');

      // 실제 API 호출
      final stageInfoResult = await dashboardRepository.getStageInfo(state.selectedStage);
      debugPrint('StageInfo loaded');

      final tryAvgResult = await dashboardRepository.getStageTryAvg(state.selectedStage);
      debugPrint('TryAvg loaded');

      final correctRateResult = await dashboardRepository.getStageCorrectRate(state.selectedStage);
      debugPrint('CorrectRate loaded');

      final weakPhonemesResult = await dashboardRepository.getWrongPhonemesRank(5);
      debugPrint('WeakPhonemes loaded');

      final practicedPhonemesResult = await dashboardRepository.getTryPhonemesRank(5);
      debugPrint('PracticedPhonemes loaded');

      // KC가 있는 스테이지만 mastery 조회
      StageMasteryResponse? mastery;
      if (StageConstants.kcEnabledStages.contains(state.selectedStage)) {
        final masteryResult = await dashboardRepository.getStageMastery(state.selectedStage);
        mastery = masteryResult.isSuccess ? masteryResult.dataOrNull : null;
        debugPrint('Mastery loaded');
      } else {
        debugPrint('Mastery skipped: Stage ${state.selectedStage} has no KC');
      }

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfoResult.dataOrNull,
        tryAvg: tryAvgResult.dataOrNull,
        correctRate: correctRateResult.dataOrNull,
        mastery: mastery,
        weakPhonemes: weakPhonemesResult.dataOrNull,
        practicedPhonemes: practicedPhonemesResult.dataOrNull,
      );
      debugPrint('=== Analysis Data Load Complete ===');
    } catch (e, stackTrace) {
      debugPrint('=== Analysis Data Load Error ===');
      debugPrint('Error: $e');
      debugPrint('StackTrace: $stackTrace');
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('데이터를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 스테이지 선택 변경
  Future<void> selectStage(String stage) async {
    if (state.selectedStage == stage) return;

    state = state.copyWith(selectedStage: stage, isLoading: true, clearError: true);

    try {
      // 실제 API 호출
      final stageInfoResult = await dashboardRepository.getStageInfo(stage);
      final tryAvgResult = await dashboardRepository.getStageTryAvg(stage);
      final correctRateResult = await dashboardRepository.getStageCorrectRate(stage);

      // KC가 있는 스테이지만 mastery 조회
      StageMasteryResponse? mastery;
      if (StageConstants.kcEnabledStages.contains(stage)) {
        final masteryResult = await dashboardRepository.getStageMastery(stage);
        mastery = masteryResult.dataOrNull;
      }

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfoResult.dataOrNull,
        tryAvg: tryAvgResult.dataOrNull,
        correctRate: correctRateResult.dataOrNull,
        mastery: mastery,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('스테이지 데이터를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadAnalysisData();
  }
}

/// 학습 분석 Provider
final analysisProvider =
    StateNotifierProvider<AnalysisNotifier, AnalysisState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return AnalysisNotifier(dashboardRepository);
});
