// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'all_kc_mastery_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AllKcAverageMasteryResponse _$AllKcAverageMasteryResponseFromJson(
        Map<String, dynamic> json) =>
    AllKcAverageMasteryResponse(
      totalKcCount: (json['totalKcCount'] as num?)?.toInt(),
      overallAverageMastery:
          (json['overallAverageMastery'] as num?)?.toDouble(),
      kcMasteries: (json['kcMasteries'] as List<dynamic>?)
          ?.map((e) => KcMasteryInfo.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$AllKcAverageMasteryResponseToJson(
        AllKcAverageMasteryResponse instance) =>
    <String, dynamic>{
      'totalKcCount': instance.totalKcCount,
      'overallAverageMastery': instance.overallAverageMastery,
      'kcMasteries': instance.kcMasteries,
    };

KcMasteryInfo _$KcMasteryInfoFromJson(Map<String, dynamic> json) =>
    KcMasteryInfo(
      kcId: (json['kcId'] as num?)?.toInt(),
      kcCategory: json['kcCategory'] as String?,
      kcDescription: json['kcDescription'] as String?,
      stage: json['stage'] as String?,
      pLearn: (json['plearn'] as num?)?.toDouble(),
      pTrain: (json['ptrain'] as num?)?.toDouble(),
      pGuess: (json['pguess'] as num?)?.toDouble(),
      pSlip: (json['pslip'] as num?)?.toDouble(),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$KcMasteryInfoToJson(KcMasteryInfo instance) =>
    <String, dynamic>{
      'kcId': instance.kcId,
      'kcCategory': instance.kcCategory,
      'kcDescription': instance.kcDescription,
      'stage': instance.stage,
      'plearn': instance.pLearn,
      'ptrain': instance.pTrain,
      'pguess': instance.pGuess,
      'pslip': instance.pSlip,
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };
