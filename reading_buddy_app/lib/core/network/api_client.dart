import 'package:dio/dio.dart';
import 'package:retrofit/retrofit.dart';
import '../constants/api_constants.dart';
import '../../features/auth/data/models/login_request.dart';
import '../../features/auth/data/models/signup_request.dart';
import '../../features/auth/data/models/token_response.dart';
import '../../features/auth/data/models/device_code_request.dart';
import '../../features/auth/data/models/device_login_response.dart';
import '../../features/auth/data/models/api_response.dart';
import '../../features/auth/data/models/empty_response.dart';
import '../../features/dashboard/data/models/stage_info_response.dart';
import '../../features/dashboard/data/models/stage_try_avg_response.dart';
import '../../features/dashboard/data/models/stage_correct_rate_response.dart';
import '../../features/dashboard/data/models/stage_mastery_response.dart';
import '../../features/dashboard/data/models/attendance_response.dart';
import '../../features/dashboard/data/models/phoneme_rank_response.dart';
import '../../features/dashboard/data/models/last_played_stage_response.dart';

part 'api_client.g.dart';

@RestApi(baseUrl: ApiConstants.baseUrl)
abstract class ApiClient {
  factory ApiClient(Dio dio, {String baseUrl}) = _ApiClient;

  // ==================== Auth APIs ====================

  /// 로그인
  @POST(ApiConstants.login)
  Future<ApiResponse<TokenResponse>> login(@Body() LoginRequest request);

  /// 회원가입
  @POST(ApiConstants.signup)
  Future<ApiResponse<EmptyResponse>> signup(@Body() SignupRequest request);

  /// 토큰 재발급
  @POST(ApiConstants.reissueToken)
  Future<ApiResponse<TokenResponse>> reissueToken(
    @Body() Map<String, dynamic> request,
  );

  /// 출석 체크
  @POST(ApiConstants.attend)
  Future<ApiResponse<EmptyResponse>> checkAttendance();

  /// Device Code 생성 (VR용)
  @GET(ApiConstants.activation)
  Future<ApiResponse<DeviceLoginResponse>> getActivationCode();

  /// Device Code 인증 (앱용)
  @POST(ApiConstants.authDevice)
  Future<ApiResponse<EmptyResponse>> authorizeDevice(
    @Body() DeviceCodeRequest request,
  );

  /// Device Polling (VR용 - 앱에서는 사용 안 함)
  @POST(ApiConstants.polling)
  Future<ApiResponse<TokenResponse>> pollDeviceAuth(
    @Body() DeviceCodeRequest request,
  );

  // ==================== Dashboard APIs ====================

  /// 스테이지 통계 정보
  @GET(ApiConstants.stageInfo)
  Future<ApiResponse<StageInfoResponse>> getStageInfo(
    @Query('stage') String stage,
  );

  /// 스테이지 평균 시도 횟수
  @GET(ApiConstants.stageTryAvg)
  Future<ApiResponse<StageTryAvgResponse>> getStageTryAvg(
    @Query('stage') String stage,
  );

  /// 스테이지 정답률
  @GET(ApiConstants.stageCorrectRate)
  Future<ApiResponse<StageCorrectRateResponse>> getStageCorrectRate(
    @Query('stage') String stage,
  );

  /// 출석 기록 조회 (기간별)
  @GET(ApiConstants.attendance)
  Future<ApiResponse<AttendanceResponse>> getAttendanceByPeriod(
    @Query('startdate') String startDate,
    @Query('enddate') String endDate,
  );

  /// 출석 기록 조회 (일별)
  @GET(ApiConstants.attendance)
  Future<ApiResponse<AttendanceResponse>> getAttendanceByDate(
    @Query('date') String date,
  );

  /// 틀린 음소 랭킹
  @GET(ApiConstants.mistakePhonemesRank)
  Future<ApiResponse<List<PhonemeRankResponse>>> getWrongPhonemesRank(
    @Query('limit') int limit,
  );

  /// 시도 많은 음소 랭킹
  @GET(ApiConstants.tryPhonemesRank)
  Future<ApiResponse<List<PhonemeRankResponse>>> getTryPhonemesRank(
    @Query('limit') int limit,
  );

  /// KC 숙련도 변화 추이 조회
  @GET(ApiConstants.kcMasteryTrend)
  Future<HttpResponse<dynamic>> getKcMasteryTrend(
    @Query('kcId') int kcId,
    @Query('startdate') String? startDate,
    @Query('enddate') String? endDate,
  );

  /// Stage별 현재 숙련도 조회
  @GET(ApiConstants.stageMastery)
  Future<ApiResponse<StageMasteryResponse>> getStageMastery(
    @Query('stage') String stage,
    @Query('startdate') String? startDate,
    @Query('enddate') String? endDate,
  );

  // ==================== Train APIs ====================

  /// 마지막으로 플레이한 스테이지 조회
  @GET(ApiConstants.lastStage)
  Future<ApiResponse<LastPlayedStageResponse>> getLastPlayedStage();
}
