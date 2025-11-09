import 'package:json_annotation/json_annotation.dart';

part 'stage_try_avg_response.g.dart';

@JsonSerializable()
class StageTryAvgResponse {
  final String stage;
  final double averageTryCount;
  final int totalSessions;

  StageTryAvgResponse({
    required this.stage,
    required this.averageTryCount,
    required this.totalSessions,
  });

  factory StageTryAvgResponse.fromJson(Map<String, dynamic> json) =>
      _$StageTryAvgResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageTryAvgResponseToJson(this);
}
