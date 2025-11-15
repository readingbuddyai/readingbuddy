import 'package:json_annotation/json_annotation.dart';

part 'stage_kc_mastery_trend_response.g.dart';

/// Stage별 KC 숙련도 변화 추이 응답 모델
@JsonSerializable()
class StageKcMasteryTrendResponse {
  final String? stage;
  final List<KcTrend>? kcTrends;

  StageKcMasteryTrendResponse({
    this.stage,
    this.kcTrends,
  });

  factory StageKcMasteryTrendResponse.fromJson(Map<String, dynamic> json) =>
      _$StageKcMasteryTrendResponseFromJson(json);

  Map<String, dynamic> toJson() => _$StageKcMasteryTrendResponseToJson(this);
}

/// KC 추이 정보
@JsonSerializable()
class KcTrend {
  final int? kcId;
  final String? kcCategory;
  final String? kcDescription;
  final MasteryPoint? initialMastery;
  final List<MasteryPoint>? masteryTrend;

  KcTrend({
    this.kcId,
    this.kcCategory,
    this.kcDescription,
    this.initialMastery,
    this.masteryTrend,
  });

  factory KcTrend.fromJson(Map<String, dynamic> json) =>
      _$KcTrendFromJson(json);

  Map<String, dynamic> toJson() => _$KcTrendToJson(this);
}

/// 숙련도 시점 데이터
@JsonSerializable()
class MasteryPoint {
  @JsonKey(name: 'plearn')
  final double? pLearn;

  @JsonKey(name: 'ptrain')
  final double? pTrain;

  @JsonKey(name: 'pguess')
  final double? pGuess;

  @JsonKey(name: 'pslip')
  final double? pSlip;

  final DateTime? updatedAt;

  MasteryPoint({
    this.pLearn,
    this.pTrain,
    this.pGuess,
    this.pSlip,
    this.updatedAt,
  });

  factory MasteryPoint.fromJson(Map<String, dynamic> json) =>
      _$MasteryPointFromJson(json);

  Map<String, dynamic> toJson() => _$MasteryPointToJson(this);
}
