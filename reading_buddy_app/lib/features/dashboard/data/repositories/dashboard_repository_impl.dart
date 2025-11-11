import 'package:logger/logger.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../models/stage_info_response.dart';
import '../models/stage_try_avg_response.dart';
import '../models/stage_correct_rate_response.dart';
import '../models/stage_mastery_response.dart';
import '../models/attendance_response.dart';
import '../models/phoneme_rank_response.dart';
import '../models/last_played_stage_response.dart';
import '../../../../core/network/api_client.dart';

/// Dashboard Repository 구현체
class DashboardRepositoryImpl implements DashboardRepository {
  final ApiClient _apiClient;
  final Logger _logger = Logger();

  DashboardRepositoryImpl(this._apiClient);

  @override
  Future<StageInfoResponse?> getStageInfo(String stage) async {
    try {
      final response = await _apiClient.getStageInfo(stage);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('스테이지 정보 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<StageTryAvgResponse?> getStageTryAvg(String stage) async {
    try {
      final response = await _apiClient.getStageTryAvg(stage);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('스테이지 평균 시도 횟수 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<StageCorrectRateResponse?> getStageCorrectRate(String stage) async {
    try {
      final response = await _apiClient.getStageCorrectRate(stage);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('스테이지 정답률 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<AttendanceResponse?> getAttendanceByPeriod(
    String startDate,
    String endDate,
  ) async {
    try {
      final response =
          await _apiClient.getAttendanceByPeriod(startDate, endDate);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('출석 기록 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<AttendanceResponse?> getAttendanceByDate(String date) async {
    try {
      final response = await _apiClient.getAttendanceByDate(date);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('일별 출석 기록 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<List<PhonemeRankResponse>> getWrongPhonemesRank(int limit) async {
    try {
      final response = await _apiClient.getWrongPhonemesRank(limit);
      if (response.isSuccess && response.data != null) {
        return response.data!;
      }
      return [];
    } catch (e) {
      _logger.e('틀린 음소 랭킹 조회 실패: $e');
      return [];
    }
  }

  @override
  Future<List<PhonemeRankResponse>> getTryPhonemesRank(int limit) async {
    try {
      final response = await _apiClient.getTryPhonemesRank(limit);
      if (response.isSuccess && response.data != null) {
        return response.data!;
      }
      return [];
    } catch (e) {
      _logger.e('시도 많은 음소 랭킹 조회 실패: $e');
      return [];
    }
  }

  @override
  Future<StageMasteryResponse?> getStageMastery(String stage) async {
    try {
      final response = await _apiClient.getStageMastery(stage, null, null);
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('스테이지 숙련도 조회 실패: $e');
      return null;
    }
  }

  @override
  Future<LastPlayedStageResponse?> getLastPlayedStage() async {
    try {
      final response = await _apiClient.getLastPlayedStage();
      if (response.isSuccess && response.data != null) {
        return response.data;
      }
      return null;
    } catch (e) {
      _logger.e('마지막 플레이 스테이지 조회 실패: $e');
      return null;
    }
  }
}
