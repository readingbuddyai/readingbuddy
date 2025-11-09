// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_info_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageInfoResponse _$StageInfoResponseFromJson(Map<String, dynamic> json) =>
    StageInfoResponse(
      stage: json['stage'] as String,
      totalTryCount: (json['totalTryCount'] as num).toInt(),
      totalCorrectCount: (json['totalCorrectCount'] as num).toInt(),
      totalWrongCount: (json['totalWrongCount'] as num).toInt(),
    );

Map<String, dynamic> _$StageInfoResponseToJson(StageInfoResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'totalTryCount': instance.totalTryCount,
      'totalCorrectCount': instance.totalCorrectCount,
      'totalWrongCount': instance.totalWrongCount,
    };
