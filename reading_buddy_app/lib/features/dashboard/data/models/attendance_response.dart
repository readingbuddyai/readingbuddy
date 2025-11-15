import 'package:json_annotation/json_annotation.dart';

part 'attendance_response.g.dart';

@JsonSerializable()
class AttendanceResponse {
  final PeriodAttendance? periodData;
  final DailyAttendance? dailyData;

  AttendanceResponse({
    this.periodData,
    this.dailyData,
  });

  factory AttendanceResponse.fromJson(Map<String, dynamic> json) =>
      _$AttendanceResponseFromJson(json);

  Map<String, dynamic> toJson() => _$AttendanceResponseToJson(this);
}

@JsonSerializable()
class PeriodAttendance {
  final List<AttendDateInfo> attendDates;
  final int totalAttendDays;

  PeriodAttendance({
    required this.attendDates,
    required this.totalAttendDays,
  });

  factory PeriodAttendance.fromJson(Map<String, dynamic> json) =>
      _$PeriodAttendanceFromJson(json);

  Map<String, dynamic> toJson() => _$PeriodAttendanceToJson(this);
}

@JsonSerializable()
class DailyAttendance {
  final String attendDate;
  final String playtime;
  final bool attended;

  DailyAttendance({
    required this.attendDate,
    required this.playtime,
    required this.attended,
  });

  factory DailyAttendance.fromJson(Map<String, dynamic> json) =>
      _$DailyAttendanceFromJson(json);

  Map<String, dynamic> toJson() => _$DailyAttendanceToJson(this);
}

@JsonSerializable()
class AttendDateInfo {
  final String attendDate;
  final String playtime;

  AttendDateInfo({
    required this.attendDate,
    required this.playtime,
  });

  factory AttendDateInfo.fromJson(Map<String, dynamic> json) =>
      _$AttendDateInfoFromJson(json);

  Map<String, dynamic> toJson() => _$AttendDateInfoToJson(this);

  /// playtime을 분과 초로 파싱 (형식: "15:30")
  int get totalSeconds {
    final parts = playtime.split(':');
    if (parts.length != 2) return 0;
    final minutes = int.tryParse(parts[0]) ?? 0;
    final seconds = int.tryParse(parts[1]) ?? 0;
    return minutes * 60 + seconds;
  }
}
