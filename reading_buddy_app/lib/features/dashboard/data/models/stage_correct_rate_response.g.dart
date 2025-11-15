// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_correct_rate_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageCorrectRateResponse _$StageCorrectRateResponseFromJson(
        Map<String, dynamic> json) =>
    StageCorrectRateResponse(
      stage: json['stage'] as String,
      correctRate: (json['correctRate'] as num).toDouble(),
      correctCount: (json['correctCount'] as num).toInt(),
      wrongCount: (json['wrongCount'] as num).toInt(),
      totalProblems: (json['totalProblems'] as num).toInt(),
      completedAt: json['completedAt'] == null
          ? null
          : DateTime.parse(json['completedAt'] as String),
    );

Map<String, dynamic> _$StageCorrectRateResponseToJson(
        StageCorrectRateResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'correctRate': instance.correctRate,
      'correctCount': instance.correctCount,
      'wrongCount': instance.wrongCount,
      'totalProblems': instance.totalProblems,
      'completedAt': instance.completedAt?.toIso8601String(),
    };
