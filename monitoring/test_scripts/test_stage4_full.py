import time
import logging
import random
from colorama import init

import config
from common import TrainAPIClient, print_header, print_success, print_error, print_info

# Colorama 초기화
init(autoreset=True)

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class Stage4Tester:
    """
    Stage 4 전체 플로우 테스터
    login -> stage/start -> problem set -> attempt -> complete
    """

    def __init__(self):
        self.client = TrainAPIClient(config.BASE_URL)

    def run_full_test(self, email, password, stage='4.1', problem_count=5, auto_signup=False):
        """
        전체 플로우 테스트: (signup) -> login -> start -> set -> attempt -> complete
        """
        print_header(f"Stage {stage} 전체 플로우 테스트 시작")

        # 0. 회원가입 (옵션)
        if auto_signup:
            print_header("0단계: 회원가입")
            if self.client.signup(email, password):
                print_success("회원가입 성공")
            else:
                print_error("회원가입 실패 (이미 존재할 수 있음)")
            time.sleep(1)

        # 1. 로그인
        print_header("1단계: 로그인")
        if not self.client.login(email, password):
            print_error("로그인 실패로 테스트를 중단합니다.")
            return False

        print_success(f"로그인 성공! 토큰: {self.client.get_token()[:20]}...")
        time.sleep(1)

        # 2. 스테이지 시작
        print_header("2단계: 스테이지 시작")
        print_info(f"Stage {stage} 시작 중... (문제 수: {problem_count})")

        start_time = time.time()
        stage_session_id = self.client.start_stage(stage, problem_count)
        duration = time.time() - start_time

        if not stage_session_id:
            print_error("스테이지 시작 실패로 테스트를 중단합니다.")
            return False

        print_success(f"Stage 시작 성공! Session ID: {stage_session_id}")
        print_info(f"응답 시간: {duration:.2f}초")
        time.sleep(1)

        # 3. 문제 세트 생성
        print_header("3단계: 문제 세트 생성")
        print_info(f"Stage {stage} 문제 생성 중... (개수: {problem_count})")

        start_time = time.time()
        problems = self.client.get_problem_set(stage, problem_count, stage_session_id)
        duration = time.time() - start_time

        if not problems:
            print_error("문제 생성 실패로 테스트를 중단합니다.")
            return False

        print_success(f"문제 생성 성공! (응답 시간: {duration:.2f}초)")
        print(f"  생성된 문제 수: {len(problems)}")

        # 생성된 문제 상세 정보 출력
        for i, prob in enumerate(problems, 1):
            korean_char = prob.get('koreanChar', 'N/A')
            phonemes = prob.get('phonemes', [])
            print(f"  문제 {i}: '{korean_char}' - 음소: {phonemes}")

        time.sleep(1)

        # 4. 각 문제에 대해 attempt 기록
        print_header("4단계: 문제 시도 기록")

        for i, problem in enumerate(problems, 1):
            # 랜덤으로 정답/오답 결정 (80% 정답률)
            is_correct = random.random() < 0.8
            answer = problem.get('koreanChar', '')

            start_time = time.time()
            success = self.client.submit_attempt(
                stage_session_id=stage_session_id,
                stage=stage,
                problem_number=i,
                answer=answer,
                is_correct=is_correct
            )
            duration = time.time() - start_time

            if success:
                result = "정답" if is_correct else "오답"
                print_success(f"문제 {i} 시도 기록 완료 ({result}, {duration:.2f}초)")
            else:
                print_error(f"문제 {i} 시도 기록 실패")

            time.sleep(0.5)

        time.sleep(1)

        # 5. 스테이지 완료
        print_header("5단계: 스테이지 완료")
        print_info(f"Stage {stage} 완료 처리 중...")

        start_time = time.time()
        if not self.client.complete_stage(stage_session_id):
            print_error("스테이지 완료 실패")
            return False

        duration = time.time() - start_time
        print_success(f"Stage {stage} 완료! (응답 시간: {duration:.2f}초)")

        print_header("테스트 완료!")
        print_success(f"Stage {stage} 전체 플로우 테스트가 성공적으로 완료되었습니다.")
        return True

    def close(self):
        """클라이언트 종료"""
        self.client.close()


def main():
    """메인 함수"""
    tester = Stage4Tester()

    try:
        while True:
            print_header("Stage 4 전체 플로우 테스트")
            print("1. Stage 4.1 테스트 (기본 5문제)")
            print("2. Stage 4.2 테스트 (기본 5문제)")
            print("3. Stage 4.1 커스텀 문제 개수")
            print("4. Stage 4.2 커스텀 문제 개수")
            print("5. 반복 테스트 (부하 테스트)")
            print("0. 종료")

            choice = input("\n선택: ").strip()

            if choice == '1':
                tester.run_full_test(
                    email=config.TEST_USER['email'],
                    password=config.TEST_USER['password'],
                    stage='4.1',
                    problem_count=5
                )
            elif choice == '2':
                tester.run_full_test(
                    email=config.TEST_USER['email'],
                    password=config.TEST_USER['password'],
                    stage='4.2',
                    problem_count=5
                )
            elif choice == '3':
                try:
                    count = int(input("문제 개수 입력: ").strip())
                    tester.run_full_test(
                        email=config.TEST_USER['email'],
                        password=config.TEST_USER['password'],
                        stage='4.1',
                        problem_count=count
                    )
                except ValueError:
                    print_error("올바른 숫자를 입력하세요.")
            elif choice == '4':
                try:
                    count = int(input("문제 개수 입력: ").strip())
                    tester.run_full_test(
                        email=config.TEST_USER['email'],
                        password=config.TEST_USER['password'],
                        stage='4.2',
                        problem_count=count
                    )
                except ValueError:
                    print_error("올바른 숫자를 입력하세요.")
            elif choice == '5':
                try:
                    iterations = int(input("반복 횟수 입력: ").strip())
                    stage = input("스테이지 선택 (4.1/4.2): ").strip()
                    count = int(input("문제 개수 입력: ").strip())

                    print_info(f"반복 테스트 시작 ({iterations}회)")
                    success_count = 0

                    for i in range(iterations):
                        print(f"\n{'='*60}")
                        print(f"반복 {i+1}/{iterations}")
                        print(f"{'='*60}")

                        # 각 반복마다 새로운 tester 인스턴스 생성
                        iteration_tester = Stage4Tester()
                        if iteration_tester.run_full_test(
                            email=config.TEST_USER['email'],
                            password=config.TEST_USER['password'],
                            stage=stage,
                            problem_count=count
                        ):
                            success_count += 1
                        iteration_tester.close()
                        time.sleep(2)

                    print_header("반복 테스트 완료!")
                    print(f"성공: {success_count}/{iterations} ({success_count/iterations*100:.1f}%)")

                except ValueError:
                    print_error("올바른 입력값을 넣어주세요.")
            elif choice == '0':
                print_info("테스트를 종료합니다.")
                break
            else:
                print_error("잘못된 선택입니다. 다시 선택해주세요.")

    finally:
        tester.close()


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print_info("\n사용자에 의해 중단되었습니다.")
    except Exception as e:
        print_error(f"오류 발생: {str(e)}")
        logger.exception("Unexpected error")
