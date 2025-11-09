import 'package:json_annotation/json_annotation.dart';

part 'stage_info_response.g.dart';

@JsonSerializable()
class StageInfoResponse {
  final String stage;
  final int totalTryCount;
  final int totalCorrectCount;
  final int totalWrongCount;

  StageInfoResponse({
    required this.stage,
    required this.totalTryCount,
    required this.totalCorrectCount,
    required this.totalWrongCount,
  });

  factory StageInfoResponse.fromJson(Map<String, dynamic> json) =>
      _$StageInfoResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageInfoResponseToJson(this);

  /// 정답률 계산
  double get correctRate {
    final total = totalCorrectCount + totalWrongCount;
    if (total == 0) return 0.0;
    return (totalCorrectCount / total) * 100.0;
  }
}
