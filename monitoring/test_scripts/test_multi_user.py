import time
import logging
import random
from colorama import init
from concurrent.futures import ThreadPoolExecutor, as_completed
from threading import Lock

import config
from common import TrainAPIClient, print_header, print_success, print_error, print_info, generate_test_users

# Colorama 초기화
init(autoreset=True)

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# 통계를 위한 Lock (스레드 안전성)
stats_lock = Lock()
global_stats = {
    'total_users': 0,
    'success_users': 0,
    'failed_users': 0,
    'total_requests': 0,
    'failed_requests': 0,
    'total_duration': 0
}


class UserSimulator:
    """
    개별 유저를 시뮬레이션하는 클래스
    """

    def __init__(self, user_id, email, password, base_url):
        self.user_id = user_id
        self.email = email
        self.password = password
        self.client = TrainAPIClient(base_url)

    def log(self, level, message):
        """로그 출력"""
        prefix = f"[User {self.user_id}]"
        if level == 'info':
            print_info(f"{prefix} {message}")
        elif level == 'success':
            print_success(f"{prefix} {message}")
        elif level == 'error':
            print_error(f"{prefix} {message}")

    def run_full_flow(self, stage='4.1', problem_count=5, auto_signup=False):
        """
        전체 플로우 실행: (signup) -> login -> start -> set -> attempt -> complete
        """
        start_time = time.time()
        request_count = 0
        failed_count = 0

        self.log('info', f"전체 플로우 시작 (Stage {stage}, {problem_count}문제)")

        # 0. 회원가입 (옵션)
        if auto_signup:
            request_count += 1
            if not self.client.signup(self.email, self.password):
                self.log('error', "회원가입 실패했지만 로그인 시도합니다.")

        # 1. 로그인
        request_count += 1
        if not self.client.login(self.email, self.password):
            failed_count += 1
            self.log('error', "로그인 실패")
            return False, request_count, failed_count, time.time() - start_time

        self.log('success', "로그인 성공")

        # 2. 스테이지 시작
        request_count += 1
        stage_session_id = self.client.start_stage(stage, problem_count)
        if not stage_session_id:
            failed_count += 1
            self.log('error', "스테이지 시작 실패")
            return False, request_count, failed_count, time.time() - start_time

        self.log('success', f"Stage {stage} 시작 (SessionID: {stage_session_id})")

        # 3. 문제 세트 생성
        request_count += 1
        problems = self.client.get_problem_set(stage, problem_count, stage_session_id)
        if not problems:
            failed_count += 1
            self.log('error', "문제 생성 실패")
            return False, request_count, failed_count, time.time() - start_time

        self.log('success', f"문제 생성 성공 ({len(problems)}개)")

        # 4. 각 문제에 대해 attempt 기록
        for i, problem in enumerate(problems, 1):
            request_count += 1
            is_correct = random.random() < 0.8  # 80% 정답률
            answer = ""
            problem_text = problem.get('problemWord', '')  # 문제 텍스트 (String)

            print(problem)
            if not self.client.submit_attempt(stage_session_id, stage, i, 1, answer, problem_text, is_correct):
                failed_count += 1

        self.log('info', f"모든 문제 시도 완료 ({len(problems)}개)")

        # 5. 스테이지 완료
        request_count += 1
        if not self.client.complete_stage(stage_session_id):
            failed_count += 1
            self.log('error', "스테이지 완료 실패")
            return False, request_count, failed_count, time.time() - start_time

        duration = time.time() - start_time
        self.log('success', f"전체 플로우 완료! (소요 시간: {duration:.2f}초)")

        return True, request_count, failed_count, duration


def simulate_user(user_id, email, password, stage, problem_count, auto_signup=False):
    """
    개별 유저 시뮬레이션 함수 (스레드에서 실행됨)
    """
    user = UserSimulator(user_id, email, password, config.BASE_URL)
    success, requests, failures, duration = user.run_full_flow(stage, problem_count, auto_signup)

    # 전역 통계 업데이트 (Thread-safe)
    with stats_lock:
        global_stats['total_users'] += 1
        if success:
            global_stats['success_users'] += 1
        else:
            global_stats['failed_users'] += 1
        global_stats['total_requests'] += requests
        global_stats['failed_requests'] += failures
        global_stats['total_duration'] += duration

    return {
        'user_id': user_id,
        'email': email,
        'success': success,
        'requests': requests,
        'failures': failures,
        'duration': duration
    }


def signup_users(test_users):
    """
    여러 유저를 일괄 회원가입
    """
    print_header("회원 일괄 가입 시작")
    print(f"가입할 유저 수: {len(test_users)}\n")
    
    success_count = 0
    fail_count = 0
    

    for user_info in test_users:
        print(user_info)
        client = TrainAPIClient(config.BASE_URL)
        if client.signup(user_info['email'], user_info['password'], user_info.get('nickname')):
            success_count += 1
            print_success(f"[User {user_info['user_id']}] {user_info['email']} ({user_info.get('nickname', 'N/A')}) 회원가입 성공")
        else:
            fail_count += 1
            print_error(f"[User {user_info['user_id']}] {user_info['email']} 회원가입 실패")
        client.close()
        time.sleep(0.1)  # 서버 부하 방지

    print_header("회원가입 완료")
    print_success(f"성공: {success_count}")
    print_error(f"실패: {fail_count}")


def run_multi_user_test(num_users, stage='4.1', problem_count=5, max_workers=10, auto_signup=False):
    """
    멀티 유저 동시 테스트 실행
    """
    print_header("멀티 유저 부하 테스트 시작")
    print(f"총 유저 수: {num_users}")
    print(f"동시 접속 수: {max_workers}")
    print(f"스테이지: {stage}")
    print(f"문제 개수: {problem_count}\n")

    # 전역 통계 초기화
    global global_stats
    global_stats = {
        'total_users': 0,
        'success_users': 0,
        'failed_users': 0,
        'total_requests': 0,
        'failed_requests': 0,
        'total_duration': 0
    }

    # 테스트 유저 생성
    test_users = generate_test_users(
        config.TEST_USER['email'],
        config.TEST_USER['password'],
        num_users,
        config.TEST_USER.get('nickname', 'testuser')
    )

    start_time = time.time()

    # 회원가입 (옵션)
    if auto_signup:
        signup_users(test_users)
        time.sleep(1)

    # ThreadPoolExecutor로 동시 실행
    results = []
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(
                simulate_user,
                user['user_id'],
                user['email'],
                user['password'],
                stage,
                problem_count,
                False  # auto_signup은 이미 위에서 했으므로 False
            ): user for user in test_users
        }

        for future in as_completed(futures):
            try:
                result = future.result()
                results.append(result)
            except Exception as e:
                logger.exception(f"User simulation error: {e}")

    total_time = time.time() - start_time

    print_test_summary(results, total_time, num_users, max_workers, stage, problem_count)


def print_test_summary(results, total_time, num_users, max_workers, stage, problem_count):
    """테스트 결과 요약 출력"""
    print_header("테스트 결과 요약")

    print("\n[기본 정보]")
    print(f"  총 유저 수: {num_users}")
    print(f"  동시 접속 수: {max_workers}")
    print(f"  스테이지: {stage}")
    print(f"  문제 개수: {problem_count}")
    print(f"  총 소요 시간: {total_time:.2f}초")

    print("\n[유저별 통계]")
    print_success(f"  성공한 유저: {global_stats['success_users']}")
    print_error(f"  실패한 유저: {global_stats['failed_users']}")
    success_rate = (global_stats['success_users'] / global_stats['total_users'] * 100) if global_stats['total_users'] > 0 else 0
    print(f"  성공률: {success_rate:.1f}%")

    print("\n[요청 통계]")
    print(f"  총 요청 수: {global_stats['total_requests']}")
    print_error(f"  실패한 요청: {global_stats['failed_requests']}")
    request_success_rate = ((global_stats['total_requests'] - global_stats['failed_requests']) / global_stats['total_requests'] * 100) if global_stats['total_requests'] > 0 else 0
    print(f"  요청 성공률: {request_success_rate:.1f}%")

    print("\n[성능 통계]")
    avg_duration = global_stats['total_duration'] / global_stats['total_users'] if global_stats['total_users'] > 0 else 0
    print(f"  유저당 평균 소요 시간: {avg_duration:.2f}초")

    durations = [r['duration'] for r in results if r['success']]
    if durations:
        print(f"  최소 소요 시간: {min(durations):.2f}초")
        print(f"  최대 소요 시간: {max(durations):.2f}초")

    throughput = global_stats['total_users'] / total_time if total_time > 0 else 0
    print(f"  처리량 (Throughput): {throughput:.2f} users/sec")

    print("\n[상세 결과 (처음 10명)]")
    for result in sorted(results, key=lambda x: x['user_id'])[:10]:
        if result['success']:
            print_success(f"  User {result['user_id']:3d} ({result['email']:25s}): {result['duration']:.2f}초 ({result['requests']}개 요청, {result['failures']}개 실패)")
        else:
            print_error(f"  User {result['user_id']:3d} ({result['email']:25s}): 실패 - {result['duration']:.2f}초 ({result['requests']}개 요청, {result['failures']}개 실패)")


def main():
    """메인 함수"""
    while True:
        print_header("멀티 유저 부하 테스트")
        print("1. 빠른 테스트 (5명, 동시 5명)")
        print("2. 중간 테스트 (20명, 동시 10명)")
        print("3. 부하 테스트 (50명, 동시 20명)")
        print("4. 커스텀 설정")
        print("5. 회원 일괄 가입 (테스트 유저 생성)")
        print("0. 종료")

        choice = input("\n선택: ").strip()

        if choice == '1':
            run_multi_user_test(num_users=5, stage='4.1', problem_count=5, max_workers=5, auto_signup=True)
        elif choice == '2':
            run_multi_user_test(num_users=20, stage='4.1', problem_count=5, max_workers=10, auto_signup=True)
        elif choice == '3':
            run_multi_user_test(num_users=50, stage='4.1', problem_count=5, max_workers=20, auto_signup=True)
        elif choice == '4':
            try:
                num_users = int(input("총 유저 수: ").strip())
                max_workers = int(input("동시 접속 수: ").strip())
                stage = input("스테이지 (4.1/4.2): ").strip()
                problem_count = int(input("문제 개수: ").strip())
                auto_signup = input("자동 회원가입? (y/n, 기본값: y): ").strip().lower() != 'n'

                run_multi_user_test(
                    num_users=num_users,
                    stage=stage,
                    problem_count=problem_count,
                    max_workers=max_workers,
                    auto_signup=auto_signup
                )
            except ValueError:
                print_error("올바른 값을 입력하세요.")
        elif choice == '5':
            try:
                num_users = int(input("가입할 유저 수: ").strip())
                test_users = generate_test_users(
                    config.TEST_USER['email'],
                    config.TEST_USER['password'],
                    num_users,
                    config.TEST_USER.get('nickname', 'testuser')
                )

                print("\n생성될 계정 목록:")
                for user in test_users[:5]:
                    print(f"  - {user['email']} / {user.get('nickname', 'N/A')}")
                if num_users > 5:
                    print(f"  ... (총 {num_users}개)")

                confirm = input("\n계속 진행하시겠습니까? (y/n): ").strip().lower()
                if confirm == 'y':
                    signup_users(test_users)
                else:
                    print("취소되었습니다.")
            except ValueError:
                print_error("올바른 숫자를 입력하세요.")
        elif choice == '0':
            print_info("테스트를 종료합니다.")
            break
        else:
            print_error("잘못된 선택입니다.")


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print_info("\n사용자에 의해 중단되었습니다.")
    except Exception as e:
        print_error(f"오류 발생: {str(e)}")
        logger.exception("Unexpected error")
