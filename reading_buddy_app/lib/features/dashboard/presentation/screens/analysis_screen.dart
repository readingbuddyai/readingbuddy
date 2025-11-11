import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/constants/stage_constants.dart';
import '../providers/analysis_provider.dart';

class AnalysisScreen extends ConsumerWidget {
  const AnalysisScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final analysisState = ref.watch(analysisProvider);
    final theme = Theme.of(context);

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
                                  // 1. 전체 정답률
                                  _buildStatItem(
                                    icon: Icons.check_circle,
                                    label: '전체 정답률',
                                    value: _calculateOverallCorrectRate(analysisState),
                                    color: AppTheme.getScoreColor(
                                      _getOverallCorrectRateValue(analysisState),
                                    ),
                                    theme: theme,
                                  ),
                                  const SizedBox(height: 20),

                                  // 2. 평균 시도
                                  if (analysisState.tryAvg != null)
                                    _buildStatItem(
                                      icon: Icons.replay,
                                      label: '평균 시도',
                                      value: '${analysisState.tryAvg!.averageTryCount.toStringAsFixed(1)}회',
                                      color: theme.colorScheme.primary,
                                      theme: theme,
                                    )
                                  else
                                    _buildStatItem(
                                      icon: Icons.replay,
                                      label: '평균 시도',
                                      value: '-',
                                      color: theme.colorScheme.primary,
                                      theme: theme,
                                    ),
                                  const SizedBox(height: 20),

                                  // 3. 숙련된 음소
                                  _buildStatItem(
                                    icon: Icons.psychology,
                                    label: '숙련된 음소',
                                    value: _getMasteredKcCount(analysisState),
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

              // 취약 음소 섹션
              const SliverToBoxAdapter(
                child: SectionHeader(
                  title: '취약 음소 TOP 5',
                  subtitle: '가장 많이 틀린 음소',
                ),
              ),

              // 취약 음소 리스트
              if (analysisState.weakPhonemes != null &&
                  analysisState.weakPhonemes!.isNotEmpty)
                SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      final phoneme = analysisState.weakPhonemes![index];
                      return PhonemeRankItem(
                        rank: index + 1,
                        phoneme: phoneme.value,
                        category: phoneme.category,
                        count: phoneme.wrongCnt ?? 0,
                        countLabel: '실수',
                      );
                    },
                    childCount: analysisState.weakPhonemes!.length,
                  ),
                )
              else
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Center(
                      child: Text(
                        '아직 데이터가 없습니다',
                        style: theme.textTheme.bodyMedium,
                      ),
                    ),
                  ),
                ),

              const SliverToBoxAdapter(child: SizedBox(height: 24)),

              // 많이 연습한 음소 섹션
              const SliverToBoxAdapter(
                child: SectionHeader(
                  title: '많이 연습한 음소 TOP 5',
                  subtitle: '가장 많이 시도한 음소',
                ),
              ),

              // 많이 연습한 음소 리스트
              if (analysisState.practicedPhonemes != null &&
                  analysisState.practicedPhonemes!.isNotEmpty)
                SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      final phoneme = analysisState.practicedPhonemes![index];
                      return PhonemeRankItem(
                        rank: index + 1,
                        phoneme: phoneme.value,
                        category: phoneme.category,
                        count: phoneme.tryCnt ?? 0,
                        countLabel: '시도',
                      );
                    },
                    childCount: analysisState.practicedPhonemes!.length,
                  ),
                )
              else
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Center(
                      child: Text(
                        '아직 데이터가 없습니다',
                        style: theme.textTheme.bodyMedium,
                      ),
                    ),
                  ),
                ),

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
            child: DropdownButton<String>(
              value: state.selectedStage,
              isExpanded: true,
              underline: const SizedBox(),
              icon: Icon(
                Icons.keyboard_arrow_down,
                color: theme.colorScheme.primary,
              ),
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w600,
              ),
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
                  fontSize: 12,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: color,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  /// 전체 정답률 계산 (stageInfo 기반)
  String _calculateOverallCorrectRate(AnalysisState state) {
    if (state.stageInfo == null) return '-';

    final correctRate = state.stageInfo!.correctRate;
    return '${correctRate.toStringAsFixed(1)}%';
  }

  /// 전체 정답률 숫자 값 (색상 계산용)
  double _getOverallCorrectRateValue(AnalysisState state) {
    if (state.stageInfo == null) return 0.0;
    return state.stageInfo!.correctRate;
  }

  /// 숙련된 음소 개수 계산 (mastery >= 70%)
  String _getMasteredKcCount(AnalysisState state) {
    if (state.mastery?.kcMasteries == null) return '-';

    final kcMasteries = state.mastery!.kcMasteries!;
    final totalCount = kcMasteries.length;

    // pLearn >= 0.7인 KC 개수 계산
    final masteredCount = kcMasteries.where((kc) {
      return (kc.pLearn ?? 0.0) >= 0.7;
    }).length;

    return '$masteredCount개 / $totalCount개';
  }

}
