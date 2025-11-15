import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../../../../core/router/app_router.dart';

class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkAutoLogin();
  }

  /// 자동 로그인 체크
  Future<void> _checkAutoLogin() async {
    print('🚀 스플래시: 자동 로그인 체크 시작');

    // 최소 1초는 스플래시 화면을 보여줌 (너무 빠르게 지나가는 것 방지)
    final results = await Future.wait([
      ref.read(authStateProvider.notifier).checkAutoLogin(),
      Future.delayed(const Duration(seconds: 1)),
    ]);

    final success = results[0] as bool;
    print('🚀 스플래시: 자동 로그인 결과 = $success');

    if (!mounted) return;

    if (success) {
      // 자동 로그인 성공 -> 메인 화면으로
      print('✅ 스플래시: 메인 화면으로 이동');
      context.go(AppRouter.main);
    } else {
      // 자동 로그인 실패 -> 로그인 화면으로
      print('❌ 스플래시: 로그인 화면으로 이동');
      context.go(AppRouter.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      body: Center(
        child: Image.asset(
          'assets/images/logo.png',
          width: 200,
          height: 200,
          fit: BoxFit.contain,
          errorBuilder: (context, error, stackTrace) {
            // 로고 파일 없으면 기본 아이콘 표시
            return Icon(
              Icons.book_rounded,
              size: 150,
              color: Theme.of(context).colorScheme.primary,
            );
          },
        ),
      ),
    );
  }
}
