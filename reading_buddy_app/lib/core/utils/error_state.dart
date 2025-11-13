import 'result.dart';

/// Provider에서 사용할 에러 상태
///
/// 사용 예시:
/// ```dart
/// class HomeState {
///   final ErrorState? error;
///   // ...
/// }
///
/// // 에러 발생 시
/// state = state.copyWith(
///   error: ErrorState.network('네트워크 연결을 확인해주세요'),
/// );
///
/// // UI에서 사용
/// if (state.error != null) {
///   if (state.error!.isRetryable) {
///     // 재시도 버튼 표시
///   }
/// }
/// ```
class ErrorState {
  final ErrorType type;
  final String message;
  final int? statusCode;
  final DateTime timestamp;

  ErrorState({
    required this.type,
    required this.message,
    this.statusCode,
    DateTime? timestamp,
  }) : timestamp = timestamp ?? DateTime.now();

  /// 네트워크 에러 생성
  factory ErrorState.network([String? customMessage]) {
    return ErrorState(
      type: ErrorType.network,
      message: customMessage ?? ErrorType.network.defaultMessage,
      timestamp: DateTime.now(),
    );
  }

  /// 인증 에러 생성
  factory ErrorState.auth([String? customMessage]) {
    return ErrorState(
      type: ErrorType.auth,
      message: customMessage ?? ErrorType.auth.defaultMessage,
      statusCode: 401,
      timestamp: DateTime.now(),
    );
  }

  /// 서버 에러 생성
  factory ErrorState.server([String? customMessage, int? statusCode]) {
    return ErrorState(
      type: ErrorType.server,
      message: customMessage ?? ErrorType.server.defaultMessage,
      statusCode: statusCode,
      timestamp: DateTime.now(),
    );
  }

  /// 데이터 없음 에러 생성
  factory ErrorState.notFound([String? customMessage]) {
    return ErrorState(
      type: ErrorType.notFound,
      message: customMessage ?? ErrorType.notFound.defaultMessage,
      statusCode: 404,
      timestamp: DateTime.now(),
    );
  }

  /// 파싱 에러 생성
  factory ErrorState.parse([String? customMessage]) {
    return ErrorState(
      type: ErrorType.parse,
      message: customMessage ?? ErrorType.parse.defaultMessage,
      timestamp: DateTime.now(),
    );
  }

  /// 잘못된 요청 에러 생성
  factory ErrorState.badRequest([String? customMessage]) {
    return ErrorState(
      type: ErrorType.badRequest,
      message: customMessage ?? ErrorType.badRequest.defaultMessage,
      statusCode: 400,
      timestamp: DateTime.now(),
    );
  }

  /// 알 수 없는 에러 생성
  factory ErrorState.unknown([String? customMessage]) {
    return ErrorState(
      type: ErrorType.unknown,
      message: customMessage ?? ErrorType.unknown.defaultMessage,
      timestamp: DateTime.now(),
    );
  }

  /// Result의 Failure로부터 ErrorState 생성
  factory ErrorState.fromFailure(Failure failure) {
    return ErrorState(
      type: failure.type,
      message: failure.message,
      statusCode: failure.statusCode,
      timestamp: DateTime.now(),
    );
  }

  /// 재시도 가능한 에러인지 확인
  bool get isRetryable =>
      type == ErrorType.network || type == ErrorType.server;

  /// 인증 에러인지 확인
  bool get isAuthError => type == ErrorType.auth;

  /// 데이터 없음 에러인지 확인
  bool get isNotFoundError => type == ErrorType.notFound;

  /// ErrorState 복사 (일부 필드만 변경)
  ErrorState copyWith({
    ErrorType? type,
    String? message,
    int? statusCode,
    DateTime? timestamp,
  }) {
    return ErrorState(
      type: type ?? this.type,
      message: message ?? this.message,
      statusCode: statusCode ?? this.statusCode,
      timestamp: timestamp ?? this.timestamp,
    );
  }

  @override
  String toString() =>
      'ErrorState(type: $type, message: $message, statusCode: $statusCode)';

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is ErrorState &&
        other.type == type &&
        other.message == message &&
        other.statusCode == statusCode;
  }

  @override
  int get hashCode => Object.hash(type, message, statusCode);
}
