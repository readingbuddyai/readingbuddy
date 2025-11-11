import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
  final String? errorMessage;
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
    this.errorMessage,
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
    String? errorMessage,
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
      errorMessage: errorMessage,
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
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      debugPrint('=== Analysis Data Load Start ===');
      debugPrint('Selected Stage: ${state.selectedStage}');

      // 실제 API 호출
      final stageInfo = await dashboardRepository.getStageInfo(state.selectedStage);
      debugPrint('StageInfo loaded');

      final tryAvg = await dashboardRepository.getStageTryAvg(state.selectedStage);
      debugPrint('TryAvg loaded: ${tryAvg?.averageTryCount}');

      final correctRate = await dashboardRepository.getStageCorrectRate(state.selectedStage);
      debugPrint('CorrectRate loaded: ${correctRate?.correctRate}');

      final weakPhonemes = await dashboardRepository.getWrongPhonemesRank(5);
      debugPrint('WeakPhonemes loaded: ${weakPhonemes.length} items');

      final practicedPhonemes = await dashboardRepository.getTryPhonemesRank(5);
      debugPrint('PracticedPhonemes loaded: ${practicedPhonemes.length} items');

      final mastery = await dashboardRepository.getStageMastery(state.selectedStage);
      debugPrint('Mastery loaded: ${mastery?.averageMastery}');

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfo,
        tryAvg: tryAvg,
        correctRate: correctRate,
        mastery: mastery,
        weakPhonemes: weakPhonemes,
        practicedPhonemes: practicedPhonemes,
      );
      debugPrint('=== Analysis Data Load Complete ===');
    } catch (e, stackTrace) {
      debugPrint('=== Analysis Data Load Error ===');
      debugPrint('Error: $e');
      debugPrint('StackTrace: $stackTrace');
      state = state.copyWith(
        isLoading: false,
        errorMessage: '데이터를 불러오는데 실패했습니다.',
      );
    }
  }

  /// 스테이지 선택 변경
  Future<void> selectStage(String stage) async {
    if (state.selectedStage == stage) return;

    state = state.copyWith(selectedStage: stage, isLoading: true);

    try {
      // 실제 API 호출
      final stageInfo = await dashboardRepository.getStageInfo(stage);
      final tryAvg = await dashboardRepository.getStageTryAvg(stage);
      final correctRate = await dashboardRepository.getStageCorrectRate(stage);
      final mastery = await dashboardRepository.getStageMastery(stage);

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfo,
        tryAvg: tryAvg,
        correctRate: correctRate,
        mastery: mastery,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: '스테이지 데이터를 불러오는데 실패했습니다.',
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
