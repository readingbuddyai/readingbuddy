// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_info_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageInfoResponse _$StageInfoResponseFromJson(Map<String, dynamic> json) =>
    StageInfoResponse(
      stage: json['stage'] as String?,
      totalProblemCount: (json['totalProblemCount'] as num?)?.toInt(),
      correctProblemCount: (json['correctProblemCount'] as num?)?.toInt(),
      correctRate: (json['correctRate'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$StageInfoResponseToJson(StageInfoResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'totalProblemCount': instance.totalProblemCount,
      'correctProblemCount': instance.correctProblemCount,
      'correctRate': instance.correctRate,
    };
