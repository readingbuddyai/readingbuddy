import 'package:json_annotation/json_annotation.dart';

part 'device_code_request.g.dart';

@JsonSerializable()
class DeviceCodeRequest {
  final String deviceAuthCode;

  DeviceCodeRequest({
    required this.deviceAuthCode,
  });

  factory DeviceCodeRequest.fromJson(Map<String, dynamic> json) =>
      _$DeviceCodeRequestFromJson(json);

  Map<String, dynamic> toJson() => _$DeviceCodeRequestToJson(this);
}
