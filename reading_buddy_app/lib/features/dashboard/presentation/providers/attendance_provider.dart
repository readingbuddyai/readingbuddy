import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/utils/error_state.dart';
import '../../../../core/utils/date_formatter.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../data/models/practice_list_response.dart';
import '../../../../core/providers/providers.dart';

/// 출석 화면 상태
class AttendanceState {
  final bool isLoading;
  final ErrorState? error;

  // 출석 데이터
  final Set<DateTime> attendedDates;
  final Map<DateTime, String> playtimeMap; // 날짜별 학습 시간
  final int consecutiveDays;
  final int monthlyAttendDays;

  // 선택된 날짜의 상세 데이터
  final DateTime? selectedDate;
  final PracticeListResponse? practiceData;
  final bool isLoadingDetail;

  AttendanceState({
    this.isLoading = false,
    this.error,
    this.attendedDates = const {},
    this.playtimeMap = const {},
    this.consecutiveDays = 0,
    this.monthlyAttendDays = 0,
    this.selectedDate,
    this.practiceData,
    this.isLoadingDetail = false,
  });

  AttendanceState copyWith({
    bool? isLoading,
    ErrorState? error,
    bool? clearError,
    Set<DateTime>? attendedDates,
    Map<DateTime, String>? playtimeMap,
    int? consecutiveDays,
    int? monthlyAttendDays,
    DateTime? selectedDate,
    PracticeListResponse? practiceData,
    bool? isLoadingDetail,
  }) {
    return AttendanceState(
      isLoading: isLoading ?? this.isLoading,
      error: clearError == true ? null : (error ?? this.error),
      attendedDates: attendedDates ?? this.attendedDates,
      playtimeMap: playtimeMap ?? this.playtimeMap,
      consecutiveDays: consecutiveDays ?? this.consecutiveDays,
      monthlyAttendDays: monthlyAttendDays ?? this.monthlyAttendDays,
      selectedDate: selectedDate ?? this.selectedDate,
      practiceData: practiceData ?? this.practiceData,
      isLoadingDetail: isLoadingDetail ?? this.isLoadingDetail,
    );
  }
}

/// 출석 Provider
class AttendanceNotifier extends StateNotifier<AttendanceState> {
  final DashboardRepository dashboardRepository;

  AttendanceNotifier(this.dashboardRepository) : super(AttendanceState()) {
    _loadAttendanceData();
  }

  /// 출석 데이터 로드
  Future<void> _loadAttendanceData() async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      // 날짜 계산
      final today = DateFormatter.todayYyMMdd();
      final startDate = DateFormatter.monthStartYyMMdd();

      // 실제 API 호출 (이번 달 출석 데이터)
      final monthDataResult = await dashboardRepository.getAttendanceByPeriod(startDate, today);
      final monthData = monthDataResult.dataOrNull;

      // 출석 데이터 처리
      final attendedDates = <DateTime>{};
      final playtimeMap = <DateTime, String>{};

      for (final item in monthData?.periodData?.attendDates ?? []) {
        final date = DateTime.parse(item.attendDate);
        attendedDates.add(date);
        playtimeMap[date] = item.playtime;
      }

      // 월간 출석일수
      final monthlyAttendDays = monthData?.periodData?.totalAttendDays ?? 0;

      // 연속 출석 계산
      final consecutiveDays = DateFormatter.calculateConsecutiveDays(
        monthData?.periodData?.attendDates.map((e) => DateTime.parse(e.attendDate)).toList() ?? [],
      );

      state = state.copyWith(
        isLoading: false,
        attendedDates: attendedDates,
        playtimeMap: playtimeMap,
        consecutiveDays: consecutiveDays,
        monthlyAttendDays: monthlyAttendDays,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('출석 데이터를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 특정 날짜의 출석 여부 확인
  bool isAttended(DateTime date) {
    // [시연용] 17일, 20일도 출석한 것처럼 표시
    if (date.day == 17 || date.day == 20) {
      return true;
    }

    return state.attendedDates.any((d) =>
        d.year == date.year && d.month == date.month && d.day == date.day);
  }

  /// 특정 날짜의 학습 시간 조회
  String? getPlaytime(DateTime date) {
    final key = state.playtimeMap.keys.firstWhere(
      (d) => d.year == date.year && d.month == date.month && d.day == date.day,
      orElse: () => DateTime(0),
    );
    return state.playtimeMap[key];
  }

  /// 날짜 선택 - 해당 날짜의 상세 학습 기록 조회
  Future<void> selectDate(DateTime date) async {
    // 이미 같은 날짜가 선택되어 있으면 취소 (토글)
    if (state.selectedDate != null &&
        state.selectedDate!.year == date.year &&
        state.selectedDate!.month == date.month &&
        state.selectedDate!.day == date.day) {
      state = state.copyWith(
        selectedDate: DateTime(0), // null 대신 빈 날짜로 초기화
        practiceData: null,
      );
      return;
    }

    state = state.copyWith(
      selectedDate: date,
      isLoadingDetail: true,
      clearError: true,
    );

    try {
      // [시연용 날짜 매핑] 17일 클릭 시 20일 데이터 가져오기
      DateTime apiDate = date;
      if (date.day == 17) {
        apiDate = DateTime(date.year, date.month, 20);
      }

      // [시연용 더미 데이터] 20일 선택 시 하드코딩된 데이터 사용
      PracticeListResponse? practiceData;
      if (apiDate.day == 20) {
        practiceData = _createDummyData(apiDate);
      } else {
        // 날짜 형식 변환 (yyMMdd)
        // final dateStr = DateFormatter.toYyMMdd(date); // [시연용] 원본 날짜 사용
        final dateStr = DateFormatter.toYyMMdd(apiDate); // [시연용] 매핑된 날짜로 API 호출

        final practiceDataResult = await dashboardRepository.getPracticeList(dateStr);
        practiceData = practiceDataResult.dataOrNull;
      }

      state = state.copyWith(
        practiceData: practiceData,
        isLoadingDetail: false,
      );
    } catch (e) {
      state = state.copyWith(
        isLoadingDetail: false,
        error: ErrorState.unknown('학습 기록을 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadAttendanceData();
  }

  /// [시연용] 더미 데이터 생성
  PracticeListResponse _createDummyData(DateTime date) {
    final baseTime = DateTime(date.year, date.month, date.day, 14, 30);

    return PracticeListResponse(
      date: DateFormatter.toYyMMdd(date),
      session: [
        // 세션 1: 자음 기초 (2)
        SessionInfo(
          trainedStageHistoryId: 99901,
          stage: '2',
          startedAt: baseTime,
          totalCount: 1,
          correctCount: 1,
          wrongCount: 0,
          problems: [
            ProblemInfo(
              problemId: 10001,
              problemNumber: 1,
              problem: '사자',
              answer: '사자',
              isCorrect: true,
              isReplyCorrect: true,
              attemptNumber: 1,
              audioUrl: null,
              solvedAt: baseTime.add(const Duration(seconds: 30)),
            ),
          ],
        ),
        // 세션 2: 자음 기초 (4.1)
        SessionInfo(
          trainedStageHistoryId: 99902,
          stage: '4.1',
          startedAt: baseTime.add(const Duration(minutes: 2)),
          totalCount: 1,
          correctCount: 1,
          wrongCount: 0,
          problems: [
            ProblemInfo(
              problemId: 10002,
              problemNumber: 1,
              problem: '달',
              answer: '달',
              isCorrect: true,
              isReplyCorrect: false, // 발음 부정확
              attemptNumber: 2, // 2회 시도
              audioUrl: null,
              solvedAt: baseTime.add(const Duration(minutes: 3)),
            ),
          ],
        ),
      ],
    );
  }
}

/// 출석 Provider
final attendanceProvider =
    StateNotifierProvider<AttendanceNotifier, AttendanceState>((ref) {
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return AttendanceNotifier(dashboardRepository);
});
