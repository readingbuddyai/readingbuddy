// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'practice_list_response.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

PracticeListResponse _$PracticeListResponseFromJson(
        Map<String, dynamic> json) =>
    PracticeListResponse(
      date: json['date'] as String,
      session: (json['session'] as List<dynamic>)
          .map((e) => SessionInfo.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$PracticeListResponseToJson(
        PracticeListResponse instance) =>
    <String, dynamic>{
      'date': instance.date,
      'session': instance.session,
    };

SessionInfo _$SessionInfoFromJson(Map<String, dynamic> json) => SessionInfo(
      trainedStageHistoryId: (json['trainedStageHistoryId'] as num).toInt(),
      stage: json['stage'] as String,
      startedAt: DateTime.parse(json['startedAt'] as String),
      totalCount: (json['totalCount'] as num).toInt(),
      correctCount: (json['correctCount'] as num).toInt(),
      wrongCount: (json['wrongCount'] as num).toInt(),
      problems: (json['problems'] as List<dynamic>)
          .map((e) => ProblemInfo.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$SessionInfoToJson(SessionInfo instance) =>
    <String, dynamic>{
      'trainedStageHistoryId': instance.trainedStageHistoryId,
      'stage': instance.stage,
      'startedAt': instance.startedAt.toIso8601String(),
      'totalCount': instance.totalCount,
      'correctCount': instance.correctCount,
      'wrongCount': instance.wrongCount,
      'problems': instance.problems,
    };

ProblemInfo _$ProblemInfoFromJson(Map<String, dynamic> json) => ProblemInfo(
      problemId: (json['problemId'] as num).toInt(),
      problemNumber: (json['problemNumber'] as num).toInt(),
      problem: json['problem'] as String,
      answer: json['answer'] as String,
      isCorrect: json['isCorrect'] as bool,
      isReplyCorrect: json['isReplyCorrect'] as bool?,
      attemptNumber: (json['attemptNumber'] as num).toInt(),
      audioUrl: json['audioUrl'] as String?,
      solvedAt: DateTime.parse(json['solvedAt'] as String),
    );

Map<String, dynamic> _$ProblemInfoToJson(ProblemInfo instance) =>
    <String, dynamic>{
      'problemId': instance.problemId,
      'problemNumber': instance.problemNumber,
      'problem': instance.problem,
      'answer': instance.answer,
      'isCorrect': instance.isCorrect,
      'isReplyCorrect': instance.isReplyCorrect,
      'attemptNumber': instance.attemptNumber,
      'audioUrl': instance.audioUrl,
      'solvedAt': instance.solvedAt.toIso8601String(),
    };
