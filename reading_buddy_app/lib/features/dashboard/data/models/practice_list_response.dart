import 'package:json_annotation/json_annotation.dart';

part 'practice_list_response.g.dart';

/// 일별 학습 기록 응답
@JsonSerializable()
class PracticeListResponse {
  final String date;
  final List<SessionInfo> session;

  PracticeListResponse({
    required this.date,
    required this.session,
  });

  factory PracticeListResponse.fromJson(Map<String, dynamic> json) =>
      _$PracticeListResponseFromJson(json);

  Map<String, dynamic> toJson() => _$PracticeListResponseToJson(this);
}

/// 세션 정보 (하나의 게임 플레이)
@JsonSerializable()
class SessionInfo {
  final int trainedStageHistoryId;
  final String stage;
  final DateTime startedAt;
  final int totalCount;
  final int correctCount;
  final int wrongCount;
  final List<ProblemInfo> problems;

  SessionInfo({
    required this.trainedStageHistoryId,
    required this.stage,
    required this.startedAt,
    required this.totalCount,
    required this.correctCount,
    required this.wrongCount,
    required this.problems,
  });

  factory SessionInfo.fromJson(Map<String, dynamic> json) =>
      _$SessionInfoFromJson(json);

  Map<String, dynamic> toJson() => _$SessionInfoToJson(this);

  /// 정답률 계산
  double get correctRate {
    if (totalCount == 0) return 0.0;
    return (correctCount / totalCount) * 100.0;
  }
}

/// 문제 정보
@JsonSerializable()
class ProblemInfo {
  final int problemId;
  final int problemNumber;
  final String problem;
  final String answer;
  final bool isCorrect;
  final bool? isReplyCorrect;
  final int attemptNumber;
  final String? audioUrl;
  final DateTime solvedAt;

  ProblemInfo({
    required this.problemId,
    required this.problemNumber,
    required this.problem,
    required this.answer,
    required this.isCorrect,
    this.isReplyCorrect,
    required this.attemptNumber,
    this.audioUrl,
    required this.solvedAt,
  });

  factory ProblemInfo.fromJson(Map<String, dynamic> json) =>
      _$ProblemInfoFromJson(json);

  Map<String, dynamic> toJson() => _$ProblemInfoToJson(this);
}
