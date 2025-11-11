// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stage_mastery_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

StageMasteryResponse _$StageMasteryResponseFromJson(
        Map<String, dynamic> json) =>
    StageMasteryResponse(
      stage: json['stage'] as String?,
      kcMasteries: (json['kcMasteries'] as List<dynamic>?)
          ?.map((e) => KcMastery.fromJson(e as Map<String, dynamic>))
          .toList(),
      averageMastery: (json['averageMastery'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$StageMasteryResponseToJson(
        StageMasteryResponse instance) =>
    <String, dynamic>{
      'stage': instance.stage,
      'kcMasteries': instance.kcMasteries,
      'averageMastery': instance.averageMastery,
    };

KcMastery _$KcMasteryFromJson(Map<String, dynamic> json) => KcMastery(
      kcId: (json['kcId'] as num?)?.toInt(),
      kcCategory: json['kcCategory'] as String?,
      pLearn: (json['pLearn'] as num?)?.toDouble(),
      pTrain: (json['pTrain'] as num?)?.toDouble(),
      pGuess: (json['pGuess'] as num?)?.toDouble(),
      pSlip: (json['pSlip'] as num?)?.toDouble(),
      updatedAt: json['updatedAt'] as String?,
    );

Map<String, dynamic> _$KcMasteryToJson(KcMastery instance) => <String, dynamic>{
      'kcId': instance.kcId,
      'kcCategory': instance.kcCategory,
      'pLearn': instance.pLearn,
      'pTrain': instance.pTrain,
      'pGuess': instance.pGuess,
      'pSlip': instance.pSlip,
      'updatedAt': instance.updatedAt,
    };
