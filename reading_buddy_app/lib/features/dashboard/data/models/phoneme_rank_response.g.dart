// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'phoneme_rank_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

PhonemeRankResponse _$PhonemeRankResponseFromJson(Map<String, dynamic> json) =>
    PhonemeRankResponse(
      phonemeId: (json['phonemeId'] as num).toInt(),
      value: json['value'] as String,
      category: json['category'] as String,
      wrongCnt: (json['wrongCnt'] as num?)?.toInt(),
      tryCnt: (json['tryCnt'] as num?)?.toInt(),
    );

Map<String, dynamic> _$PhonemeRankResponseToJson(
        PhonemeRankResponse instance) =>
    <String, dynamic>{
      'phonemeId': instance.phonemeId,
      'value': instance.value,
      'category': instance.category,
      'wrongCnt': instance.wrongCnt,
      'tryCnt': instance.tryCnt,
    };
