import 'package:json_annotation/json_annotation.dart';

part 'device_login_response.g.dart';

@JsonSerializable()
class DeviceLoginResponse {
  final String authCode;

  DeviceLoginResponse({
    required this.authCode,
  });

  factory DeviceLoginResponse.fromJson(Map<String, dynamic> json) =>
      _$DeviceLoginResponseFromJson(json);

  Map<String, dynamic> toJson() => _$DeviceLoginResponseToJson(this);
}
