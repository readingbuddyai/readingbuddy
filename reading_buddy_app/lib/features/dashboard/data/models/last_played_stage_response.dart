import 'package:json_annotation/json_annotation.dart';

part 'last_played_stage_response.g.dart';

@JsonSerializable()
class LastPlayedStageResponse {
  final String? stage;
  final DateTime? playedAt;

  LastPlayedStageResponse({
    this.stage,
    this.playedAt,
  });

  factory LastPlayedStageResponse.fromJson(Map<String, dynamic> json) =>
      _$LastPlayedStageResponseFromJson(json);

  Map<String, dynamic> toJson() => _$LastPlayedStageResponseToJson(this);
}
