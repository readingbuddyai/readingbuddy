/// Result 패턴으로 성공/실패를 명확하게 구분
///
/// 사용 예시:
/// ```dart
/// Future<Result<User>> getUser(int id) async {
///   try {
///     final user = await api.getUser(id);
///     return Success(user);
///   } catch (e) {
///     return Failure('사용자를 찾을 수 없습니다', type: ErrorType.notFound);
///   }
/// }
///
/// // 사용
/// final result = await getUser(1);
/// if (result.isSuccess) {
///   print(result.data);
/// } else {
///   print(result.errorMessage);
/// }
/// ```
sealed class Result<T> {
  const Result();

  /// 성공 여부
  bool get isSuccess => this is Success<T>;

  /// 실패 여부
  bool get isFailure => this is Failure<T>;

  /// 성공 시 데이터 반환, 실패 시 null
  T? get dataOrNull => isSuccess ? (this as Success<T>).data : null;

  /// 실패 시 에러 메시지 반환, 성공 시 null
  String? get errorOrNull => isFailure ? (this as Failure<T>).message : null;

  /// 실패 시 에러 타입 반환, 성공 시 null
  ErrorType? get errorTypeOrNull =>
      isFailure ? (this as Failure<T>).type : null;

  /// when 패턴으로 success/failure 분기 처리
  R when<R>({
    required R Function(T data) success,
    required R Function(String message, ErrorType type, int? statusCode)
        failure,
  }) {
    if (this is Success<T>) {
      return success((this as Success<T>).data);
    } else {
      final failure_ = this as Failure<T>;
      return failure(failure_.message, failure_.type, failure_.statusCode);
    }
  }
}

/// 성공 결과
class Success<T> extends Result<T> {
  final T data;

  const Success(this.data);

  @override
  String toString() => 'Success(data: $data)';

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Success<T> && other.data == data;
  }

  @override
  int get hashCode => data.hashCode;
}

/// 실패 결과
class Failure<T> extends Result<T> {
  final String message;
  final int? statusCode;
  final ErrorType type;

  const Failure(
    this.message, {
    this.statusCode,
    this.type = ErrorType.unknown,
  });

  /// 재시도 가능한 에러인지 확인
  bool get isRetryable =>
      type == ErrorType.network || type == ErrorType.server;

  /// 인증 에러인지 확인
  bool get isAuthError => type == ErrorType.auth;

  /// 데이터 없음 에러인지 확인
  bool get isNotFoundError => type == ErrorType.notFound;

  @override
  String toString() =>
      'Failure(message: $message, type: $type, statusCode: $statusCode)';

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Failure<T> &&
        other.message == message &&
        other.statusCode == statusCode &&
        other.type == type;
  }

  @override
  int get hashCode => Object.hash(message, statusCode, type);
}

/// 에러 타입 분류
enum ErrorType {
  /// 네트워크 연결 오류 (재시도 가능)
  network,

  /// 인증 만료 또는 권한 없음 (로그인 필요)
  auth,

  /// 서버 오류 (재시도 가능)
  server,

  /// 데이터 없음 (404)
  notFound,

  /// 데이터 파싱 실패
  parse,

  /// 잘못된 요청 (400)
  badRequest,

  /// 알 수 없는 오류
  unknown,
}

/// ErrorType에 대한 사용자 친화적 메시지
extension ErrorTypeMessage on ErrorType {
  String get defaultMessage {
    switch (this) {
      case ErrorType.network:
        return '네트워크 연결을 확인해주세요';
      case ErrorType.auth:
        return '로그인이 필요합니다';
      case ErrorType.server:
        return '서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요';
      case ErrorType.notFound:
        return '데이터를 찾을 수 없습니다';
      case ErrorType.parse:
        return '데이터 처리 중 오류가 발생했습니다';
      case ErrorType.badRequest:
        return '잘못된 요청입니다';
      case ErrorType.unknown:
        return '알 수 없는 오류가 발생했습니다';
    }
  }
}
