// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'last_played_stage_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

LastPlayedStageResponse _$LastPlayedStageResponseFromJson(
        Map<String, dynamic> json) =>
    LastPlayedStageResponse(
      stage: json['stage'] as String?,
      playedAt: json['playedAt'] == null
          ? null
          : DateTime.parse(json['playedAt'] as String),
    );

Map<String, dynamic> _$LastPlayedStageResponseToJson(
        LastPlayedStageResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'playedAt': instance.playedAt?.toIso8601String(),
    };
