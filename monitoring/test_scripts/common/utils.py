"""
공통 유틸리티 함수
"""

from colorama import Fore, Style


def print_header(text):
    """예쁜 헤더를 출력합니다"""
    print(f"\n{Fore.CYAN}{'='*60}")
    print(f"{Fore.CYAN}{text:^60}")
    print(f"{Fore.CYAN}{'='*60}{Style.RESET_ALL}\n")


def print_success(text):
    """성공 메시지를 녹색으로 출력합니다"""
    print(f"{Fore.GREEN}✓ {text}{Style.RESET_ALL}")


def print_error(text):
    """에러 메시지를 빨간색으로 출력합니다"""
    print(f"{Fore.RED}✗ {text}{Style.RESET_ALL}")


def print_info(text):
    """정보 메시지를 노란색으로 출력합니다"""
    print(f"{Fore.YELLOW}ℹ {text}{Style.RESET_ALL}")


def generate_test_users(base_email, password, num_users, base_nickname=None):
    """
    테스트 유저 정보 생성

    Args:
        base_email: 기본 이메일 (예: 'user@example.com')
        password: 비밀번호
        num_users: 생성할 유저 수
        base_nickname: 기본 닉네임 (선택, 없으면 이메일 @ 앞부분 사용)

    Returns:
        list: 유저 정보 리스트
    """
    test_users = []
    email_parts = base_email.split('@')

    # 기본 닉네임이 없으면 이메일의 @ 앞부분 사용
    if base_nickname is None:
        base_nickname = email_parts[0]

    for i in range(num_users):
        email = f"{email_parts[0]}{i+1}@{email_parts[1]}"
        nickname = f"{base_nickname}{i+1}"
        print(nickname)
        test_users.append({
            'user_id': i + 1,
            'email': email,
            'password': password,
            'nickname': nickname
        })
    return test_users
