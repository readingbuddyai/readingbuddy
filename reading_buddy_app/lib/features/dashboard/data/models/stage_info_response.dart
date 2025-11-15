import 'package:json_annotation/json_annotation.dart';

part 'stage_info_response.g.dart';

@JsonSerializable()
class StageInfoResponse {
  final String? stage;
  final int? totalProblemCount;
  final int? correctProblemCount;
  final double? correctRate;

  StageInfoResponse({
    this.stage,
    this.totalProblemCount,
    this.correctProblemCount,
    this.correctRate,
  });

  factory StageInfoResponse.fromJson(Map<String, dynamic> json) =>
      _$StageInfoResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageInfoResponseToJson(this);
}
