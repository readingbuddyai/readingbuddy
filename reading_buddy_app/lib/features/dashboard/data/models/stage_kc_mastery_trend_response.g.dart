// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_kc_mastery_trend_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageKcMasteryTrendResponse _$StageKcMasteryTrendResponseFromJson(
        Map<String, dynamic> json) =>
    StageKcMasteryTrendResponse(
      stage: json['stage'] as String?,
      kcTrends: (json['kcTrends'] as List<dynamic>?)
          ?.map((e) => KcTrend.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$StageKcMasteryTrendResponseToJson(
        StageKcMasteryTrendResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'kcTrends': instance.kcTrends,
    };

KcTrend _$KcTrendFromJson(Map<String, dynamic> json) => KcTrend(
      kcId: (json['kcId'] as num?)?.toInt(),
      kcCategory: json['kcCategory'] as String?,
      kcDescription: json['kcDescription'] as String?,
      initialMastery: json['initialMastery'] == null
          ? null
          : MasteryPoint.fromJson(
              json['initialMastery'] as Map<String, dynamic>),
      masteryTrend: (json['masteryTrend'] as List<dynamic>?)
          ?.map((e) => MasteryPoint.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$KcTrendToJson(KcTrend instance) => <String, dynamic>{
      'kcId': instance.kcId,
      'kcCategory': instance.kcCategory,
      'kcDescription': instance.kcDescription,
      'initialMastery': instance.initialMastery,
      'masteryTrend': instance.masteryTrend,
    };

MasteryPoint _$MasteryPointFromJson(Map<String, dynamic> json) => MasteryPoint(
      pLearn: (json['plearn'] as num?)?.toDouble(),
      pTrain: (json['ptrain'] as num?)?.toDouble(),
      pGuess: (json['pguess'] as num?)?.toDouble(),
      pSlip: (json['pslip'] as num?)?.toDouble(),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$MasteryPointToJson(MasteryPoint instance) =>
    <String, dynamic>{
      'plearn': instance.pLearn,
      'ptrain': instance.pTrain,
      'pguess': instance.pGuess,
      'pslip': instance.pSlip,
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };
