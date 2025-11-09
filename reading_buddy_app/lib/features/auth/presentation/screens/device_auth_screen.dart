import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/auth_provider.dart';

class DeviceAuthScreen extends ConsumerStatefulWidget {
  const DeviceAuthScreen({super.key});

  @override
  ConsumerState<DeviceAuthScreen> createState() => _DeviceAuthScreenState();
}

class _DeviceAuthScreenState extends ConsumerState<DeviceAuthScreen> {
  final _codeController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _handleAuthorize() async {
    if (!_formKey.currentState!.validate()) return;

    final authNotifier = ref.read(authStateProvider.notifier);
    final errorMessage = await authNotifier.authorizeDevice(
      _codeController.text.trim().toUpperCase(),
    );

    if (errorMessage == null && mounted) {
      // 성공
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green),
              SizedBox(width: 8),
              Text('인증 성공'),
            ],
          ),
          content:
              const Text('VR 기기가 성공적으로 인증되었습니다!\n\nVR 기기에서 자동으로 로그인됩니다.'),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
                Navigator.of(context).pop();
              },
              child: const Text('확인'),
            ),
          ],
        ),
      );
    } else if (mounted) {
      // 실패
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(errorMessage ?? 'VR 기기 인증에 실패했습니다.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('VR 기기 연결'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // 아이콘
                Icon(
                  Icons.vrpano,
                  size: 100,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(height: 24),

                // 제목
                Text(
                  'VR 기기 연결',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),

                // 설명
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          '연결 방법:',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 12),
                        _buildStep(1, 'VR 기기에서 "앱으로 로그인" 선택'),
                        _buildStep(2, 'VR 화면에 표시된 10자리 코드 확인'),
                        _buildStep(3, '아래 입력란에 코드 입력'),
                        _buildStep(4, '인증 버튼 클릭'),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 32),

                // 주의사항
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.orange.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.orange.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.info_outline, color: Colors.orange.shade700),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          '코드는 1분 후 만료됩니다',
                          style: TextStyle(
                            color: Colors.orange.shade900,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),

                // 코드 입력
                TextFormField(
                  controller: _codeController,
                  maxLength: 10,
                  textCapitalization: TextCapitalization.characters,
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 4,
                  ),
                  textAlign: TextAlign.center,
                  decoration: const InputDecoration(
                    labelText: '인증 코드',
                    hintText: 'ABCD1234EF',
                    prefixIcon: Icon(Icons.vpn_key),
                  ),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z0-9]')),
                    UpperCaseTextFormatter(),
                  ],
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return '인증 코드를 입력해주세요';
                    }
                    if (value.length != 10) {
                      return '인증 코드는 10자리입니다';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 32),

                // 인증 버튼
                ElevatedButton.icon(
                  onPressed: authState.isLoading ? null : _handleAuthorize,
                  icon: authState.isLoading
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.check_circle),
                  label: Text(authState.isLoading ? '인증 중...' : 'VR 기기 인증'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildStep(int number, String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.primary,
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                '$number',
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(top: 2.0),
              child: Text(text),
            ),
          ),
        ],
      ),
    );
  }
}

/// VR 기기 연결 컨텐츠 (모달용)
class DeviceAuthContent extends ConsumerStatefulWidget {
  const DeviceAuthContent({super.key});

  @override
  ConsumerState<DeviceAuthContent> createState() => _DeviceAuthContentState();
}

class _DeviceAuthContentState extends ConsumerState<DeviceAuthContent> {
  final _codeController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _handleAuthorize() async {
    if (!_formKey.currentState!.validate()) return;

    final authNotifier = ref.read(authStateProvider.notifier);
    final errorMessage = await authNotifier.authorizeDevice(
      _codeController.text.trim().toUpperCase(),
    );

    if (errorMessage == null && mounted) {
      // 성공
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green),
              SizedBox(width: 8),
              Text('인증 성공'),
            ],
          ),
          content:
              const Text('VR 기기가 성공적으로 인증되었습니다!\n\nVR 기기에서 자동으로 로그인됩니다.'),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop(); // 다이얼로그 닫기
                Navigator.of(context).pop(); // 모달 닫기
              },
              child: const Text('확인'),
            ),
          ],
        ),
      );
    } else if (mounted) {
      // 실패
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(errorMessage ?? 'VR 기기 인증에 실패했습니다.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24.0),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 아이콘
            Icon(
              Icons.vrpano,
              size: 100,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(height: 24),

            // 제목
            Text(
              'VR 기기 연결',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),

            // 설명
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      '연결 방법:',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 12),
                    _buildStep(1, 'VR 기기에서 "앱으로 로그인" 선택'),
                    _buildStep(2, 'VR 화면에 표시된 10자리 코드 확인'),
                    _buildStep(3, '아래 입력란에 코드 입력'),
                    _buildStep(4, '인증 버튼 클릭'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 32),

            // 주의사항
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.orange.shade50,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.orange.shade200),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline, color: Colors.orange.shade700),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '코드는 1분 후 만료됩니다',
                      style: TextStyle(
                        color: Colors.orange.shade900,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // 코드 입력
            TextFormField(
              controller: _codeController,
              maxLength: 10,
              textCapitalization: TextCapitalization.characters,
              style: const TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
                letterSpacing: 4,
              ),
              textAlign: TextAlign.center,
              decoration: const InputDecoration(
                labelText: '인증 코드',
                hintText: 'ABCD1234EF',
                prefixIcon: Icon(Icons.vpn_key),
              ),
              inputFormatters: [
                FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z0-9]')),
                UpperCaseTextFormatter(),
              ],
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '인증 코드를 입력해주세요';
                }
                if (value.length != 10) {
                  return '인증 코드는 10자리입니다';
                }
                return null;
              },
            ),
            const SizedBox(height: 32),

            // 인증 버튼
            ElevatedButton.icon(
              onPressed: authState.isLoading ? null : _handleAuthorize,
              icon: authState.isLoading
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.check_circle),
              label: Text(authState.isLoading ? '인증 중...' : 'VR 기기 인증'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStep(int number, String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.primary,
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                '$number',
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(top: 2.0),
              child: Text(text),
            ),
          ),
        ],
      ),
    );
  }
}

/// 대문자 변환 InputFormatter
class UpperCaseTextFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    return TextEditingValue(
      text: newValue.text.toUpperCase(),
      selection: newValue.selection,
    );
  }
}
