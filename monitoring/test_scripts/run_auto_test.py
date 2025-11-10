"""
자동 부하 테스트 실행 스크립트
환경변수로 테스트 파라미터를 받아서 비인터랙티브하게 실행
"""

import os
import sys
import logging
from colorama import init

import config
from common import TrainAPIClient, print_header, print_success, print_error, print_info, generate_test_users
from test_multi_user import run_multi_user_test

# Colorama 초기화
init(autoreset=True)

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def main():
    """
    환경변수로부터 설정을 읽어서 자동으로 부하 테스트 실행
    """
    print_header("자동 부하 테스트 시작")

    # 환경변수에서 설정 읽기
    base_url = os.getenv('BASE_URL', config.BASE_URL)
    num_users = int(os.getenv('NUM_USERS', '20'))
    max_workers = int(os.getenv('MAX_WORKERS', '10'))
    stage = os.getenv('STAGE', '4.1')
    problem_count = int(os.getenv('PROBLEM_COUNT', '5'))
    auto_signup = os.getenv('AUTO_SIGNUP', 'true').lower() == 'true'

    # 설정 출력
    print_info("테스트 설정:")
    print(f"  BASE_URL: {base_url}")
    print(f"  NUM_USERS: {num_users}")
    print(f"  MAX_WORKERS: {max_workers}")
    print(f"  STAGE: {stage}")
    print(f"  PROBLEM_COUNT: {problem_count}")
    print(f"  AUTO_SIGNUP: {auto_signup}")
    print()

    # config.BASE_URL 업데이트
    config.BASE_URL = base_url

    try:
        # 부하 테스트 실행
        run_multi_user_test(
            num_users=num_users,
            stage=stage,
            problem_count=problem_count,
            max_workers=max_workers,
            auto_signup=auto_signup
        )
        print_success("테스트가 성공적으로 완료되었습니다!")
        sys.exit(0)

    except Exception as e:
        print_error(f"테스트 실행 중 오류 발생: {str(e)}")
        logger.exception("Test execution error")
        sys.exit(1)


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print_info("\n사용자에 의해 중단되었습니다.")
        sys.exit(0)
