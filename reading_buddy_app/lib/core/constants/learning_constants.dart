/// 학습 관련 상수 정의
class LearningConstants {
  LearningConstants._();

  // 숙련도 임계값
  /// 스테이지 완료 숙련도 임계값 (70%)
  static const double masteryCompletionThreshold = 70.0;

  /// 스테이지 숙련도 우수 임계값 (80%)
  static const double masteryExcellentThreshold = 80.0;

  // 정답률 임계값
  /// 스테이지 정답률 우수 임계값 (80%)
  static const double correctRateExcellentThreshold = 80.0;

  /// 스테이지 정답률 보통 임계값 (60%)
  static const double correctRateGoodThreshold = 60.0;

  // 학습 시간 관련
  /// 기본 학습 목표 시간 (분)
  static const int dailyLearningGoalMinutes = 30;

  /// 주간 학습 목표 시간 (분)
  static const int weeklyLearningGoalMinutes = 150; // 5일 * 30분

  // 출석 관련
  /// 연속 출석 달성 목표 (일)
  static const int consecutiveDaysGoal = 7;

  /// 월간 출석 목표 (일)
  static const int monthlyAttendGoal = 20;

  // KC 관련
  /// KC 숙련도 완료 임계값 (70%)
  static const double kcMasteryThreshold = 0.7;

  /// KC 숙련도 우수 임계값 (85%)
  static const double kcMasteryExcellentThreshold = 0.85;

  // 추천 메시지
  /// 이전 스테이지 복습 권장 메시지
  static const String recommendReviewPrevious = '이전 단계를 먼저 완성해보세요!';

  /// 현재 스테이지 조금 더 연습 권장 메시지
  static const String recommendPracticeMore = '잘하셨어요! 조금만 더 연습하면 완성!';

  /// 다음 스테이지로 진행 권장 메시지
  static const String recommendNextStage = '완벽해요! 다음 단계로 가볼까요?';

  /// 다시 도전 권장 메시지
  static const String recommendRetry = '다시 도전해보세요!';

  /// 전체 완료 축하 메시지
  static const String congratsAllComplete = '모든 단계를 완료했어요! 🎉';

  // 데이터 조회 기간
  /// 기본 통계 조회 기간 (일) - 최근 1년
  static const int defaultStatsPeriodDays = 365;

  /// 연속 출석 계산 기간 (일) - 최근 30일
  static const int consecutiveDaysCheckPeriod = 30;

  /// 주간 통계 기간 (일)
  static const int weeklyStatsPeriod = 7;

  /// 월간 통계 기간 (일)
  static const int monthlyStatsPeriod = 30;
}
