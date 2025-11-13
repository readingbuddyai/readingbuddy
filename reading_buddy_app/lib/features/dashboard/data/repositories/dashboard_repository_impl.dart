import 'package:dio/dio.dart';
import 'package:logger/logger.dart';
import '../../../../core/utils/result.dart';
import '../../../auth/data/models/api_response.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../models/stage_info_response.dart';
import '../models/stage_try_avg_response.dart';
import '../models/stage_correct_rate_response.dart';
import '../models/stage_mastery_response.dart';
import '../models/attendance_response.dart';
import '../models/phoneme_rank_response.dart';
import '../models/last_played_stage_response.dart';
import '../models/practice_list_response.dart';
import '../models/all_kc_mastery_response.dart';
import '../models/stage_kc_mastery_trend_response.dart';
import '../../../../core/network/api_client.dart';

/// Dashboard Repository 구현체
class DashboardRepositoryImpl implements DashboardRepository {
  final ApiClient _apiClient;
  final Logger _logger = Logger();

  DashboardRepositoryImpl(this._apiClient);

  /// API 응답을 Result로 변환하는 헬퍼 메서드
  Result<T> _handleResponse<T>(
    ApiResponse<T> response,
    String errorMessage,
  ) {
    if (response.isSuccess) {
      if (response.data != null) {
        return Success(response.data!);
      } else {
        _logger.w('$errorMessage: success는 true이지만 data가 null');
        return const Failure(
          '데이터가 없습니다',
          type: ErrorType.notFound,
        );
      }
    } else {
      return Failure(
        response.message ?? errorMessage,
        type: ErrorType.server,
      );
    }
  }

  /// DioException을 Result로 변환하는 헬퍼 메서드
  Failure<T> _handleDioException<T>(DioException e, String context) {
    _logger.e('$context 실패: ${e.message}');

    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.sendTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return Failure(
        '네트워크 연결 시간이 초과되었습니다',
        type: ErrorType.network,
      );
    }

    if (e.type == DioExceptionType.connectionError) {
      return Failure(
        '네트워크 연결을 확인해주세요',
        type: ErrorType.network,
      );
    }

    final statusCode = e.response?.statusCode;

    switch (statusCode) {
      case 401:
        return Failure(
          '인증이 만료되었습니다. 다시 로그인해주세요',
          statusCode: 401,
          type: ErrorType.auth,
        );
      case 404:
        return Failure(
          '데이터를 찾을 수 없습니다',
          statusCode: 404,
          type: ErrorType.notFound,
        );
      case 400:
        return Failure(
          '잘못된 요청입니다',
          statusCode: 400,
          type: ErrorType.badRequest,
        );
      case 500:
      case 502:
      case 503:
        return Failure(
          '서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요',
          statusCode: statusCode,
          type: ErrorType.server,
        );
      default:
        return Failure(
          e.message ?? '알 수 없는 오류가 발생했습니다',
          statusCode: statusCode,
          type: ErrorType.unknown,
        );
    }
  }

  /// 일반 Exception을 Result로 변환하는 헬퍼 메서드
  Failure<T> _handleException<T>(Object e, String context) {
    _logger.e('$context 실패: $e');
    return Failure(
      '데이터 처리 중 오류가 발생했습니다',
      type: ErrorType.parse,
    );
  }

  @override
  Future<Result<StageInfoResponse>> getStageInfo(String stage) async {
    try {
      final response = await _apiClient.getStageInfo(stage);
      return _handleResponse(response, '스테이지 정보 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '스테이지 정보 조회');
    } catch (e) {
      return _handleException(e, '스테이지 정보 조회');
    }
  }

  @override
  Future<Result<StageTryAvgResponse>> getStageTryAvg(String stage) async {
    try {
      final response = await _apiClient.getStageTryAvg(stage);
      return _handleResponse(response, '스테이지 평균 시도 횟수 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '스테이지 평균 시도 횟수 조회');
    } catch (e) {
      return _handleException(e, '스테이지 평균 시도 횟수 조회');
    }
  }

  @override
  Future<Result<StageCorrectRateResponse>> getStageCorrectRate(String stage) async {
    try {
      final response = await _apiClient.getStageCorrectRate(stage);
      return _handleResponse(response, '스테이지 정답률 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '스테이지 정답률 조회');
    } catch (e) {
      return _handleException(e, '스테이지 정답률 조회');
    }
  }

  @override
  Future<Result<AttendanceResponse>> getAttendanceByPeriod(
    String startDate,
    String endDate,
  ) async {
    try {
      _logger.d('=== Attendance Period Request ===');
      _logger.d('Start Date: $startDate, End Date: $endDate');

      final response =
          await _apiClient.getAttendanceByPeriod(startDate, endDate);

      _logger.d('=== Attendance Period Response ===');
      _logger.d('Success: ${response.isSuccess}');
      _logger.d('Data: ${response.data}');

      if (response.isSuccess && response.data != null) {
        _logger.d('Attend Dates Count: ${response.data?.periodData?.attendDates?.length ?? 0}');
        final attendDates = response.data?.periodData?.attendDates;
        if (attendDates != null) {
          for (var date in attendDates) {
            _logger.d('  - ${date.attendDate}: ${date.playtime}');
          }
        }
      }

      return _handleResponse(response, '출석 기록 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '출석 기록 조회');
    } catch (e) {
      return _handleException(e, '출석 기록 조회');
    }
  }

  @override
  Future<Result<AttendanceResponse>> getAttendanceByDate(String date) async {
    try {
      final response = await _apiClient.getAttendanceByDate(date);
      return _handleResponse(response, '일별 출석 기록 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '일별 출석 기록 조회');
    } catch (e) {
      return _handleException(e, '일별 출석 기록 조회');
    }
  }

  @override
  Future<Result<List<PhonemeRankResponse>>> getWrongPhonemesRank(int limit) async {
    try {
      final response = await _apiClient.getWrongPhonemesRank(limit);
      return _handleResponse(response, '틀린 음소 랭킹 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '틀린 음소 랭킹 조회');
    } catch (e) {
      return _handleException(e, '틀린 음소 랭킹 조회');
    }
  }

  @override
  Future<Result<List<PhonemeRankResponse>>> getTryPhonemesRank(int limit) async {
    try {
      final response = await _apiClient.getTryPhonemesRank(limit);
      return _handleResponse(response, '시도 많은 음소 랭킹 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '시도 많은 음소 랭킹 조회');
    } catch (e) {
      return _handleException(e, '시도 많은 음소 랭킹 조회');
    }
  }

  @override
  Future<Result<StageMasteryResponse>> getStageMastery(String stage) async {
    try {
      final response = await _apiClient.getStageMastery(stage, null, null);

      // KC 데이터가 없는 스테이지 목록 (정상)
      const noKcStages = ['2', '1.1', '1.2'];

      if (response.isSuccess && response.data != null) {
        return Success(response.data!);
      } else if (noKcStages.contains(stage)) {
        // KC 데이터가 없는 스테이지는 notFound 에러로 처리 (정상 케이스)
        _logger.d('스테이지 $stage는 KC 데이터가 없는 스테이지입니다 (정상)');
        return Failure(
          '해당 스테이지는 KC 데이터가 없습니다',
          type: ErrorType.notFound,
        );
      } else {
        return _handleResponse(response, '스테이지 숙련도 조회 실패');
      }
    } on DioException catch (e) {
      return _handleDioException(e, '스테이지 숙련도 조회');
    } catch (e) {
      return _handleException(e, '스테이지 숙련도 조회');
    }
  }

  @override
  Future<Result<AllKcAverageMasteryResponse>> getAllKcAverageMastery() async {
    try {
      final response = await _apiClient.getAllKcAverageMastery();
      return _handleResponse(response, '모든 KC 평균 숙련도 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '모든 KC 평균 숙련도 조회');
    } catch (e) {
      return _handleException(e, '모든 KC 평균 숙련도 조회');
    }
  }

  @override
  Future<Result<StageKcMasteryTrendResponse>> getStageKcMasteryTrend(
    String stage,
    String? startDate,
    String? endDate,
  ) async {
    try {
      final response = await _apiClient.getStageKcMasteryTrend(stage, startDate, endDate);
      return _handleResponse(response, 'Stage KC 숙련도 변화 추이 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, 'Stage KC 숙련도 변화 추이 조회');
    } catch (e) {
      return _handleException(e, 'Stage KC 숙련도 변화 추이 조회');
    }
  }

  @override
  Future<Result<LastPlayedStageResponse>> getLastPlayedStage() async {
    try {
      final response = await _apiClient.getLastPlayedStage();
      return _handleResponse(response, '마지막 플레이 스테이지 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '마지막 플레이 스테이지 조회');
    } catch (e) {
      return _handleException(e, '마지막 플레이 스테이지 조회');
    }
  }

  @override
  Future<Result<PracticeListResponse>> getPracticeList(String date) async {
    try {
      final response = await _apiClient.getPracticeList(date);

      if (response.isSuccess && response.data != null) {
        // 디버깅: 응답 데이터 로그
        _logger.d('=== Practice List Response ===');
        _logger.d('Date: ${response.data!.date}');
        _logger.d('Session count: ${response.data!.session.length}');
        for (var session in response.data!.session) {
          _logger.d('Session - Stage: ${session.stage}');
          for (var problem in session.problems) {
            _logger.d('  Problem ${problem.problemNumber}: "${problem.problem}" (isCorrect: ${problem.isCorrect})');
          }
        }
      }

      return _handleResponse(response, '일별 학습 기록 조회 실패');
    } on DioException catch (e) {
      return _handleDioException(e, '일별 학습 기록 조회');
    } catch (e) {
      return _handleException(e, '일별 학습 기록 조회');
    }
  }
}
