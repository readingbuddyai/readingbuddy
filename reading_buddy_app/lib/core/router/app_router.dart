import 'package:go_router/go_router.dart';
import '../../features/auth/presentation/screens/splash_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/signup_screen.dart';
import '../../features/auth/presentation/screens/device_auth_screen.dart';
import '../../features/dashboard/presentation/screens/main_screen.dart';

/// 앱 라우팅 설정
class AppRouter {
  static const String splash = '/';
  static const String login = '/';
  static const String signup = '/signup';
  static const String deviceAuth = '/device-auth';
  static const String main = '/main';

  static GoRouter router = GoRouter(
    initialLocation: login,
    routes: [
      GoRoute(
        path: login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: signup,
        builder: (context, state) => const SignupScreen(),
      ),
      GoRoute(
        path: deviceAuth,
        builder: (context, state) => const DeviceAuthScreen(),
      ),
      GoRoute(
        path: main,
        builder: (context, state) => const MainScreen(),
      ),
    ],
  );
}
