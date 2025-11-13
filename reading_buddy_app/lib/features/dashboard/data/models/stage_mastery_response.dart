import 'package:json_annotation/json_annotation.dart';

part 'stage_mastery_response.g.dart';

@JsonSerializable()
class StageMasteryResponse {
  final String? stage;
  final List<KcMastery>? kcMasteries;
  final double? averageMastery;

  StageMasteryResponse({
    this.stage,
    this.kcMasteries,
    this.averageMastery,
  });

  factory StageMasteryResponse.fromJson(Map<String, dynamic> json) =>
      _$StageMasteryResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageMasteryResponseToJson(this);

  /// 숙련도를 퍼센트로 변환 (0-100)
  double get masteryPercent => (averageMastery ?? 0.0) * 100.0;
}

@JsonSerializable()
class KcMastery {
  final int? kcId;
  final String? kcCategory;
  final double? pLearn;
  final double? pTrain;
  final double? pGuess;
  final double? pSlip;
  final DateTime? updatedAt;

  KcMastery({
    this.kcId,
    this.kcCategory,
    this.pLearn,
    this.pTrain,
    this.pGuess,
    this.pSlip,
    this.updatedAt,
  });

  factory KcMastery.fromJson(Map<String, dynamic> json) =>
      _$KcMasteryFromJson(json);

  Map<String, dynamic> toJson() => _$KcMasteryToJson(this);
}
