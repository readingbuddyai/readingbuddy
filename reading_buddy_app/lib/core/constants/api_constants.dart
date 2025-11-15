/// API 관련 상수 정의
class ApiConstants {
  // Base URL - Reading Buddy AI 배포 서버
  static const String baseUrl = 'https://readingbuddyai.co.kr';
  // Timeout
  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);

  // Auth Endpoints
  static const String login = '/api/user/login';
  static const String signup = '/api/user/signup';
  static const String reissueToken = '/api/user/reissue-token';
  static const String activation = '/api/user/activation';
  static const String authDevice = '/api/user/auth-device';
  static const String polling = '/api/user/polling';
  static const String attend = '/api/user/attend';

  // Dashboard Endpoints
  static const String stageInfo = '/api/dashboard/stage/info';
  static const String stageTryAvg = '/api/dashboard/stage/try-avg';
  static const String stageCorrectRate = '/api/dashboard/stage/correct-rate';
  static const String attendance = '/api/dashboard/attendance';
  static const String mistakePhonemesRank = '/api/dashboard/mistake/phonemes/rank';
  static const String tryPhonemesRank = '/api/dashboard/try/phonemes/rank';
  static const String kcMasteryTrend = '/api/dashboard/kc/mastery-trend';
  static const String stageMastery = '/api/dashboard/stage/mastery';
  static const String stageKcMasteryTrend = '/api/dashboard/stage/kc-mastery-trend';
  static const String allKcMastery = '/api/dashboard/kc/all-mastery';
  static const String practiceList = '/api/dashboard/practice/list';

  // Train Endpoints
  static const String lastStage = '/api/train/last/stage';

  // Headers
  static const String contentType = 'application/json';
  static const String authorization = 'Authorization';
  static const String bearer = 'Bearer';
}
