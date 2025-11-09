import 'package:json_annotation/json_annotation.dart';

part 'phoneme_rank_response.g.dart';

@JsonSerializable()
class PhonemeRankResponse {
  final int phonemeId;
  final String value;
  final String category;
  final int? wrongCnt;
  final int? tryCnt;

  PhonemeRankResponse({
    required this.phonemeId,
    required this.value,
    required this.category,
    this.wrongCnt,
    this.tryCnt,
  });

  factory PhonemeRankResponse.fromJson(Map<String, dynamic> json) =>
      _$PhonemeRankResponseFromJson(json);

  Map<String, dynamic> toJson() => _$PhonemeRankResponseToJson(this);

  /// 랭킹에 사용할 카운트 (wrongCnt 또는 tryCnt)
  int get count => wrongCnt ?? tryCnt ?? 0;
}
