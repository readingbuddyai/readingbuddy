import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:dropdown_button2/dropdown_button2.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/constants/stage_constants.dart';
import '../providers/analysis_provider.dart';
import '../providers/learning_trend_provider.dart';
import '../../data/models/stage_kc_mastery_trend_response.dart';

/// KC 레이블 생성 (Stage별 다른 로직 적용)
String _getKcLabel(KcTrend kcTrend, String stage) {
  final isStage4 = stage == '4.1' || stage == '4.2';

  if (isStage4) {
    // 4단계: kcDescription 그대로 사용 (예: "초성 분절 평균", "중성 분절 평균")
    return kcTrend.kcDescription ?? kcTrend.kcCategory ?? 'KC';
  } else {
    // 1~3단계: 기존 로직 (kcDescription + 숫자)
    String label = kcTrend.kcDescription ?? 'KC';

    // kcCategory에서 숫자 추출 (예: MONOPHTHONG_2 -> 2)
    if (kcTrend.kcCategory != null) {
      final categoryParts = kcTrend.kcCategory!.split('_');
      if (categoryParts.length > 1) {
        final number = categoryParts.last;
        label = '${label}_$number';
      }
    }

    return label;
  }
}

/// KC 카테고리/설명 영어 -> 한글 변환
String _getKcLabelKorean(String? text) {
  if (text == null || text.isEmpty) return '';

  final categoryMap = {
    'MONOPHTHONG': '단모음',
    'DIPHTHONG': '이중모음',
    'CONSONANT': '자음',
    'STOP': '파열음',
    'FRICATIVE': '마찰음',
    'AFFRICATE': '파찰음',
    'NASAL': '비음',
    'LIQUID': '유음',
    'ASPIRATED': '격음',
    'TENSED': '경음',
    'PLAIN': '평음',
  };

  // "MONOPHTHONG_1" 같은 패턴 처리
  final parts = text.split('_');
  if (parts.isNotEmpty) {
    final mainCategory = categoryMap[parts[0].toUpperCase()] ?? parts[0];
    if (parts.length > 1) {
      // "단모음_1" 형태로 반환
      return '${mainCategory}_${parts.sublist(1).join('_')}';
    }
    return mainCategory;
  }

  return categoryMap[text.toUpperCase()] ?? text;
}

class AnalysisScreen extends ConsumerStatefulWidget {
  const AnalysisScreen({super.key});

  @override
  ConsumerState<AnalysisScreen> createState() => _AnalysisScreenState();
}

class _AnalysisScreenState extends ConsumerState<AnalysisScreen> {
  String? _selectedKcCategory; // 선택된 KC 카테고리 (kcId 대신 kcCategory 사용)
  String? _previousStage; // 이전 스테이지 (스테이지 변경 감지용)

  @override
  Widget build(BuildContext context) {
    final analysisState = ref.watch(analysisProvider);
    final theme = Theme.of(context);

    // 스테이지가 변경되면 KC 선택 초기화 (첫 번째 KC로 자동 선택됨)
    if (_previousStage != analysisState.selectedStage) {
      _selectedKcCategory = null;
      _previousStage = analysisState.selectedStage;
    }

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => ref.read(analysisProvider.notifier).refresh(),
          child: CustomScrollView(
            slivers: [
              // 상단 헤더
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(
                    '학습 분석',
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),

              // 스테이지 선택 드롭다운
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
                  child: _buildStageDropdown(context, ref, analysisState),
                ),
              ),

              // 스테이지 숙련도 및 통계
              if (analysisState.mastery != null ||
                  analysisState.tryAvg != null ||
                  analysisState.correctRate != null)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Card(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 24.0,
                          vertical: 32.0,
                        ),
                        child: Row(
                          children: [
                            // 좌측: 원형 차트 (숙련도) - 크게!
                            if (analysisState.mastery != null)
                              MasteryCircularChart(
                                percentage: analysisState.mastery!.masteryPercent,
                                label: '숙련도',
                                size: 180,
                                strokeWidth: 14,
                              )
                            else
                              MasteryCircularChart(
                                percentage: 0,
                                label: '숙련도',
                                size: 180,
                                strokeWidth: 14,
                              ),

                            const SizedBox(width: 32),

                            // 우측: 3개 통계
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  // 1. 전체 문제 수
                                  _buildStatItem(
                                    icon: Icons.quiz,
                                    label: '전체 문제 수',
                                    value: _getTotalProblemCount(analysisState),
                                    color: theme.colorScheme.primary,
                                    theme: theme,
                                  ),
                                  const SizedBox(height: 20),

                                  // 2. 정답률
                                  _buildStatItem(
                                    icon: Icons.check_circle,
                                    label: '정답률',
                                    value: _getCorrectRate(analysisState),
                                    color: AppTheme.getScoreColor(
                                      _getCorrectRateValue(analysisState),
                                    ),
                                    theme: theme,
                                  ),
                                  const SizedBox(height: 20),

                                  // 3. 숙련된 음소
                                  _buildStatItem(
                                    icon: Icons.psychology,
                                    label: '숙련된 음소',
                                    value: _getMasteredKcCount(ref, analysisState),
                                    color: AppTheme.successColor,
                                    theme: theme,
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),

              const SliverToBoxAdapter(child: SizedBox(height: 16)),

              // KC 숙련도 추이 섹션
              const SliverToBoxAdapter(
                child: SectionHeader(
                  title: '숙련도 그래프',
                  subtitle: '스테이지별 숙련도 학습 변화',
                ),
              ),

              // 기간 선택 토글
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Row(
                    children: [
                      Expanded(
                        child: _buildPeriodButton(
                          context,
                          ref,
                          TrendPeriod.week,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _buildPeriodButton(
                          context,
                          ref,
                          TrendPeriod.month,
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              const SliverToBoxAdapter(child: SizedBox(height: 16)),

              // KC 선택 토글
              ..._buildKcSelector(context, ref, theme, analysisState.selectedStage),

              const SliverToBoxAdapter(child: SizedBox(height: 16)),

              // KC 추이 그래프 (현재 선택된 스테이지만)
              ..._buildTrendCharts(context, ref, theme, analysisState.selectedStage),

              // 하단 여백
              const SliverToBoxAdapter(child: SizedBox(height: 24)),
            ],
          ),
        ),
      ),
    );
  }

  /// 스테이지 선택 드롭다운
  Widget _buildStageDropdown(
      BuildContext context, WidgetRef ref, AnalysisState state) {
    final stages = StageConstants.allStages;
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: theme.colorScheme.outline.withOpacity(0.3),
          width: 1,
        ),
      ),
      child: Row(
        children: [
          Icon(
            Icons.filter_list,
            size: 20,
            color: theme.colorScheme.primary,
          ),
          const SizedBox(width: 12),
          Text(
            '스테이지',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurface.withOpacity(0.7),
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: DropdownButton2<String>(
              value: state.selectedStage,
              isExpanded: true,
              underline: const SizedBox(),
              items: stages.map((stageConfig) {
                return DropdownMenuItem<String>(
                  value: stageConfig.id,
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.primary.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          stageConfig.category,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: theme.colorScheme.primary,
                            fontSize: 11,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(stageConfig.displayName),
                    ],
                  ),
                );
              }).toList(),
              onChanged: (value) {
                if (value != null) {
                  ref.read(analysisProvider.notifier).selectStage(value);
                }
              },
              buttonStyleData: ButtonStyleData(
                height: 50,
                padding: const EdgeInsets.symmetric(horizontal: 0),
                decoration: BoxDecoration(
                  color: Colors.transparent,
                ),
              ),
              iconStyleData: IconStyleData(
                icon: Icon(
                  Icons.keyboard_arrow_down,
                  color: theme.colorScheme.primary,
                ),
                iconSize: 24,
              ),
              dropdownStyleData: DropdownStyleData(
                maxHeight: 400,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  color: theme.colorScheme.surface,
                ),
                offset: const Offset(0, -5),
                scrollbarTheme: ScrollbarThemeData(
                  radius: const Radius.circular(40),
                  thickness: WidgetStateProperty.all(6),
                  thumbVisibility: WidgetStateProperty.all(true),
                ),
              ),
              menuItemStyleData: MenuItemStyleData(
                height: 56,
                padding: const EdgeInsets.symmetric(horizontal: 16),
              ),
            ),
          ),
        ],
      ),
    );
  }

  /// 통계 행
  Widget _buildStatRow(String label, String value, ThemeData theme,
      {Color? color}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurface.withOpacity(0.7),
          ),
        ),
        Text(
          value,
          style: theme.textTheme.bodyLarge?.copyWith(
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
      ],
    );
  }

  /// 통계 아이템 (아이콘 + 레이블 + 값)
  Widget _buildStatItem({
    required IconData icon,
    required String label,
    required String value,
    required Color color,
    required ThemeData theme,
  }) {
    return Row(
      children: [
        Icon(
          icon,
          size: 20,
          color: color,
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                  fontSize: 11,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: color,
                  fontSize: 15,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ],
    );
  }

  /// 전체 문제 수
  String _getTotalProblemCount(AnalysisState state) {
    if (state.stageInfo == null) return '-';
    return '${state.stageInfo!.totalProblemCount ?? 0}개';
  }

  /// 정답률
  String _getCorrectRate(AnalysisState state) {
    if (state.stageInfo == null) return '-';
    final correctRate = state.stageInfo!.correctRate;
    if (correctRate == null) return '-';
    return '${correctRate.toStringAsFixed(1)}%';
  }

  /// 정답률 숫자 값 (색상 계산용)
  double _getCorrectRateValue(AnalysisState state) {
    if (state.stageInfo == null) return 0.0;
    return state.stageInfo!.correctRate ?? 0.0;
  }

  /// 숙련된 음소 개수 계산 (mastery >= 70%)
  /// 현재 선택된 스테이지의 KC만 계산
  String _getMasteredKcCount(WidgetRef ref, AnalysisState state) {
    final selectedStage = state.selectedStage;

    // KC가 없는 스테이지는 '-' 반환
    if (!StageConstants.kcEnabledStages.contains(selectedStage)) {
      return '-';
    }

    final trendState = ref.watch(learningTrendProvider);
    final trendData = trendState.trendData[selectedStage];
    final kcTrends = trendData?.kcTrends;

    // 로딩 중이거나 데이터가 없으면 '-'
    if (trendState.isLoading || kcTrends == null || kcTrends.isEmpty) {
      return '-';
    }

    final totalCount = kcTrends.length;

    // 각 KC의 최신 pLearn >= 0.7인 개수 계산
    final masteredCount = kcTrends.where((kcTrend) {
      final masteryTrend = kcTrend.masteryTrend;
      if (masteryTrend == null || masteryTrend.isEmpty) return false;

      // 최신 데이터 (마지막 항목)의 pLearn 확인
      final latestMastery = masteryTrend.last;
      final pLearnValue = latestMastery.pLearn ?? 0.0;

      return pLearnValue >= 0.7;
    }).length;

    return '$masteredCount개 / $totalCount개';
  }

  /// 기간 선택 버튼
  Widget _buildPeriodButton(
    BuildContext context,
    WidgetRef ref,
    TrendPeriod period,
  ) {
    final theme = Theme.of(context);
    final trendState = ref.watch(learningTrendProvider);
    final isSelected = trendState.period == period;

    return InkWell(
      onTap: () {
        ref.read(learningTrendProvider.notifier).setPeriod(period);
      },
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12),
        decoration: BoxDecoration(
          color: isSelected
              ? theme.colorScheme.primary
              : theme.colorScheme.surface,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: isSelected
                ? theme.colorScheme.primary
                : theme.colorScheme.outline.withOpacity(0.3),
          ),
        ),
        child: Center(
          child: Text(
            period.label,
            style: theme.textTheme.titleSmall?.copyWith(
              color: isSelected
                  ? theme.colorScheme.onPrimary
                  : theme.colorScheme.onSurface,
              fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ),
      ),
    );
  }

  /// KC 선택 드롭다운 빌드
  List<Widget> _buildKcSelector(
    BuildContext context,
    WidgetRef ref,
    ThemeData theme,
    String selectedStage,
  ) {
    final trendState = ref.watch(learningTrendProvider);

    // KC 추이 대상이 아니거나 데이터가 없으면 표시 안 함
    if (!StageConstants.kcEnabledStages.contains(selectedStage)) {
      return [];
    }

    final trendData = trendState.trendData[selectedStage];
    final kcTrends = trendData?.kcTrends;

    if (kcTrends == null || kcTrends.isEmpty) {
      return [];
    }

    // 첫 번째 KC가 선택되어 있지 않으면 첫 번째로 설정
    if (_selectedKcCategory == null || !kcTrends.any((kc) => kc.kcCategory == _selectedKcCategory)) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          setState(() {
            _selectedKcCategory = kcTrends.first.kcCategory;
          });
        }
      });
    }

    return [
      SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Container(
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(12),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            child: DropdownButtonHideUnderline(
              child: DropdownButton2<String>(
                isExpanded: true,
                value: _selectedKcCategory ?? kcTrends.first.kcCategory,
                items: kcTrends.map((kcTrend) {
                  // 레이블 생성 로직 개선
                  String label = _getKcLabel(kcTrend, trendData?.stage ?? '');

                  return DropdownMenuItem<String>(
                    value: kcTrend.kcCategory,
                    child: Text(
                      label,
                      style: theme.textTheme.bodyLarge?.copyWith(
                        color: theme.colorScheme.onSurface,
                      ),
                    ),
                  );
                }).toList(),
                onChanged: (newKcCategory) {
                  setState(() {
                    _selectedKcCategory = newKcCategory;
                  });
                },
                buttonStyleData: ButtonStyleData(
                  height: 50,
                  padding: const EdgeInsets.symmetric(horizontal: 0),
                  decoration: BoxDecoration(
                    color: Colors.transparent,
                  ),
                ),
                iconStyleData: IconStyleData(
                  icon: Icon(
                    Icons.arrow_drop_down,
                    color: theme.colorScheme.primary,
                  ),
                  iconSize: 24,
                ),
                dropdownStyleData: DropdownStyleData(
                  maxHeight: 300,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(12),
                    color: theme.colorScheme.surfaceContainerHighest,
                  ),
                  offset: const Offset(0, -5),
                  scrollbarTheme: ScrollbarThemeData(
                    radius: const Radius.circular(40),
                    thickness: WidgetStateProperty.all(6),
                    thumbVisibility: WidgetStateProperty.all(true),
                  ),
                ),
                menuItemStyleData: MenuItemStyleData(
                  height: 48,
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                ),
              ),
            ),
          ),
        ),
      ),
    ];
  }

  /// KC 추이 그래프 리스트 빌드 (현재 선택된 스테이지만)
  List<Widget> _buildTrendCharts(
    BuildContext context,
    WidgetRef ref,
    ThemeData theme,
    String selectedStage,
  ) {
    final trendState = ref.watch(learningTrendProvider);

    // 선택된 스테이지가 KC 추이 대상이 아니면 표시 안 함
    if (!StageConstants.kcEnabledStages.contains(selectedStage)) {
      return [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Center(
              child: Text(
                '이 스테이지는 KC 추이 데이터가 제공되지 않습니다',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.5),
                ),
              ),
            ),
          ),
        ),
      ];
    }

    if (trendState.isLoading) {
      return [
        const SliverToBoxAdapter(
          child: Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: CircularProgressIndicator(),
            ),
          ),
        ),
      ];
    }

    if (trendState.error != null) {
      return [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Center(
              child: Text(
                trendState.error!.message,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.error,
                ),
              ),
            ),
          ),
        ),
      ];
    }

    // 현재 선택된 스테이지의 KC별 차트 표시
    final trendData = trendState.trendData[selectedStage];
    final kcTrends = trendData?.kcTrends;

    if (trendData == null || kcTrends == null || kcTrends.isEmpty) {
      return [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Center(
              child: Text(
                '이 기간에 학습 데이터가 없습니다',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.5),
                ),
              ),
            ),
          ),
        ),
      ];
    }

    // 선택된 KC만 필터링
    final filteredKcTrends = _selectedKcCategory == null
        ? kcTrends
        : kcTrends.where((kc) => kc.kcCategory == _selectedKcCategory).toList();

    if (filteredKcTrends.isEmpty) {
      return [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Center(
              child: Text(
                '선택한 KC의 데이터가 없습니다',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.5),
                ),
              ),
            ),
          ),
        ),
      ];
    }

    // KC마다 별도의 차트 카드 생성
    return filteredKcTrends.map<Widget>((kcTrend) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: _buildKcChart(context, theme, kcTrend),
        ),
      );
    }).toList();
  }

  /// KC별 차트 카드
  Widget _buildKcChart(
    BuildContext context,
    ThemeData theme,
    dynamic kcTrend,
  ) {
    // 레이블 생성: kcDescription (한글) + kcCategory의 숫자 부분
    String labelKorean = kcTrend.kcDescription ?? 'KC';

    // kcCategory에서 숫자 추출 (예: MONOPHTHONG_2 -> 2)
    if (kcTrend.kcCategory != null) {
      final categoryParts = kcTrend.kcCategory!.split('_');
      if (categoryParts.length > 1) {
        final number = categoryParts.last;
        labelKorean = '${labelKorean}_$number';
      }
    }

    // 디버깅
    debugPrint('KC: $labelKorean, Trend Count: ${kcTrend.masteryTrend?.length ?? 0}');

    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // KC 제목
            Row(
              children: [
                Icon(
                  Icons.timeline,
                  color: theme.colorScheme.primary,
                  size: 20,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    labelKorean,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // 차트 또는 빈 상태
            if (kcTrend.masteryTrend == null || kcTrend.masteryTrend.isEmpty)
              SizedBox(
                height: 200,
                child: Center(
                  child: Text(
                    '이 기간에 학습 데이터가 없습니다',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurface.withOpacity(0.5),
                    ),
                  ),
                ),
              )
            else
              SizedBox(
                height: 250,
                child: _buildLineChart(context, theme, kcTrend),
              ),
          ],
        ),
      ),
    );
  }

  /// 라인 차트 빌드 (단일 KC의 전체 데이터)
  Widget _buildLineChart(
    BuildContext context,
    ThemeData theme,
    dynamic kcTrend,
  ) {
    final trendState = ref.watch(learningTrendProvider);
    final period = trendState.period;

    debugPrint('=== 📊 그래프 생성 시작 ===');
    debugPrint('선택된 기간: ${period.label} (${period.days}일)');
    debugPrint('masteryTrend 데이터 개수: ${kcTrend?.masteryTrend?.length ?? 0}');

    // 날짜별로 데이터 그룹화 (하루에 여러 세션 → 마지막 값 사용)
    final dateMap = <String, MasteryPoint>{};

    // 1. initialMastery를 먼저 추가 (가장 오래된 데이터)
    if (kcTrend?.initialMastery != null && kcTrend.initialMastery!.updatedAt != null && kcTrend.initialMastery!.pLearn != null) {
      final initialDate = kcTrend.initialMastery!.updatedAt!;
      final initialDateKey = '${initialDate.year}-${initialDate.month.toString().padLeft(2, '0')}-${initialDate.day.toString().padLeft(2, '0')}';
      dateMap[initialDateKey] = kcTrend.initialMastery!;
      debugPrint('📍 Initial Mastery: $initialDateKey = ${(kcTrend.initialMastery!.pLearn! * 100).toStringAsFixed(1)}%');
    }

    // 2. masteryTrend 데이터 추가
    final masteryTrend = kcTrend?.masteryTrend;
    if (masteryTrend != null && masteryTrend.isNotEmpty) {
      for (final point in masteryTrend) {
        if (point.updatedAt != null && point.pLearn != null) {
          final dateKey = '${point.updatedAt!.year}-${point.updatedAt!.month.toString().padLeft(2, '0')}-${point.updatedAt!.day.toString().padLeft(2, '0')}';
          dateMap[dateKey] = point;
          debugPrint('데이터: $dateKey = ${(point.pLearn! * 100).toStringAsFixed(1)}%');
        }
      }
    }

    debugPrint('총 날짜별 데이터: ${dateMap.length}개');

    // 기간에 따라 고정된 X축 생성
    final now = DateTime.now();
    final spots = <FlSpot>[];
    final dateLabels = <int, String>{};

    debugPrint('현재 시간: ${now.year}-${now.month}-${now.day} ${now.hour}:${now.minute}');

    // dateMap의 날짜들을 정렬 (오래된 순)
    final sortedDates = dateMap.keys.toList()..sort();

    // 특정 날짜 이전의 가장 가까운 데이터 찾기 헬퍼
    double? findClosestPreviousValue(DateTime targetDate) {
      final targetDateKey = '${targetDate.year}-${targetDate.month.toString().padLeft(2, '0')}-${targetDate.day.toString().padLeft(2, '0')}';

      // 역순으로 순회 (최신 → 과거)
      for (int i = sortedDates.length - 1; i >= 0; i--) {
        final dateKey = sortedDates[i];

        // targetDate와 같거나 이전 날짜면 반환
        if (dateKey.compareTo(targetDateKey) <= 0) {
          return dateMap[dateKey]?.pLearn;
        }
      }
      return null;
    }

    if (period == TrendPeriod.week) {
      debugPrint('✅ 1주일 모드: X축 7개 고정');
      // 1주일: 항상 7개 (최근 7일)
      for (int i = 0; i < 7; i++) {
        final date = now.subtract(Duration(days: 6 - i));
        final dateKey = '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

        final point = dateMap[dateKey];
        double yValue;

        if (point?.pLearn != null) {
          // 해당 날짜 데이터가 있으면 사용
          yValue = point!.pLearn! * 100;
        } else {
          // 데이터 없으면 이전 가장 가까운 날짜의 값 찾기
          final previousValue = findClosestPreviousValue(date);
          if (previousValue != null) {
            yValue = previousValue * 100;
          } else {
            // 이전 데이터도 없으면 직전 spot의 값 사용
            yValue = i > 0 && spots.isNotEmpty ? spots.last.y : 0.0;
          }
        }

        spots.add(FlSpot(i.toDouble(), yValue));
        dateLabels[i] = '${date.month}/${date.day}';

        debugPrint('[$i] ${date.month}/${date.day} ($dateKey) = ${yValue.toStringAsFixed(1)}% ${point != null ? "✅" : "❌(이전값)"}');
      }
    } else {
      debugPrint('✅ 1개월 모드: X축 7개 구간');
      // 1개월: 7개 구간 (약 4-5일씩 묶음)
      final daysPerSegment = (period.days / 7).ceil();
      debugPrint('구간당 일수: $daysPerSegment일');

      for (int i = 0; i < 7; i++) {
        final segmentEnd = now.subtract(Duration(days: (6 - i) * daysPerSegment));
        final segmentStart = now.subtract(Duration(days: (7 - i) * daysPerSegment));

        debugPrint('[$i] 구간: ${segmentStart.month}/${segmentStart.day} ~ ${segmentEnd.month}/${segmentEnd.day}');

        // 해당 구간의 평균값 또는 마지막 값 계산
        double segmentValue;
        int count = 0;
        double sum = 0;

        for (int day = 0; day < daysPerSegment; day++) {
          final checkDate = segmentStart.add(Duration(days: day));
          final dateKey = '${checkDate.year}-${checkDate.month.toString().padLeft(2, '0')}-${checkDate.day.toString().padLeft(2, '0')}';

          final point = dateMap[dateKey];
          if (point?.pLearn != null) {
            sum += point!.pLearn! * 100;
            count++;
            debugPrint('  - ${checkDate.month}/${checkDate.day}: ${(point.pLearn! * 100).toStringAsFixed(1)}%');
          }
        }

        if (count > 0) {
          segmentValue = sum / count;
          debugPrint('  ✅ 평균: ${segmentValue.toStringAsFixed(1)}% (${count}개 데이터)');
        } else {
          // 해당 구간에 데이터 없으면 이전 가장 가까운 날짜의 값 찾기
          final previousValue = findClosestPreviousValue(segmentEnd);
          if (previousValue != null) {
            segmentValue = previousValue * 100;
            debugPrint('  ❌ 데이터 없음 → 이전 dateMap값: ${segmentValue.toStringAsFixed(1)}%');
          } else {
            // 이전 데이터도 없으면 직전 spot의 값 사용
            segmentValue = i > 0 && spots.isNotEmpty ? spots.last.y : 0.0;
            debugPrint('  ❌ 데이터 없음 → 이전 spot값: ${segmentValue.toStringAsFixed(1)}%');
          }
        }

        spots.add(FlSpot(i.toDouble(), segmentValue));
        dateLabels[i] = '${segmentEnd.month}/${segmentEnd.day}';
      }
    }

    debugPrint('최종 spots 개수: ${spots.length}');
    debugPrint('최종 dateLabels: $dateLabels');
    debugPrint('=== 📊 그래프 생성 완료 ===\n');

    if (spots.isEmpty) {
      return Center(
        child: Text(
          'pLearn 데이터가 없습니다',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurface.withOpacity(0.5),
          ),
        ),
      );
    }

    final lineData = LineChartBarData(
      spots: spots,
      isCurved: false,  // 직선으로 변경
      color: theme.colorScheme.primary,
      barWidth: 3,
      dotData: const FlDotData(show: true),
      belowBarData: BarAreaData(
        show: true,
        color: theme.colorScheme.primary.withOpacity(0.1),
      ),
    );

    return LineChart(
      LineChartData(
        lineBarsData: [lineData],
        titlesData: FlTitlesData(
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 40,
              getTitlesWidget: (value, meta) {
                return Text(
                  '${value.toInt()}%',
                  style: theme.textTheme.bodySmall,
                );
              },
            ),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 30,
              getTitlesWidget: (value, meta) {
                final label = dateLabels[value.toInt()];
                if (label == null) return const SizedBox.shrink();
                return Text(
                  label,
                  style: theme.textTheme.bodySmall,
                );
              },
            ),
          ),
          rightTitles: const AxisTitles(
            sideTitles: SideTitles(showTitles: false),
          ),
          topTitles: const AxisTitles(
            sideTitles: SideTitles(showTitles: false),
          ),
        ),
        gridData: FlGridData(
          show: true,
          drawVerticalLine: true,
          getDrawingHorizontalLine: (value) {
            return FlLine(
              color: theme.colorScheme.outline.withOpacity(0.2),
              strokeWidth: 1,
            );
          },
          getDrawingVerticalLine: (value) {
            return FlLine(
              color: theme.colorScheme.outline.withOpacity(0.2),
              strokeWidth: 1,
            );
          },
        ),
        borderData: FlBorderData(
          show: true,
          border: Border.all(
            color: theme.colorScheme.outline.withOpacity(0.3),
          ),
        ),
        minY: 0,
        maxY: 100,
      ),
    );
  }

}
