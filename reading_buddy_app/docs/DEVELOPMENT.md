# Development Guide - Reading Buddy App

개발자를 위한 상세 가이드입니다.

## 아키텍처 개요

### Clean Architecture

프로젝트는 Clean Architecture 원칙을 따릅니다:

```
Presentation Layer (UI)
      ↓
Domain Layer (Business Logic)
      ↓
Data Layer (API, Database)
```

### 레이어별 책임

1. **Presentation Layer**
   - UI 렌더링
   - 사용자 입력 처리
   - 상태 관리 (Riverpod)

2. **Domain Layer**
   - 비즈니스 로직
   - Entity 정의
   - Repository 인터페이스

3. **Data Layer**
   - API 통신
   - 로컬 저장소 접근
   - Repository 구현

## 상태 관리 (Riverpod)

### Provider 종류

```dart
// 1. Provider: 변하지 않는 값
final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(...);
});

// 2. StateProvider: 간단한 상태
final counterProvider = StateProvider<int>((ref) => 0);

// 3. StateNotifierProvider: 복잡한 상태
final authStateProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(...);
});

// 4. FutureProvider: 비동기 데이터
final userProvider = FutureProvider<User>((ref) async {
  return await fetchUser();
});
```

### Provider 사용법

```dart
// Consumer Widget에서 사용
class MyWidget extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // watch: 값이 변경되면 rebuild
    final authState = ref.watch(authStateProvider);

    // read: 한 번만 읽기 (이벤트 핸들러에서 사용)
    final authNotifier = ref.read(authStateProvider.notifier);

    // listen: 변경 감지 (부수 효과)
    ref.listen(authStateProvider, (previous, next) {
      if (next.isLoggedIn) {
        // 로그인 성공 처리
      }
    });

    return Container(...);
  }
}
```

## API 통신

### Retrofit API 추가

1. **ApiClient 정의** (`core/network/api_client.dart`):

```dart
@GET('/api/your-endpoint')
Future<ApiResponse<YourModel>> getYourData(
  @Query('param') String param,
);

@POST('/api/your-endpoint')
Future<void> postYourData(@Body() YourRequest request);
```

2. **코드 생성**:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

3. **Repository에서 사용**:

```dart
@override
Future<YourModel?> getYourData(String param) async {
  try {
    final response = await _apiClient.getYourData(param);
    if (response.isSuccess && response.data != null) {
      return response.data;
    }
    return null;
  } catch (e) {
    _logger.e('데이터 조회 실패: $e');
    return null;
  }
}
```

### 에러 처리

**DioClient**에서 자동으로 처리됩니다:
- **401 Unauthorized**: 토큰 자동 재발급
- **Network Error**: 로깅
- **Timeout**: 30초 후 타임아웃

커스텀 에러 처리:

```dart
try {
  final response = await _apiClient.someMethod();
  return response.data;
} on DioException catch (e) {
  if (e.response?.statusCode == 404) {
    // Not Found 처리
  } else if (e.type == DioExceptionType.connectionTimeout) {
    // 타임아웃 처리
  }
  rethrow;
} catch (e) {
  // 기타 에러
  _logger.e('에러 발생: $e');
}
```

## 데이터 모델

### JSON Serialization

1. **모델 작성**:

```dart
import 'package:json_annotation/json_annotation.dart';

part 'your_model.g.dart';

@JsonSerializable()
class YourModel {
  final int id;
  final String name;

  @JsonKey(name: 'created_at') // 서버 필드명과 다를 때
  final String? createdAt;

  YourModel({
    required this.id,
    required this.name,
    this.createdAt,
  });

  factory YourModel.fromJson(Map<String, dynamic> json) =>
      _$YourModelFromJson(json);

  Map<String, dynamic> toJson() => _$YourModelToJson(this);
}
```

2. **코드 생성**:

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### Generic 모델

`ApiResponse<T>`는 Generic 모델의 예시:

```dart
@JsonSerializable(genericArgumentFactories: true)
class ApiResponse<T> {
  final String status;
  final T? data;

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) => _$ApiResponseFromJson(json, fromJsonT);
}
```

## 로컬 저장소

### SecureStorage vs SharedPreferences

**SecureStorage** (민감한 정보):
```dart
final tokenStorage = ref.watch(tokenStorageProvider);

// 저장
await tokenStorage.saveAccessToken(token);

// 조회
final token = await tokenStorage.getAccessToken();

// 삭제
await tokenStorage.clearAll();
```

**SharedPreferences** (일반 정보):
```dart
final prefs = await SharedPreferences.getInstance();

// 저장
await prefs.setString('key', 'value');
await prefs.setInt('count', 10);
await prefs.setBool('flag', true);

// 조회
final value = prefs.getString('key');
final count = prefs.getInt('count') ?? 0;
final flag = prefs.getBool('flag') ?? false;

// 삭제
await prefs.remove('key');
await prefs.clear();
```

## 라우팅 (go_router)

### 새 화면 추가

1. **AppRouter 수정** (`core/router/app_router.dart`):

```dart
static const String yourScreen = '/your-screen';

static GoRouter router = GoRouter(
  routes: [
    // ...
    GoRoute(
      path: yourScreen,
      builder: (context, state) => const YourScreen(),
    ),
  ],
);
```

2. **화면 이동**:

```dart
// Push (스택에 추가)
context.push(AppRouter.yourScreen);

// Go (스택 교체)
context.go(AppRouter.yourScreen);

// Pop (뒤로 가기)
context.pop();

// 파라미터 전달
context.push('/details?id=123');
```

## 테마

### 새 테마 추가

`core/theme/app_theme.dart`:

```dart
static ThemeData myCustomTheme() {
  return ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: Colors.purple,
    ),
    // ... 기타 설정
  );
}

static ThemeData getTheme(String themeName) {
  switch (themeName) {
    case 'my_custom':
      return myCustomTheme();
    // ...
  }
}
```

### 런타임 테마 변경

```dart
final tokenStorage = ref.watch(tokenStorageProvider);
await tokenStorage.saveSelectedTheme(AppTheme.professional);

// 앱 재시작 필요
```

## 위젯 작성 가이드

### StatelessWidget vs StatefulWidget vs ConsumerWidget

```dart
// 1. StatelessWidget: 상태 없음
class MyWidget extends StatelessWidget {
  const MyWidget({super.key});

  @override
  Widget build(BuildContext context) {
    return Container();
  }
}

// 2. StatefulWidget: 로컬 상태 있음
class MyWidget extends StatefulWidget {
  const MyWidget({super.key});

  @override
  State<MyWidget> createState() => _MyWidgetState();
}

class _MyWidgetState extends State<MyWidget> {
  int _counter = 0;

  @override
  Widget build(BuildContext context) {
    return Container();
  }
}

// 3. ConsumerWidget: Riverpod 사용
class MyWidget extends ConsumerWidget {
  const MyWidget({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(someProvider);
    return Container();
  }
}

// 4. ConsumerStatefulWidget: Riverpod + 로컬 상태
class MyWidget extends ConsumerStatefulWidget {
  const MyWidget({super.key});

  @override
  ConsumerState<MyWidget> createState() => _MyWidgetState();
}

class _MyWidgetState extends ConsumerState<MyWidget> {
  int _counter = 0;

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(someProvider);
    return Container();
  }
}
```

### Best Practices

1. **const 생성자 사용**:
```dart
const MyWidget({super.key}); // ✅ Good
MyWidget({super.key}); // ❌ Bad
```

2. **위젯 분리**:
```dart
// ✅ Good
class MyScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _Header(),
        _Body(),
        _Footer(),
      ],
    );
  }
}

// ❌ Bad
class MyScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(...), // 복잡한 위젯
        Container(...), // 복잡한 위젯
        Container(...), // 복잡한 위젯
      ],
    );
  }
}
```

3. **BuildContext 사용 시 주의**:
```dart
// ✅ Good
Future<void> _handleLogin() async {
  final success = await authNotifier.login(email, password);

  if (success && mounted) { // mounted 체크
    context.go(AppRouter.main);
  }
}

// ❌ Bad
Future<void> _handleLogin() async {
  final success = await authNotifier.login(email, password);

  context.go(AppRouter.main); // 비동기 후 mounted 체크 없음
}
```

## 테스트 작성

### 유닛 테스트

```dart
// test/unit/auth_repository_test.dart
void main() {
  group('AuthRepository', () {
    late AuthRepository repository;
    late MockApiClient mockApiClient;

    setUp(() {
      mockApiClient = MockApiClient();
      repository = AuthRepositoryImpl(mockApiClient, ...);
    });

    test('로그인 성공 시 true 반환', () async {
      // Arrange
      when(mockApiClient.login(any))
          .thenAnswer((_) async => TokenResponse(...));

      // Act
      final result = await repository.login('test@test.com', 'password');

      // Assert
      expect(result, true);
    });
  });
}
```

### 위젯 테스트

```dart
// test/widget/login_screen_test.dart
void main() {
  testWidgets('로그인 버튼 탭 시 로그인 함수 호출', (tester) async {
    // Arrange
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(home: LoginScreen()),
      ),
    );

    // Act
    await tester.enterText(find.byType(TextField).first, 'test@test.com');
    await tester.enterText(find.byType(TextField).last, 'password');
    await tester.tap(find.text('로그인'));
    await tester.pumpAndSettle();

    // Assert
    expect(find.text('로그인 성공'), findsOneWidget);
  });
}
```

## 성능 최적화

### 1. Const 생성자 사용

```dart
const Text('Hello'); // ✅ 재사용 가능
Text('Hello'); // ❌ 매번 새 객체 생성
```

### 2. ListView.builder 사용

```dart
// ✅ Good: 보이는 아이템만 렌더링
ListView.builder(
  itemCount: items.length,
  itemBuilder: (context, index) => ItemWidget(items[index]),
);

// ❌ Bad: 모든 아이템 한 번에 렌더링
ListView(
  children: items.map((item) => ItemWidget(item)).toList(),
);
```

### 3. Image 캐싱

```dart
CachedNetworkImage(
  imageUrl: 'https://example.com/image.jpg',
  placeholder: (context, url) => CircularProgressIndicator(),
  errorWidget: (context, url, error) => Icon(Icons.error),
);
```

### 4. Provider 최적화

```dart
// ✅ Good: 필요한 부분만 watch
final username = ref.watch(userProvider.select((user) => user.name));

// ❌ Bad: 전체 객체 watch
final user = ref.watch(userProvider);
```

## 디버깅 팁

### 1. DevTools 사용

```bash
flutter run
# 앱 실행 중 'd' 키 입력
```

- Widget Inspector: 위젯 트리 확인
- Network: API 요청/응답 확인
- Performance: 프레임 드롭 확인

### 2. Logger 활용

```dart
final logger = Logger();

logger.d('Debug message'); // 디버그
logger.i('Info message'); // 정보
logger.w('Warning message'); // 경고
logger.e('Error message'); // 에러
```

### 3. Breakpoint

VS Code에서 줄 번호 왼쪽 클릭 → 빨간 점 생성 → F5로 디버그 실행

## 배포

### Android APK

```bash
# Release APK 빌드
flutter build apk --release

# APK 위치
build/app/outputs/flutter-apk/app-release.apk
```

### Android App Bundle (Play Store)

```bash
flutter build appbundle --release

# AAB 위치
build/app/outputs/bundle/release/app-release.aab
```

### iOS IPA

```bash
flutter build ios --release

# Xcode에서 Archive → Upload to App Store
```

## 코딩 컨벤션

- 파일명: `snake_case.dart`
- 클래스명: `PascalCase`
- 변수/함수명: `camelCase`
- 상수: `lowerCamelCase` (Dart는 const 키워드 사용)
- Private: `_leadingUnderscore`

---

**Happy Development! 🚀**
