import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../data/models/stage_info_response.dart';
import '../../data/models/stage_try_avg_response.dart';
import '../../data/models/stage_correct_rate_response.dart';
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
      print('=== Analysis Data Load Start ===');
      print('Selected Stage: ${state.selectedStage}');

      // 실제 API 호출
      final stageInfo = await dashboardRepository.getStageInfo(state.selectedStage);
      print('StageInfo: $stageInfo');

      final tryAvg = await dashboardRepository.getStageTryAvg(state.selectedStage);
      print('TryAvg: $tryAvg (averageTryCount: ${tryAvg?.averageTryCount})');

      final correctRate = await dashboardRepository.getStageCorrectRate(state.selectedStage);
      print('CorrectRate: $correctRate (correctRate: ${correctRate?.correctRate})');

      final weakPhonemes = await dashboardRepository.getWrongPhonemesRank(5);
      print('WeakPhonemes: ${weakPhonemes?.length ?? 0} items');
      if (weakPhonemes != null && weakPhonemes.isNotEmpty) {
        print('First weak phoneme: ${weakPhonemes.first.value}, wrongCnt: ${weakPhonemes.first.wrongCnt}');
      }

      final practicedPhonemes = await dashboardRepository.getTryPhonemesRank(5);
      print('PracticedPhonemes: ${practicedPhonemes?.length ?? 0} items');
      if (practicedPhonemes != null && practicedPhonemes.isNotEmpty) {
        print('First practiced phoneme: ${practicedPhonemes.first.value}, tryCnt: ${practicedPhonemes.first.tryCnt}');
      }

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfo,
        tryAvg: tryAvg,
        correctRate: correctRate,
        weakPhonemes: weakPhonemes,
        practicedPhonemes: practicedPhonemes,
      );
      print('=== Analysis Data Load Complete ===');
    } catch (e, stackTrace) {
      print('=== Analysis Data Load Error ===');
      print('Error: $e');
      print('StackTrace: $stackTrace');
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

      state = state.copyWith(
        isLoading: false,
        stageInfo: stageInfo,
        tryAvg: tryAvg,
        correctRate: correctRate,
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
