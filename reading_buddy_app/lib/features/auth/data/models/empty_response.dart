import 'package:json_annotation/json_annotation.dart';

part 'empty_response.g.dart';

/// 빈 응답을 처리하기 위한 모델
/// void 타입을 사용할 수 없는 경우 대신 사용
@JsonSerializable()
class EmptyResponse {
  const EmptyResponse();

  factory EmptyResponse.fromJson(Map<String, dynamic> json) =>
      _$EmptyResponseFromJson(json);

  Map<String, dynamic> toJson() => _$EmptyResponseToJson(this);
}
