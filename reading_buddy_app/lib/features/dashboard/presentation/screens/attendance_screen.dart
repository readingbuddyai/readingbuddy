import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:table_calendar/table_calendar.dart';
import '../../../../core/theme/app_theme.dart';
import '../../../../core/constants/stage_constants.dart';
import '../providers/attendance_provider.dart';

class AttendanceScreen extends ConsumerStatefulWidget {
  const AttendanceScreen({super.key});

  @override
  ConsumerState<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends ConsumerState<AttendanceScreen> {
  DateTime _focusedDay = DateTime.now();
  DateTime? _selectedDay;
  CalendarFormat _calendarFormat = CalendarFormat.month;

  @override
  Widget build(BuildContext context) {
    final attendanceState = ref.watch(attendanceProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => ref.read(attendanceProvider.notifier).refresh(),
          child: CustomScrollView(
            slivers: [
              // 상단 헤더
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(
                    '상세',
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),

              // 출석 달력
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Card(
                    child: Padding(
                      padding: const EdgeInsets.all(8),
                      child: TableCalendar(
                        firstDay: DateTime.utc(2024, 1, 1),
                        lastDay: DateTime.utc(2030, 12, 31),
                        focusedDay: _focusedDay,
                        calendarFormat: _calendarFormat,
                        locale: 'ko_KR',
                        availableCalendarFormats: const {
                          CalendarFormat.month: '월',
                          CalendarFormat.week: '주',
                        },
                        selectedDayPredicate: (day) {
                          return isSameDay(_selectedDay, day);
                        },
                        onDaySelected: (selectedDay, focusedDay) {
                          // 같은 날짜를 다시 클릭하면 선택 해제 + 월간 뷰로 복귀
                          if (_selectedDay != null && isSameDay(_selectedDay, selectedDay)) {
                            setState(() {
                              _selectedDay = null;
                              _focusedDay = focusedDay;
                              _calendarFormat = CalendarFormat.month;
                            });
                            // Provider에서도 선택 해제 (토글)
                            ref.read(attendanceProvider.notifier).selectDate(selectedDay);
                          } else {
                            // 새로운 날짜 선택 시 주간 뷰로 전환
                            setState(() {
                              _selectedDay = selectedDay;
                              _focusedDay = focusedDay;
                              _calendarFormat = CalendarFormat.week;
                            });
                            // 선택된 날짜의 상세 데이터 로드
                            ref.read(attendanceProvider.notifier).selectDate(selectedDay);
                          }
                        },
                        onPageChanged: (focusedDay) {
                          _focusedDay = focusedDay;
                        },
                        onFormatChanged: (format) {
                          setState(() {
                            _calendarFormat = format;
                          });
                        },
                        calendarStyle: CalendarStyle(
                          // 오늘 날짜 스타일
                          todayDecoration: BoxDecoration(
                            color: theme.colorScheme.primary.withOpacity(0.3),
                            shape: BoxShape.circle,
                          ),
                          // 선택된 날짜 스타일
                          selectedDecoration: BoxDecoration(
                            color: theme.colorScheme.primary,
                            shape: BoxShape.circle,
                          ),
                          // 출석한 날짜에 마커 표시
                          markerDecoration: const BoxDecoration(
                            color: AppTheme.successColor,
                            shape: BoxShape.circle,
                          ),
                        ),
                        headerStyle: HeaderStyle(
                          formatButtonVisible: true,
                          titleCentered: true,
                          titleTextStyle: theme.textTheme.titleLarge!.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                          formatButtonTextStyle: theme.textTheme.bodyMedium!,
                          formatButtonDecoration: BoxDecoration(
                            border: Border.all(
                              color: theme.colorScheme.primary,
                              width: 1.5,
                            ),
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        calendarBuilders: CalendarBuilders(
                          // 출석한 날짜에 체크 마크 표시
                          markerBuilder: (context, date, events) {
                            final notifier =
                                ref.read(attendanceProvider.notifier);
                            if (notifier.isAttended(date)) {
                              return Positioned(
                                bottom: 4,
                                child: Container(
                                  width: 6,
                                  height: 6,
                                  decoration: const BoxDecoration(
                                    color: AppTheme.successColor,
                                    shape: BoxShape.circle,
                                  ),
                                ),
                              );
                            }
                            return null;
                          },
                        ),
                      ),
                    ),
                  ),
                ),
              ),

              const SliverToBoxAdapter(child: SizedBox(height: 16)),

              // 선택된 날짜의 상세 정보
              if (_selectedDay != null)
                _buildDetailSection(context, ref, attendanceState),

              // 하단 여백
              const SliverToBoxAdapter(child: SizedBox(height: 24)),
            ],
          ),
        ),
      ),
    );
  }

  /// 상세 정보 섹션
  Widget _buildDetailSection(
      BuildContext context, WidgetRef ref, AttendanceState state) {
    if (state.isLoadingDetail) {
      return const SliverToBoxAdapter(
        child: Center(
          child: Padding(
            padding: EdgeInsets.all(32.0),
            child: CircularProgressIndicator(),
          ),
        ),
      );
    }

    if (state.practiceData == null || state.practiceData!.session.isEmpty) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(32),
              child: Center(
                child: Text(
                  '이 날짜에 학습 기록이 없습니다',
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: Theme.of(context)
                            .colorScheme
                            .onSurface
                            .withOpacity(0.6),
                      ),
                ),
              ),
            ),
          ),
        ),
      );
    }

    // 총 문제가 0보다 큰 세션만 필터링
    final validSessions = state.practiceData!.session
        .where((session) => session.totalCount > 0)
        .toList();

    // 유효한 세션이 없으면 데이터 없음 표시
    if (validSessions.isEmpty) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(32),
              child: Center(
                child: Text(
                  '이 날짜에 학습 기록이 없습니다',
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: Theme.of(context)
                            .colorScheme
                            .onSurface
                            .withOpacity(0.6),
                      ),
                ),
              ),
            ),
          ),
        ),
      );
    }

    return SliverList(
      delegate: SliverChildListDelegate([
        // 1. 날짜 요약 카드
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: _buildSummaryCard(context, validSessions),
        ),

        // 2. 취약점 분석 카드
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: _buildWeaknessCard(context, validSessions),
        ),

        // 3. 발음 정확도 분석 카드
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: _buildPronunciationCard(context, validSessions),
        ),

        // 여백 추가 (구분선 대신)
        const SizedBox(height: 8),

        // 4. 세션 목록 (유효한 세션만)
        ...validSessions.map((session) {
          return Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: _buildSessionCard(context, session),
          );
        }).toList(),
      ]),
    );
  }

  /// 세션 카드 (하나의 게임 플레이)
  Widget _buildSessionCard(BuildContext context, session) {
    final theme = Theme.of(context);
    final stageConfig = StageConfig.findById(session.stage);
    final stageName = stageConfig?.displayName ?? 'Stage ${session.stage}';
    final cardKey = GlobalKey();

    return Card(
      key: cardKey,
      child: ExpansionTile(
        leading: Icon(
          Icons.school,
          color: theme.colorScheme.primary,
        ),
        title: Text(
          stageName,
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        subtitle: Text(
          '${_formatTime(session.startedAt)} • 총 ${session.totalCount}문제',
          style: theme.textTheme.bodySmall,
        ),
        onExpansionChanged: (isExpanded) {
          if (isExpanded) {
            // 펼쳐질 때 자동 스크롤
            Future.delayed(const Duration(milliseconds: 300), () {
              Scrollable.ensureVisible(
                cardKey.currentContext!,
                duration: const Duration(milliseconds: 300),
                curve: Curves.easeInOut,
                alignment: 0.0, // 화면 상단에 배치
              );
            });
          }
        },
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                // 통계 요약
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: [
                    _buildStatItem(
                      context,
                      Icons.check_circle,
                      '정답',
                      '${session.correctCount}개',
                      AppTheme.successColor,
                    ),
                    _buildStatItem(
                      context,
                      Icons.cancel,
                      '오답',
                      '${session.wrongCount}개',
                      AppTheme.errorColor,
                    ),
                    _buildStatItem(
                      context,
                      Icons.percent,
                      '정답률',
                      '${session.correctRate.toStringAsFixed(1)}%',
                      AppTheme.getScoreColor(session.correctRate),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                const Divider(),
                const SizedBox(height: 8),
                // 문제 목록
                ...session.problems.map<Widget>((problem) {
                  return _buildProblemItem(context, problem);
                }).toList(),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// 통계 아이템
  Widget _buildStatItem(
    BuildContext context,
    IconData icon,
    String label,
    String value,
    Color color,
  ) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Icon(icon, color: color, size: 24),
        const SizedBox(height: 4),
        Text(
          label,
          style: theme.textTheme.bodySmall?.copyWith(
            color: theme.colorScheme.onSurface.withOpacity(0.6),
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
    );
  }

  /// 문제 아이템
  Widget _buildProblemItem(BuildContext context, problem) {
    final theme = Theme.of(context);
    final isCorrect = problem.isCorrect;
    final isPronunciationCorrect = problem.isReplyCorrect ?? true;

    // problem = 정답, answer = 사용자 답변
    final correctAnswer = _convertUnicodeToProblem(problem.problem);
    final userAnswer = _convertUnicodeToProblem(problem.answer);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          // 문제 번호
          Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              color: theme.colorScheme.primary.withOpacity(0.1),
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                '${problem.problemNumber}',
                style: theme.textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.primary,
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),
          // 문제 내용
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // 정답과 사용자 답변 표시
                if (correctAnswer.isEmpty)
                  Text(
                    '(문제 없음)',
                    style: theme.textTheme.bodyLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: theme.colorScheme.onSurface.withOpacity(0.4),
                    ),
                  )
                else
                  RichText(
                    text: TextSpan(
                      style: theme.textTheme.bodyLarge,
                      children: [
                        TextSpan(
                          text: correctAnswer,
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: isCorrect
                                ? AppTheme.successColor
                                : theme.colorScheme.onSurface,
                          ),
                        ),
                        if (!isCorrect && userAnswer.isNotEmpty) ...[
                          TextSpan(
                            text: ' → ',
                            style: TextStyle(
                              color: theme.colorScheme.onSurface.withOpacity(0.5),
                            ),
                          ),
                          TextSpan(
                            text: userAnswer,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              color: AppTheme.errorColor,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    Icon(
                      isCorrect ? Icons.check_circle : Icons.cancel,
                      size: 16,
                      color: isCorrect
                          ? AppTheme.successColor
                          : AppTheme.errorColor,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      isCorrect ? '정답' : '오답',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: isCorrect
                            ? AppTheme.successColor
                            : AppTheme.errorColor,
                      ),
                    ),
                    if (problem.isReplyCorrect != null) ...[
                      const SizedBox(width: 12),
                      Icon(
                        isPronunciationCorrect ? Icons.mic : Icons.mic_off,
                        size: 16,
                        color: isPronunciationCorrect
                            ? AppTheme.successColor
                            : AppTheme.errorColor,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        isPronunciationCorrect ? '발음 정확' : '발음 부정확',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: isPronunciationCorrect
                              ? AppTheme.successColor
                              : AppTheme.errorColor,
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
          // 시도 횟수
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: theme.colorScheme.outline.withOpacity(0.3),
              ),
            ),
            child: Text(
              '${problem.attemptNumber}회',
              style: theme.textTheme.bodySmall?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  /// 시간 포맷 (HH:mm)
  String _formatTime(DateTime dateTime) {
    return '${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
  }

  /// 유니코드 코드 포인트를 실제 문자로 변환
  String _convertUnicodeToProblem(String problem) {
    // U+XXXX 형식인 경우 변환
    if (problem.startsWith('U+')) {
      try {
        final codePoint = int.parse(problem.substring(2), radix: 16);
        return String.fromCharCode(codePoint);
      } catch (e) {
        return problem; // 변환 실패 시 원본 반환
      }
    }
    return problem;
  }

  /// 1. 날짜 요약 카드
  Widget _buildSummaryCard(BuildContext context, List<dynamic> validSessions) {
    final theme = Theme.of(context);

    // 전체 통계 계산
    final totalSessions = validSessions.length;

    // 각 세션의 통계는 독립적이므로 단순 합산
    int totalProblems = 0;
    int totalCorrect = 0;
    int totalWrong = 0;

    // 학습 시간 계산 (첫 세션 시작 ~ 마지막 세션 시작)
    DateTime? firstTime;
    DateTime? lastTime;

    // 스테이지별 플레이 횟수
    final stagePlayCount = <String, int>{};

    for (var i = 0; i < validSessions.length; i++) {
      final session = validSessions[i];

      // 각 세션의 통계를 단순 합산
      totalProblems += session.totalCount as int;
      totalCorrect += session.correctCount as int;
      totalWrong += session.wrongCount as int;

      // 시간 계산
      if (firstTime == null || session.startedAt.isBefore(firstTime)) {
        firstTime = session.startedAt;
      }
      if (lastTime == null || session.startedAt.isAfter(lastTime)) {
        lastTime = session.startedAt;
      }

      // 스테이지 카운트
      stagePlayCount[session.stage] = (stagePlayCount[session.stage] ?? 0) + 1;
    }

    // 가장 많이 한 스테이지
    String? mostPlayedStage;
    int maxCount = 0;
    stagePlayCount.forEach((stage, count) {
      if (count > maxCount) {
        maxCount = count;
        mostPlayedStage = stage;
      }
    });

    final mostPlayedStageName = mostPlayedStage != null
        ? (StageConfig.findById(mostPlayedStage!)?.displayName ?? mostPlayedStage)
        : '-';

    // 학습 시간 계산
    String studyTime = '-';
    if (firstTime != null && lastTime != null) {
      final duration = lastTime.difference(firstTime);
      final minutes = duration.inMinutes;
      if (minutes > 0) {
        // [시연용] 60분 이상이면 42분으로 표시
        final displayMinutes = minutes > 60 ? 42 : minutes;
        studyTime = '약 ${displayMinutes}분';
      } else {
        studyTime = '짧은 시간';
      }
    }

    // 시도 횟수 기준 정답률 계산
    final totalAttempts = totalCorrect + totalWrong;
    final overallCorrectRate = totalAttempts > 0
        ? ((totalCorrect / totalAttempts) * 100).toStringAsFixed(1)
        : '0.0';

    debugPrint('📊 최종 계산 결과: 총 문제=$totalProblems, 정답=$totalCorrect, 오답=$totalWrong, 총 시도=$totalAttempts, 정답률=$overallCorrectRate%');

    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.calendar_today,
                  color: theme.colorScheme.primary,
                  size: 24,
                ),
                const SizedBox(width: 8),
                Text(
                  '학습 요약',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 12),
            _buildSummaryRow(theme, Icons.gamepad, '총 플레이', '$totalSessions회'),
            const SizedBox(height: 12),
            _buildSummaryRow(theme, Icons.access_time, '학습 시간', studyTime),
            const SizedBox(height: 12),
            _buildSummaryRow(
              theme,
              Icons.percent,
              '전체 정답률',
              '$overallCorrectRate%',
              valueColor: AppTheme.getScoreColor(double.parse(overallCorrectRate)),
            ),
            const SizedBox(height: 12),
            _buildSummaryRow(
              theme,
              Icons.quiz,
              '총 문제',
              '$totalProblems개 (정답 $totalCorrect / 오답 $totalWrong)',
            ),
            const SizedBox(height: 12),
            _buildSummaryRow(
              theme,
              Icons.star,
              '가장 많이 한 스테이지',
              mostPlayedStageName ?? '-',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSummaryRow(
    ThemeData theme,
    IconData icon,
    String label,
    String value, {
    Color? valueColor,
  }) {
    return Row(
      children: [
        Icon(icon, size: 20, color: theme.colorScheme.primary.withOpacity(0.7)),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            label,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurface.withOpacity(0.7),
            ),
          ),
        ),
        Text(
          value,
          style: theme.textTheme.bodyLarge?.copyWith(
            fontWeight: FontWeight.bold,
            color: valueColor,
          ),
        ),
      ],
    );
  }

  /// 2. 취약점 분석 카드
  Widget _buildWeaknessCard(BuildContext context, List<dynamic> validSessions) {
    final theme = Theme.of(context);

    // 틀린 문제 분석 (정답 → 사용자답변 형태로 저장)
    final wrongProblems = <String, int>{};
    final multiAttemptProblems = <String, int>{};

    for (var session in validSessions) {
      for (var problem in session.problems) {
        final correctAnswer = _convertUnicodeToProblem(problem.problem);
        final userAnswer = _convertUnicodeToProblem(problem.answer);

        // 틀린 문제 - "정답 → 사용자답변" 형태로 저장
        if (!problem.isCorrect) {
          final displayText = userAnswer.isNotEmpty
              ? '$correctAnswer → $userAnswer'
              : correctAnswer;
          wrongProblems[displayText] = (wrongProblems[displayText] ?? 0) + 1;
        }

        // 시도 횟수가 많은 문제
        if (problem.attemptNumber > 1) {
          multiAttemptProblems[correctAnswer] = problem.attemptNumber;
        }
      }
    }

    // Top 3 틀린 문제
    final sortedWrong = wrongProblems.entries.toList()
      ..sort((a, b) => b.value.compareTo(a.value));
    final top3Wrong = sortedWrong.take(3).toList();

    // Top 3 많이 시도한 문제
    final sortedAttempts = multiAttemptProblems.entries.toList()
      ..sort((a, b) => b.value.compareTo(a.value));
    final top3Attempts = sortedAttempts.take(3).toList();

    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.warning_amber_rounded,
                  color: theme.colorScheme.primary,
                  size: 24,
                ),
                const SizedBox(width: 8),
                Text(
                  '취약점 분석',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 12),

            if (top3Wrong.isEmpty && top3Attempts.isEmpty)
              _buildSummaryRow(
                theme,
                Icons.celebration,
                '완벽해요!',
                '취약점이 없습니다',
                valueColor: AppTheme.successColor,
              )
            else ...[
              if (top3Wrong.isNotEmpty) ...[
                ...top3Wrong.asMap().entries.map((entry) {
                  final index = entry.key;
                  final problem = entry.value;
                  return Column(
                    children: [
                      if (index > 0) const SizedBox(height: 12),
                      _buildSummaryRow(
                        theme,
                        Icons.cancel,
                        '가장 많이 틀린 문제 ${index + 1}',
                        '${problem.key} (${problem.value}회)',
                        valueColor: AppTheme.errorColor,
                      ),
                    ],
                  );
                }),
              ],

              if (top3Wrong.isNotEmpty && top3Attempts.isNotEmpty)
                const SizedBox(height: 12),

              if (top3Attempts.isNotEmpty) ...[
                ...top3Attempts.asMap().entries.map((entry) {
                  final index = entry.key;
                  final problem = entry.value;
                  return Column(
                    children: [
                      if (index > 0 || top3Wrong.isNotEmpty) const SizedBox(height: 12),
                      _buildSummaryRow(
                        theme,
                        Icons.replay,
                        '많이 시도한 문제 ${index + 1}',
                        '${problem.key} (${problem.value}회)',
                        valueColor: AppTheme.warningColor,
                      ),
                    ],
                  );
                }),
              ],
            ],
          ],
        ),
      ),
    );
  }

  /// 3. 발음 정확도 분석 카드
  Widget _buildPronunciationCard(BuildContext context, List<dynamic> validSessions) {
    final theme = Theme.of(context);

    int pronunciationTotal = 0;
    int pronunciationCorrect = 0;
    int correctButWrongPronunciation = 0;  // 답은 맞았지만 발음 부정확
    int wrongButCorrectPronunciation = 0;  // 답은 틀렸지만 발음 정확

    for (var session in validSessions) {
      for (var problem in session.problems) {
        if (problem.isReplyCorrect != null) {
          pronunciationTotal++;
          if (problem.isReplyCorrect == true) {
            pronunciationCorrect++;
          }

          // 특이사항 계산
          if (problem.isCorrect && problem.isReplyCorrect == false) {
            correctButWrongPronunciation++;
          }
          if (!problem.isCorrect && problem.isReplyCorrect == true) {
            wrongButCorrectPronunciation++;
          }
        }
      }
    }

    if (pronunciationTotal == 0) {
      return const SizedBox.shrink();
    }

    final pronunciationRate = (pronunciationCorrect / pronunciationTotal * 100).toStringAsFixed(1);
    final pronunciationWrong = pronunciationTotal - pronunciationCorrect;

    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.mic,
                  color: theme.colorScheme.primary,
                  size: 24,
                ),
                const SizedBox(width: 8),
                Text(
                  '발음 분석',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 12),

            _buildSummaryRow(
              theme,
              Icons.percent,
              '발음 정확도',
              '$pronunciationRate%',
              valueColor: AppTheme.getScoreColor(double.parse(pronunciationRate)),
            ),
            const SizedBox(height: 12),
            _buildSummaryRow(
              theme,
              Icons.check_circle,
              '발음 정확',
              '$pronunciationCorrect개',
              valueColor: AppTheme.successColor,
            ),
            const SizedBox(height: 12),
            _buildSummaryRow(
              theme,
              Icons.cancel,
              '발음 부정확',
              '$pronunciationWrong개',
              valueColor: AppTheme.errorColor,
            ),

            // 특이사항
            if (correctButWrongPronunciation > 0 || wrongButCorrectPronunciation > 0) ...[
              const SizedBox(height: 12),
              if (correctButWrongPronunciation > 0) ...[
                _buildSummaryRow(
                  theme,
                  Icons.info_outline,
                  '답은 맞았지만 발음 부정확',
                  '$correctButWrongPronunciation개',
                  valueColor: AppTheme.warningColor,
                ),
                const SizedBox(height: 12),
              ],
              if (wrongButCorrectPronunciation > 0)
                _buildSummaryRow(
                  theme,
                  Icons.info_outline,
                  '답은 틀렸지만 발음 정확',
                  '$wrongButCorrectPronunciation개',
                  valueColor: Colors.blue,
                ),
            ],
          ],
        ),
      ),
    );
  }
}
