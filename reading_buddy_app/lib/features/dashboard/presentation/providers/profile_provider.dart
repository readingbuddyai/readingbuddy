import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../../../../core/providers/providers.dart';
import '../../../../core/storage/token_storage.dart';

/// 프로필 화면 상태
class ProfileState {
  final bool isLoading;
  final String? errorMessage;

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
    this.errorMessage,
    this.userId,
    this.email,
    this.nickname,
    this.totalLearningTime = '0시간 0분',
    this.totalAttendDays = 0,
    this.consecutiveDays = 0,
  });

  ProfileState copyWith({
    bool? isLoading,
    String? errorMessage,
    int? userId,
    String? email,
    String? nickname,
    String? totalLearningTime,
    int? totalAttendDays,
    int? consecutiveDays,
  }) {
    return ProfileState(
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
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
    state = state.copyWith(isLoading: true, errorMessage: null);

    try {
      // 사용자 정보 로드 (저장된 정보에서 가져오기)
      final userId = await _tokenStorage.getUserId();
      final email = await _tokenStorage.getUserEmail();
      final nickname = await _tokenStorage.getUserNickname();

      // 실제 API 호출로 학습 통계 가져오기
      final now = DateTime.now();
      final today = '${now.year.toString().substring(2)}${now.month.toString().padLeft(2, '0')}${now.day.toString().padLeft(2, '0')}';

      // 전체 기간 데이터 (최근 1년)
      final oneYearAgo = now.subtract(const Duration(days: 365));
      final startDate = '${oneYearAgo.year.toString().substring(2)}${oneYearAgo.month.toString().padLeft(2, '0')}${oneYearAgo.day.toString().padLeft(2, '0')}';

      final allData = await dashboardRepository.getAttendanceByPeriod(startDate, today);

      // 총 학습 시간 계산
      final totalLearningTime = _calculateTotalPlaytime(allData?.periodData?.attendDates ?? []);
      final totalAttendDays = allData?.periodData?.totalAttendDays ?? 0;

      // 연속 출석 계산
      final consecutiveDays = _calculateConsecutiveDays(allData?.periodData?.attendDates ?? []);

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
        errorMessage: '프로필 정보를 불러오는데 실패했습니다.',
      );
    }
  }

  /// 총 학습 시간 계산
  String _calculateTotalPlaytime(List attendDates) {
    int totalMinutes = 0;
    int totalSeconds = 0;

    for (final item in attendDates) {
      final parts = item.playtime.split(':');
      if (parts.length == 2) {
        totalMinutes += int.tryParse(parts[0]) ?? 0;
        totalSeconds += int.tryParse(parts[1]) ?? 0;
      }
    }

    totalMinutes += totalSeconds ~/ 60;
    totalSeconds = totalSeconds % 60;

    final hours = totalMinutes ~/ 60;
    final minutes = totalMinutes % 60;

    if (hours > 0) {
      return '$hours시간 $minutes분';
    } else {
      return '$minutes분';
    }
  }

  /// 연속 출석 일수 계산
  int _calculateConsecutiveDays(List attendDates) {
    if (attendDates.isEmpty) return 0;

    final dates = attendDates
        .map((item) => DateTime.parse(item.attendDate))
        .toList()
      ..sort((a, b) => b.compareTo(a));

    int consecutive = 1;
    final today = DateTime.now();

    if (dates.first.day != today.day ||
        dates.first.month != today.month ||
        dates.first.year != today.year) {
      return 0;
    }

    for (int i = 0; i < dates.length - 1; i++) {
      final diff = dates[i].difference(dates[i + 1]).inDays;
      if (diff == 1) {
        consecutive++;
      } else {
        break;
      }
    }

    return consecutive;
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
