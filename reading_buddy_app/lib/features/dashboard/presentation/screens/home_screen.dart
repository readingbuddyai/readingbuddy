import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/widgets/widgets.dart';
import '../providers/home_provider.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  final PageController _pageController = PageController();
  int _currentPage = 0;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final homeState = ref.watch(homeProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => ref.read(homeProvider.notifier).refresh(),
          child: CustomScrollView(
            slivers: [
              // 상단 헤더
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 20,
                        backgroundColor: theme.colorScheme.primary.withOpacity(0.1),
                        child: Icon(
                          Icons.person,
                          color: theme.colorScheme.primary,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          'Reading Buddy',
                          style: theme.textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.notifications_outlined),
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('알림 기능은 추후 추가될 예정입니다')),
                          );
                        },
                      ),
                    ],
                  ),
                ),
              ),

              // 스와이프 가능한 카드 영역
              SliverToBoxAdapter(
                child: Column(
                  children: [
                    SizedBox(
                      height: 160,
                      child: PageView(
                        controller: _pageController,
                        onPageChanged: (index) {
                          setState(() {
                            _currentPage = index;
                          });
                        },
                        children: [
                          // 카드 1: 오늘의 출석 현황
                          InfoCard(
                            title: '오늘의 출석',
                            value: homeState.attendedToday ? '출석 완료' : '미출석',
                            subtitle: '학습 시간: ${homeState.todayPlaytime}',
                            icon: homeState.attendedToday
                                ? Icons.check_circle
                                : Icons.circle_outlined,
                            color: homeState.attendedToday ? Colors.green : Colors.grey,
                          ),

                          // 카드 2: 이번 주 학습 시간
                          InfoCard(
                            title: '이번 주 학습',
                            value: homeState.weeklyPlaytime,
                            subtitle: '${homeState.weeklyAttendDays}일 출석',
                            icon: Icons.timer,
                            color: theme.colorScheme.primary,
                          ),

                          // 카드 3: 최근 학습 스테이지
                          InfoCard(
                            title: '최근 학습 스테이지',
                            value: homeState.lastStage ?? '-',
                            subtitle: homeState.lastCorrectRate != null
                                ? '정답률: ${homeState.lastCorrectRate!.toStringAsFixed(1)}%'
                                : null,
                            icon: Icons.school,
                            color: Colors.orange,
                          ),
                        ],
                      ),
                    ),

                    // 페이지 인디케이터
                    const SizedBox(height: 8),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: List.generate(3, (index) {
                        return Container(
                          margin: const EdgeInsets.symmetric(horizontal: 4),
                          width: 8,
                          height: 8,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: _currentPage == index
                                ? theme.colorScheme.primary
                                : Colors.grey[600],
                          ),
                        );
                      }),
                    ),
                  ],
                ),
              ),

              // 2열 메트릭 카드
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Row(
                    children: [
                      Expanded(
                        child: MetricCard(
                          label: '연속 출석',
                          value: '${homeState.consecutiveDays}일',
                          icon: Icons.local_fire_department,
                          color: Colors.orange,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: MetricCard(
                          label: '오늘 학습',
                          value: homeState.todayPlaytime,
                          icon: Icons.access_time,
                          color: Colors.blue,
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              // 최근 학습 정답률 섹션
              SliverToBoxAdapter(
                child: SectionHeader(
                  title: '최근 학습 성과',
                  subtitle: homeState.lastStage != null
                      ? '${homeState.lastStage}'
                      : '학습 기록이 없습니다',
                ),
              ),

              // 정답률 간단 표시
              if (homeState.lastCorrectRate != null)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(
                                  '정답률',
                                  style: theme.textTheme.titleMedium,
                                ),
                                Text(
                                  '${homeState.lastCorrectRate!.toStringAsFixed(1)}%',
                                  style: theme.textTheme.titleLarge?.copyWith(
                                    fontWeight: FontWeight.bold,
                                    color: _getCorrectRateColor(
                                        homeState.lastCorrectRate!),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(8),
                              child: LinearProgressIndicator(
                                value: homeState.lastCorrectRate! / 100,
                                minHeight: 8,
                                backgroundColor: Colors.grey[800],
                                color: _getCorrectRateColor(
                                    homeState.lastCorrectRate!),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),

              // 하단 여백
              const SliverToBoxAdapter(
                child: SizedBox(height: 24),
              ),
            ],
          ),
        ),
      ),
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
