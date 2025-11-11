import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/auth_provider.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/providers/providers.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _rememberEmail = false;
  bool _autoLogin = false;

  @override
  void initState() {
    super.initState();
    _loadSavedSettings();
  }

  /// 저장된 설정 불러오기
  Future<void> _loadSavedSettings() async {
    final tokenStorage = ref.read(tokenStorageProvider);

    // 아이디 저장 설정 불러오기
    final rememberEmail = tokenStorage.isRememberEmail();
    if (rememberEmail) {
      final savedEmail = tokenStorage.getSavedEmail();
      if (savedEmail != null) {
        _emailController.text = savedEmail;
        setState(() {
          _rememberEmail = true;
        });
      }
    }

    // 자동 로그인 설정 불러오기
    final autoLogin = tokenStorage.isAutoLogin();
    setState(() {
      _autoLogin = autoLogin;
    });
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) return;

    final email = _emailController.text.trim();
    final password = _passwordController.text;
    final tokenStorage = ref.read(tokenStorageProvider);

    final authNotifier = ref.read(authStateProvider.notifier);
    final success = await authNotifier.login(email, password);

    if (success && mounted) {
      // 아이디 저장 처리
      if (_rememberEmail) {
        await tokenStorage.setRememberEmail(true);
        await tokenStorage.saveSavedEmail(email);
      } else {
        await tokenStorage.setRememberEmail(false);
        await tokenStorage.clearSavedEmail();
      }

      // 자동 로그인 처리
      if (_autoLogin) {
        await tokenStorage.setAutoLogin(true);
        await tokenStorage.saveSavedPassword(password);
      } else {
        await tokenStorage.setAutoLogin(false);
        await tokenStorage.clearSavedPassword();
      }

      context.go(AppRouter.main);
    } else if (mounted) {
      final errorMessage = ref.read(authStateProvider).errorMessage;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(errorMessage ?? '로그인에 실패했습니다.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // 로고 또는 타이틀
                  Icon(
                    Icons.book_rounded,
                    size: 80,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Reading Buddy',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'VR 한글 학습 파트너',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey[600],
                        ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 48),

                  // 이메일 입력
                  TextFormField(
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(
                      labelText: '이메일',
                      prefixIcon: Icon(Icons.email),
                    ),
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return '이메일을 입력해주세요';
                      }
                      if (!value.contains('@')) {
                        return '올바른 이메일 형식이 아닙니다';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),

                  // 비밀번호 입력
                  TextFormField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    decoration: InputDecoration(
                      labelText: '비밀번호',
                      prefixIcon: const Icon(Icons.lock),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword
                              ? Icons.visibility
                              : Icons.visibility_off,
                        ),
                        onPressed: () {
                          setState(() {
                            _obscurePassword = !_obscurePassword;
                          });
                        },
                      ),
                    ),
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return '비밀번호를 입력해주세요';
                      }
                      if (value.length < 6) {
                        return '비밀번호는 최소 6자 이상이어야 합니다';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 8),

                  // 아이디 저장 체크박스
                  Row(
                    children: [
                      Checkbox(
                        value: _rememberEmail,
                        onChanged: (value) {
                          setState(() {
                            _rememberEmail = value ?? false;
                          });
                        },
                      ),
                      const Text('아이디 저장'),
                      const SizedBox(width: 24),
                      Checkbox(
                        value: _autoLogin,
                        onChanged: (value) {
                          setState(() {
                            _autoLogin = value ?? false;
                            // 자동 로그인 체크하면 아이디 저장도 자동으로 체크
                            if (_autoLogin) {
                              _rememberEmail = true;
                            }
                          });
                        },
                      ),
                      const Text('자동 로그인'),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // 로그인 버튼
                  ElevatedButton(
                    onPressed: authState.isLoading ? null : _handleLogin,
                    child: authState.isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('로그인'),
                  ),
                  const SizedBox(height: 16),

                  // 회원가입 버튼
                  OutlinedButton(
                    onPressed: () => context.push(AppRouter.signup),
                    child: const Text('회원가입'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
