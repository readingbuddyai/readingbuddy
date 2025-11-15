import 'package:json_annotation/json_annotation.dart';

part 'all_kc_mastery_response.g.dart';

/// 모든 KC 평균 숙련도 응답 모델
@JsonSerializable()
class AllKcAverageMasteryResponse {
  final int? totalKcCount;
  final double? overallAverageMastery;
  final List<KcMasteryInfo>? kcMasteries;

  AllKcAverageMasteryResponse({
    this.totalKcCount,
    this.overallAverageMastery,
    this.kcMasteries,
  });

  factory AllKcAverageMasteryResponse.fromJson(Map<String, dynamic> json) =>
      _$AllKcAverageMasteryResponseFromJson(json);

  Map<String, dynamic> toJson() => _$AllKcAverageMasteryResponseToJson(this);
}

/// KC 숙련도 정보
@JsonSerializable()
class KcMasteryInfo {
  final int? kcId;
  final String? kcCategory;
  final String? kcDescription;
  final String? stage;

  @JsonKey(name: 'plearn')
  final double? pLearn;

  @JsonKey(name: 'ptrain')
  final double? pTrain;

  @JsonKey(name: 'pguess')
  final double? pGuess;

  @JsonKey(name: 'pslip')
  final double? pSlip;

  final DateTime? updatedAt;

  KcMasteryInfo({
    this.kcId,
    this.kcCategory,
    this.kcDescription,
    this.stage,
    this.pLearn,
    this.pTrain,
    this.pGuess,
    this.pSlip,
    this.updatedAt,
  });

  factory KcMasteryInfo.fromJson(Map<String, dynamic> json) =>
      _$KcMasteryInfoFromJson(json);

  Map<String, dynamic> toJson() => _$KcMasteryInfoToJson(this);
}
