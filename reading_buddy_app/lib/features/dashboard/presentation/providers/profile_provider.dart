import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/utils/error_state.dart';
import '../../../../core/utils/date_formatter.dart';
import '../../../../core/constants/learning_constants.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../../../core/storage/token_storage.dart';

/// 프로필 화면 상태
class ProfileState {
  final bool isLoading;
  final ErrorState? error;

  // 사용자 정보
  final int? userId;
  final String? email;
  final String? nickname;

  // 학습 통계
  final String totalLearningTime;
  final int totalAttendDays;
  final int consecutiveDays;

  ProfileState({
    this.isLoading = false,
    this.error,
    this.userId,
    this.email,
    this.nickname,
    this.totalLearningTime = '0시간 0분',
    this.totalAttendDays = 0,
    this.consecutiveDays = 0,
  });

  ProfileState copyWith({
    bool? isLoading,
    ErrorState? error,
    bool? clearError,
    int? userId,
    String? email,
    String? nickname,
    String? totalLearningTime,
    int? totalAttendDays,
    int? consecutiveDays,
  }) {
    return ProfileState(
      isLoading: isLoading ?? this.isLoading,
      error: clearError == true ? null : (error ?? this.error),
      userId: userId ?? this.userId,
      email: email ?? this.email,
      nickname: nickname ?? this.nickname,
      totalLearningTime: totalLearningTime ?? this.totalLearningTime,
      totalAttendDays: totalAttendDays ?? this.totalAttendDays,
      consecutiveDays: consecutiveDays ?? this.consecutiveDays,
    );
  }
}

/// 프로필 Provider
class ProfileNotifier extends StateNotifier<ProfileState> {
  final TokenStorage _tokenStorage;
  final DashboardRepository dashboardRepository;

  ProfileNotifier(this._tokenStorage, this.dashboardRepository)
      : super(ProfileState()) {
    _loadProfileData();
  }

  /// 프로필 데이터 로드
  Future<void> _loadProfileData() async {
    state = state.copyWith(isLoading: true, clearError: true);

    try {
      // 사용자 정보 로드 (저장된 정보에서 가져오기)
      final userId = await _tokenStorage.getUserId();
      final email = await _tokenStorage.getUserEmail();
      final nickname = await _tokenStorage.getUserNickname();

      // 실제 API 호출로 학습 통계 가져오기
      final today = DateFormatter.todayYyMMdd();
      final startDate = DateFormatter.daysAgoYyMMdd(
        LearningConstants.defaultStatsPeriodDays,
      );

      final allDataResult = await dashboardRepository.getAttendanceByPeriod(startDate, today);
      final allData = allDataResult.dataOrNull;

      // 총 학습 시간 계산
      final totalLearningTime = DateFormatter.sumPlaytimes(
        allData?.periodData?.attendDates.map((e) => e.playtime).toList() ?? [],
      );
      final totalAttendDays = allData?.periodData?.totalAttendDays ?? 0;

      // 연속 출석 계산
      final consecutiveDays = DateFormatter.calculateConsecutiveDays(
        allData?.periodData?.attendDates.map((e) => DateTime.parse(e.attendDate)).toList() ?? [],
      );

      state = state.copyWith(
        isLoading: false,
        userId: userId,
        email: email,
        nickname: nickname,
        totalLearningTime: totalLearningTime,
        totalAttendDays: totalAttendDays,
        consecutiveDays: consecutiveDays,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: ErrorState.unknown('프로필 정보를 불러오는데 실패했습니다.'),
      );
    }
  }

  /// 새로고침
  Future<void> refresh() async {
    await _loadProfileData();
  }
}

/// 프로필 Provider
final profileProvider =
    StateNotifierProvider<ProfileNotifier, ProfileState>((ref) {
  final tokenStorage = ref.watch(tokenStorageProvider);
  final dashboardRepository = ref.watch(dashboardRepositoryProvider);
  return ProfileNotifier(tokenStorage, dashboardRepository);
});
