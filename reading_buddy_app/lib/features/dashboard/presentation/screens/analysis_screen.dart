import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/widgets/widgets.dart';
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

              // 스테이지 선택 탭
              SliverToBoxAdapter(
                child: _buildStageSelector(context, ref, analysisState),
              ),

              // 스테이지 통계 (2열 그리드)
              if (analysisState.tryAvg != null &&
                  analysisState.correctRate != null)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Row(
                      children: [
                        Expanded(
                          child: MetricCard(
                            label: '평균 시도',
                            value:
                                '${analysisState.tryAvg!.averageTryCount.toStringAsFixed(1)}회',
                            icon: Icons.replay,
                            color: Colors.blue,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: MetricCard(
                            label: '정답률',
                            value:
                                '${analysisState.correctRate!.correctRate.toStringAsFixed(1)}%',
                            icon: Icons.check_circle,
                            color: _getCorrectRateColor(
                                analysisState.correctRate!.correctRate),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

              // 스테이지 상세 정보
              if (analysisState.stageInfo != null)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              '스테이지 통계',
                              style: theme.textTheme.titleMedium?.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: 16),
                            _buildStatRow(
                              '총 시도 횟수',
                              '${analysisState.stageInfo!.totalTryCount}회',
                              theme,
                            ),
                            const SizedBox(height: 8),
                            _buildStatRow(
                              '정답 수',
                              '${analysisState.stageInfo!.totalCorrectCount}회',
                              theme,
                              color: Colors.green,
                            ),
                            const SizedBox(height: 8),
                            _buildStatRow(
                              '오답 수',
                              '${analysisState.stageInfo!.totalWrongCount}회',
                              theme,
                              color: Colors.red,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),

              const SliverToBoxAdapter(child: SizedBox(height: 16)),

              // 취약 음소 섹션
              SliverToBoxAdapter(
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
                const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Center(
                      child: Text(
                        '아직 데이터가 없습니다',
                        style: TextStyle(color: Colors.grey),
                      ),
                    ),
                  ),
                ),

              const SliverToBoxAdapter(child: SizedBox(height: 24)),

              // 많이 연습한 음소 섹션
              SliverToBoxAdapter(
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
                const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Center(
                      child: Text(
                        '아직 데이터가 없습니다',
                        style: TextStyle(color: Colors.grey),
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

  /// 스테이지 선택 탭
  Widget _buildStageSelector(
      BuildContext context, WidgetRef ref, AnalysisState state) {
    final stages = ['1.1.1', '1.1.2', '1.2.1', '1.2.2', '2', '3', '4'];
    final theme = Theme.of(context);

    return SizedBox(
      height: 50,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: stages.length,
        itemBuilder: (context, index) {
          final stage = stages[index];
          final isSelected = state.selectedStage == stage;

          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              label: Text('Stage $stage'),
              selected: isSelected,
              onSelected: (selected) {
                if (selected) {
                  ref.read(analysisProvider.notifier).selectStage(stage);
                }
              },
              selectedColor: theme.colorScheme.primary,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : theme.colorScheme.onSurface,
                fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
              ),
            ),
          );
        },
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

  /// 정답률에 따른 색상 반환
  Color _getCorrectRateColor(double rate) {
    if (rate >= 80) {
      return Colors.green;
    } else if (rate >= 60) {
      return Colors.orange;
    } else {
      return Colors.red;
    }
  }
}
