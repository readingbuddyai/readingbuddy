// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'attendance_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AttendanceResponse _$AttendanceResponseFromJson(Map<String, dynamic> json) =>
    AttendanceResponse(
      periodData: json['periodData'] == null
          ? null
          : PeriodAttendance.fromJson(
              json['periodData'] as Map<String, dynamic>),
      dailyData: json['dailyData'] == null
          ? null
          : DailyAttendance.fromJson(json['dailyData'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$AttendanceResponseToJson(AttendanceResponse instance) =>
    <String, dynamic>{
      'periodData': instance.periodData,
      'dailyData': instance.dailyData,
    };

PeriodAttendance _$PeriodAttendanceFromJson(Map<String, dynamic> json) =>
    PeriodAttendance(
      attendDates: (json['attendDates'] as List<dynamic>)
          .map((e) => AttendDateInfo.fromJson(e as Map<String, dynamic>))
          .toList(),
      totalAttendDays: (json['totalAttendDays'] as num).toInt(),
    );

Map<String, dynamic> _$PeriodAttendanceToJson(PeriodAttendance instance) =>
    <String, dynamic>{
      'attendDates': instance.attendDates,
      'totalAttendDays': instance.totalAttendDays,
    };

DailyAttendance _$DailyAttendanceFromJson(Map<String, dynamic> json) =>
    DailyAttendance(
      attendDate: json['attendDate'] as String,
      playtime: json['playtime'] as String,
      attended: json['attended'] as bool,
    );

Map<String, dynamic> _$DailyAttendanceToJson(DailyAttendance instance) =>
    <String, dynamic>{
      'attendDate': instance.attendDate,
      'playtime': instance.playtime,
      'attended': instance.attended,
    };

AttendDateInfo _$AttendDateInfoFromJson(Map<String, dynamic> json) =>
    AttendDateInfo(
      attendDate: json['attendDate'] as String,
      playtime: json['playtime'] as String,
    );

Map<String, dynamic> _$AttendDateInfoToJson(AttendDateInfo instance) =>
    <String, dynamic>{
      'attendDate': instance.attendDate,
      'playtime': instance.playtime,
    };
