import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../providers/profile_provider.dart';
import '../providers/home_provider.dart';
import '../providers/analysis_provider.dart';
import '../providers/attendance_provider.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/providers/theme_provider.dart';
import '../../../../core/theme/app_theme.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileState = ref.watch(profileProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => ref.read(profileProvider.notifier).refresh(),
          child: ListView(
            children: [
              const SizedBox(height: 32),

              // 프로필 이미지
              CircleAvatar(
                radius: 50,
                backgroundColor: theme.colorScheme.primary.withOpacity(0.2),
                child: Icon(
                  Icons.person,
                  size: 50,
                  color: theme.colorScheme.primary,
                ),
              ),
              const SizedBox(height: 16),

              // 닉네임
              Text(
                profileState.nickname ?? '사용자',
                textAlign: TextAlign.center,
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),

              // 이메일
              Text(
                profileState.email ?? 'email@example.com',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                ),
              ),
              const SizedBox(height: 32),

              // 학습 요약 섹션
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(
                              Icons.school,
                              color: theme.colorScheme.primary,
                              size: 20,
                            ),
                            const SizedBox(width: 8),
                            Text(
                              '학습 요약',
                              style: theme.textTheme.titleMedium?.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        _buildStatRow(
                          context,
                          Icons.access_time,
                          '총 학습 시간',
                          profileState.totalLearningTime,
                        ),
                        const SizedBox(height: 12),
                        _buildStatRow(
                          context,
                          Icons.calendar_today,
                          '총 출석 일수',
                          '${profileState.totalAttendDays}일',
                        ),
                        const SizedBox(height: 12),
                        _buildStatRow(
                          context,
                          Icons.local_fire_department,
                          '연속 출석',
                          '${profileState.consecutiveDays}일',
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),

              const Divider(),

              // 설정 항목들
              ListTile(
                leading: Icon(Icons.devices, color: theme.colorScheme.primary),
                title: const Text('VR 기기 연결'),
                subtitle: const Text('VR 기기와 앱을 연결하세요'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  context.push(AppRouter.deviceAuth);
                },
              ),
              ListTile(
                leading: Icon(Icons.palette, color: theme.colorScheme.primary),
                title: const Text('테마 설정'),
                subtitle: const Text('앱의 색상 테마를 선택하세요'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  _showThemeDialog(context, ref);
                },
              ),
              ListTile(
                leading: Icon(Icons.notifications_outlined,
                    color: theme.colorScheme.primary),
                title: const Text('알림 설정'),
                subtitle: const Text('학습 알림을 관리하세요'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('알림 설정 기능은 추후 추가될 예정입니다')),
                  );
                },
              ),
              ListTile(
                leading:
                    Icon(Icons.info_outline, color: theme.colorScheme.primary),
                title: const Text('앱 정보'),
                subtitle: const Text('버전 1.0.0'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  showAboutDialog(
                    context: context,
                    applicationName: 'Reading Buddy',
                    applicationVersion: '1.0.0',
                    applicationLegalese: '© 2025 Reading Buddy Team',
                    children: [
                      const SizedBox(height: 16),
                      const Text('VR 한글 학습 시스템의 모바일 컴패니언 앱'),
                    ],
                  );
                },
              ),
              ListTile(
                leading:
                    Icon(Icons.description, color: theme.colorScheme.primary),
                title: const Text('이용약관'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('이용약관 보기 기능은 추후 추가될 예정입니다')),
                  );
                },
              ),
              ListTile(
                leading: Icon(Icons.privacy_tip, color: theme.colorScheme.primary),
                title: const Text('개인정보 처리방침'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                        content: Text('개인정보 처리방침 보기 기능은 추후 추가될 예정입니다')),
                  );
                },
              ),

              const Divider(),

              // 로그아웃
              ListTile(
                leading: const Icon(Icons.logout, color: AppTheme.errorColor),
                title: const Text(
                  '로그아웃',
                  style: TextStyle(color: AppTheme.errorColor),
                ),
                onTap: () {
                  _showLogoutDialog(context, ref);
                },
              ),

              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  /// 통계 행 위젯
  Widget _buildStatRow(
      BuildContext context, IconData icon, String label, String value) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(
          icon,
          size: 20,
          color: theme.colorScheme.onSurface.withOpacity(0.6),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            label,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurface.withOpacity(0.7),
            ),
          ),
        ),
        Text(
          value,
          style: theme.textTheme.bodyLarge?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
      ],
    );
  }

  /// 테마 선택 다이얼로그
  void _showThemeDialog(BuildContext context, WidgetRef ref) {
    final currentTheme = ref.read(themeProvider);

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('테마 선택'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildThemeOption(
              context,
              ref,
              AppTheme.light,
              '라이트 모드',
              '밝은 테마',
              Icons.light_mode,
              currentTheme == AppTheme.light,
            ),
            const SizedBox(height: 8),
            _buildThemeOption(
              context,
              ref,
              AppTheme.dark,
              '다크 모드',
              '어두운 테마',
              Icons.dark_mode,
              currentTheme == AppTheme.dark,
            ),
          ],
        ),
      ),
    );
  }

  /// 테마 선택 옵션 위젯
  Widget _buildThemeOption(
    BuildContext context,
    WidgetRef ref,
    String themeValue,
    String title,
    String subtitle,
    IconData icon,
    bool isSelected,
  ) {
    final theme = Theme.of(context);
    return ListTile(
      leading: Icon(
        icon,
        color: isSelected ? theme.colorScheme.primary : theme.colorScheme.onSurface.withOpacity(0.6),
      ),
      title: Text(
        title,
        style: TextStyle(
          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        ),
      ),
      subtitle: Text(subtitle),
      trailing: isSelected
          ? Icon(Icons.check_circle, color: theme.colorScheme.primary)
          : null,
      selected: isSelected,
      onTap: () async {
        await ref.read(themeProvider.notifier).setTheme(themeValue);
        if (context.mounted) {
          Navigator.pop(context);
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('$title가 적용되었습니다')),
          );
        }
      },
    );
  }

  /// 로그아웃 확인 다이얼로그
  void _showLogoutDialog(BuildContext context, WidgetRef ref) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('로그아웃'),
        content: const Text('정말 로그아웃하시겠습니까?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('취소'),
          ),
          TextButton(
            onPressed: () async {
              final authNotifier = ref.read(authStateProvider.notifier);
              await authNotifier.logout();

              // 모든 Dashboard Provider 상태 초기화
              ref.invalidate(homeProvider);
              ref.invalidate(analysisProvider);
              ref.invalidate(attendanceProvider);
              ref.invalidate(profileProvider);

              if (context.mounted) {
                Navigator.pop(context);
                context.go(AppRouter.login);
              }
            },
            child: const Text(
              '로그아웃',
              style: TextStyle(color: AppTheme.errorColor),
            ),
          ),
        ],
      ),
    );
  }
}
