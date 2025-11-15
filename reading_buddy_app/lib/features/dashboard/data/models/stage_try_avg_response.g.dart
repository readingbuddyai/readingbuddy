// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_try_avg_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageTryAvgResponse _$StageTryAvgResponseFromJson(Map<String, dynamic> json) =>
    StageTryAvgResponse(
      stage: json['stage'] as String,
      averageTryCount: (json['averageTryCount'] as num).toDouble(),
      totalSessions: (json['totalSessions'] as num).toInt(),
    );

Map<String, dynamic> _$StageTryAvgResponseToJson(
        StageTryAvgResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'averageTryCount': instance.averageTryCount,
      'totalSessions': instance.totalSessions,
    };
