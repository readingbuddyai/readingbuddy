import 'package:dio/dio.dart';
import 'package:logger/logger.dart';
import '../constants/api_constants.dart';
import '../storage/token_storage.dart';

/// Dio 클라이언트 설정
class DioClient {
  late final Dio _dio;
  final TokenStorage _tokenStorage;
  final Logger _logger = Logger();

  DioClient(this._tokenStorage) {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConstants.baseUrl,
        connectTimeout: ApiConstants.connectTimeout,
        receiveTimeout: ApiConstants.receiveTimeout,
        contentType: ApiConstants.contentType,
        headers: {
          'Accept': ApiConstants.contentType,
        },
      ),
    );

    // Interceptors 추가
    _dio.interceptors.addAll([
      _authInterceptor(),
      _loggingInterceptor(),
    ]);
  }

  Dio get dio => _dio;

  /// 인증 인터셉터 - 모든 요청에 토큰 자동 추가
  Interceptor _authInterceptor() {
    return InterceptorsWrapper(
      onRequest: (options, handler) async {
        // 로그인/회원가입 요청은 토큰 불필요
        final noAuthEndpoints = [
          ApiConstants.login,
          ApiConstants.signup,
          ApiConstants.polling,
        ];

        if (!noAuthEndpoints.contains(options.path)) {
          final accessToken = await _tokenStorage.getAccessToken();
          if (accessToken != null) {
            options.headers[ApiConstants.authorization] =
                '${ApiConstants.bearer} $accessToken';
          }
        }

        return handler.next(options);
      },
      onError: (error, handler) async {
        // 401 에러 시 토큰 재발급 시도
        if (error.response?.statusCode == 401) {
          try {
            final refreshToken = await _tokenStorage.getRefreshToken();
            final userId = await _tokenStorage.getUserId();

            if (refreshToken != null && userId != null) {
              // 토큰 재발급 요청
              final response = await _dio.post(
                ApiConstants.reissueToken,
                data: {
                  'refreshToken': refreshToken,
                  'userId': userId,
                },
              );

              if (response.statusCode == 200) {
                // ApiResponse 형식으로 변경되어 data 필드에서 토큰 정보 추출
                final responseData = response.data;
                if (responseData['success'] == true && responseData['data'] != null) {
                  final newAccessToken = responseData['data']['accessToken'];
                  final newRefreshToken = responseData['data']['refreshToken'];

                  // 새 토큰 저장
                  await _tokenStorage.saveAccessToken(newAccessToken);
                  await _tokenStorage.saveRefreshToken(newRefreshToken);

                  // 원래 요청 재시도
                  final options = error.requestOptions;
                  options.headers[ApiConstants.authorization] =
                      '${ApiConstants.bearer} $newAccessToken';

                  final retryResponse = await _dio.fetch(options);
                  return handler.resolve(retryResponse);
                }
              }
            }
          } catch (e) {
            _logger.e('토큰 재발급 실패: $e');
            // 재발급 실패 시 로그아웃 처리 필요
            await _tokenStorage.clearAll();
          }
        }

        return handler.next(error);
      },
    );
  }

  /// 로깅 인터셉터 - 개발 시 디버깅용
  Interceptor _loggingInterceptor() {
    return InterceptorsWrapper(
      onRequest: (options, handler) {
        _logger.d('''
📤 REQUEST[${options.method}] => URI: ${options.uri}
Query Params: ${options.queryParameters}
Headers: ${options.headers}
Data: ${options.data}
        ''');
        return handler.next(options);
      },
      onResponse: (response, handler) {
        _logger.i('''
📥 RESPONSE[${response.statusCode}] => PATH: ${response.requestOptions.path}
Data: ${response.data}
        ''');
        return handler.next(response);
      },
      onError: (error, handler) {
        _logger.e('''
❌ ERROR[${error.response?.statusCode}] => PATH: ${error.requestOptions.path}
Message: ${error.message}
Data: ${error.response?.data}
        ''');
        return handler.next(error);
      },
    );
  }
}
