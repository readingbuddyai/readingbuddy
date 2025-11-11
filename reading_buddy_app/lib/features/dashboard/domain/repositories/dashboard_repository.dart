import '../../data/models/stage_info_response.dart';
import '../../data/models/stage_try_avg_response.dart';
import '../../data/models/stage_correct_rate_response.dart';
import '../../data/models/stage_mastery_response.dart';
import '../../data/models/attendance_response.dart';
import '../../data/models/phoneme_rank_response.dart';
import '../../data/models/last_played_stage_response.dart';

/// Dashboard Repository 인터페이스
abstract class DashboardRepository {
  /// 스테이지 통계 정보
  Future<StageInfoResponse?> getStageInfo(String stage);

  /// 스테이지 평균 시도 횟수
  Future<StageTryAvgResponse?> getStageTryAvg(String stage);

  /// 스테이지 정답률
  Future<StageCorrectRateResponse?> getStageCorrectRate(String stage);

  /// 출석 기록 조회 (기간별)
  Future<AttendanceResponse?> getAttendanceByPeriod(
    String startDate,
    String endDate,
  );

  /// 출석 기록 조회 (일별)
  Future<AttendanceResponse?> getAttendanceByDate(String date);

  /// 틀린 음소 랭킹
  Future<List<PhonemeRankResponse>> getWrongPhonemesRank(int limit);

  /// 시도 많은 음소 랭킹
  Future<List<PhonemeRankResponse>> getTryPhonemesRank(int limit);

  /// 스테이지별 숙련도 조회
  Future<StageMasteryResponse?> getStageMastery(String stage);

  /// 마지막으로 플레이한 스테이지 조회
  Future<LastPlayedStageResponse?> getLastPlayedStage();
}
