import 'package:json_annotation/json_annotation.dart';

part 'stage_correct_rate_response.g.dart';

@JsonSerializable()
class StageCorrectRateResponse {
  final String stage;
  final double correctRate;
  final int correctCount;
  final int wrongCount;
  final int totalProblems;
  final DateTime? completedAt;

  StageCorrectRateResponse({
    required this.stage,
    required this.correctRate,
    required this.correctCount,
    required this.wrongCount,
    required this.totalProblems,
    this.completedAt,
  });

  factory StageCorrectRateResponse.fromJson(Map<String, dynamic> json) =>
      _$StageCorrectRateResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageCorrectRateResponseToJson(this);
}
