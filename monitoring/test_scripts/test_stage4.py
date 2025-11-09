#!/usr/bin/env python3
"""
=====================================================================
Reading Buddy - Stage 4.1, 4.2 Monitoring Test Script
=====================================================================
훈련 Stage 4.1과 4.2를 테스트하고 Grafana/Prometheus로 모니터링하기 위한 스크립트

주요 기능:
1. 백엔드 API 자동 호출 및 테스트
2. Prometheus 메트릭 수집 및 노출
3. API 성능 측정 (응답 시간, 성공률 등)
4. 부하 테스트 지원

작성자: SSAFY 13기
날짜: 2024-11
=====================================================================
"""

import requests
import time
import logging
from datetime import datetime
from prometheus_client import Counter, Histogram, Gauge, start_http_server
from colorama import Fore, Style, init
import config

# ============================================================
# 초기 설정
# ============================================================

# Colorama 초기화 (터미널 색상 출력)
init(autoreset=True)

# 로깅 설정 (디버깅 및 문제 추적용)
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# ============================================================
# Prometheus 메트릭 정의
# ============================================================
# Grafana에서 시각화할 메트릭들을 정의합니다

# Counter: API 요청 총 횟수를 카운트 (누적값)
# Labels: stage(4.1/4.2), endpoint(start/set/complete), status(success/fail)
api_request_total = Counter(
    'stage4_api_request_total',
    'Total API requests for Stage 4',
    ['stage', 'endpoint', 'status']
)

# Histogram: API 응답 시간 분포를 측정
# 평균, 중앙값, 백분위수 등을 계산할 수 있음
api_request_duration = Histogram(
    'stage4_api_request_duration_seconds',
    'API request duration for Stage 4',
    ['stage', 'endpoint']
)

# Gauge: 현재 성공률을 표시 (0.0 ~ 1.0 사이 값)
# Counter와 달리 값이 증가/감소할 수 있음
api_success_rate = Gauge(
    'stage4_api_success_rate',
    'API success rate for Stage 4',
    ['stage']
)

# Counter: 생성된 문제 개수를 카운트
problem_count = Counter(
    'stage4_problem_generated_total',
    'Total problems generated for Stage 4',
    ['stage']
)


# ============================================================
# TrainTester 클래스
# ============================================================
class TrainTester:
    """
    훈련 Stage 4.1, 4.2를 테스트하는 메인 클래스

    주요 메서드:
    - login(): 로그인하여 JWT 토큰 획득
    - start_stage(): 스테이지 시작 API 호출
    - get_problem_set(): 문제 세트 생성 API 호출
    - test_stage(): 전체 스테이지 테스트 플로우 실행
    """

    def __init__(self):
        """
        TrainTester 초기화
        config.py의 설정값을 사용하여 초기화
        """
        self.base_url = config.BASE_URL
        self.session = requests.Session()  # 세션 유지 (쿠키, 헤더 등)
        self.token = None

        # 각 스테이지별 성공/실패 통계
        self.stats = {
            '4.1': {'success': 0, 'fail': 0},
            '4.2': {'success': 0, 'fail': 0}
        }

    # --------------------------------------------------------
    # UI 출력 헬퍼 메서드들
    # --------------------------------------------------------

    def print_header(self, text):
        """예쁜 헤더를 출력합니다"""
        print(f"\n{Fore.CYAN}{'='*60}")
        print(f"{Fore.CYAN}{text:^60}")
        print(f"{Fore.CYAN}{'='*60}{Style.RESET_ALL}\n")

    def print_success(self, text):
        """성공 메시지를 녹색으로 출력합니다"""
        print(f"{Fore.GREEN}✓ {text}{Style.RESET_ALL}")

    def print_error(self, text):
        """에러 메시지를 빨간색으로 출력합니다"""
        print(f"{Fore.RED}✗ {text}{Style.RESET_ALL}")

    def print_info(self, text):
        """정보 메시지를 노란색으로 출력합니다"""
        print(f"{Fore.YELLOW}ℹ {text}{Style.RESET_ALL}")

    # --------------------------------------------------------
    # API 호출 메서드들
    # --------------------------------------------------------

    def login(self):
        """
        백엔드 서버에 로그인하여 JWT 토큰을 획득합니다

        Returns:
            bool: 로그인 성공 여부
        """
        self.print_header("로그인 시작")

        try:
            # POST /api/user/login 호출
            response = self.session.post(
                f"{self.base_url}/api/user/login",
                json=config.TEST_USER
            )

            if response.status_code == 200:
                data = response.json()
                # 응답에서 accessToken 추출
                self.token = data.get('data', {}).get('accessToken')

                if self.token:
                    # 이후 모든 요청에 Authorization 헤더 자동 추가
                    self.session.headers.update({
                        'Authorization': f'Bearer {self.token}'
                    })
                    self.print_success(f"로그인 성공! 토큰: {self.token[:20]}...")
                    return True
                else:
                    self.print_error("응답에서 토큰을 찾을 수 없습니다.")
                    return False
            else:
                self.print_error(f"로그인 실패: {response.status_code}")
                print(f"응답: {response.text}")
                return False

        except Exception as e:
            self.print_error(f"로그인 중 오류 발생: {str(e)}")
            logger.exception("Login error")
            return False

    def start_stage(self, stage, total_problems):
        """
        스테이지를 시작하고 세션 ID를 획득합니다

        Args:
            stage (str): 스테이지 번호 ('4.1' 또는 '4.2')
            total_problems (int): 생성할 문제 개수

        Returns:
            str: 스테이지 세션 ID (실패 시 None)
        """
        self.print_info(f"Stage {stage} 시작 중... (문제 수: {total_problems})")

        # 응답 시간 측정 시작
        start_time = time.time()

        try:
            # POST /api/train/stage/start 호출
            response = self.session.post(
                f"{self.base_url}/api/train/stage/start",
                params={
                    'stage': stage,
                    'totalProblems': total_problems
                }
            )

            # 응답 시간 계산
            duration = time.time() - start_time

            # Prometheus 메트릭 기록: API 응답 시간
            api_request_duration.labels(stage=stage, endpoint='start').observe(duration)

            if response.status_code in [200, 201]:
                data = response.json()
                stage_session_id = data.get('data', {}).get('stageSessionId')

                # Prometheus 메트릭 기록: 성공한 요청 카운트 증가
                api_request_total.labels(
                    stage=stage,
                    endpoint='start',
                    status='success'
                ).inc()

                self.print_success(f"Stage 시작 성공! Session ID: {stage_session_id}")
                return stage_session_id
            else:
                # Prometheus 메트릭 기록: 실패한 요청 카운트 증가
                api_request_total.labels(
                    stage=stage,
                    endpoint='start',
                    status='fail'
                ).inc()

                self.print_error(f"Stage 시작 실패: {response.status_code}")
                print(f"응답: {response.text}")
                return None

        except Exception as e:
            # Prometheus 메트릭 기록: 에러 발생 카운트 증가
            api_request_total.labels(
                stage=stage,
                endpoint='start',
                status='error'
            ).inc()

            self.print_error(f"Stage 시작 중 오류: {str(e)}")
            logger.exception("Start stage error")
            return None

    def get_problem_set(self, stage, count, stage_session_id):
        """
        문제 세트를 생성합니다 (핵심 테스트 대상)

        Args:
            stage (str): 스테이지 번호 ('4.1' 또는 '4.2')
            count (int): 생성할 문제 개수
            stage_session_id (str): 스테이지 세션 ID

        Returns:
            list: 생성된 문제 리스트 (실패 시 None)
        """
        self.print_info(f"Stage {stage} 문제 생성 중... (개수: {count})")

        # 응답 시간 측정 시작 - 이 부분이 Grafana에서 중요!
        start_time = time.time()

        try:
            # GET /api/train/set 호출
            # 이 API가 ProblemGenerateService.generateStage4()를 실행함
            response = self.session.get(
                f"{self.base_url}/api/train/set",
                params={
                    'stage': stage,
                    'count': count,
                    'stageSessionId': stage_session_id
                }
            )

            # 응답 시간 계산 (이 값이 Grafana에 표시됨)
            duration = time.time() - start_time

            # Prometheus 메트릭 기록: API 응답 시간
            # Grafana에서 평균, P95, P99 등을 계산할 수 있음
            api_request_duration.labels(stage=stage, endpoint='set').observe(duration)

            if response.status_code in [200, 201]:
                data = response.json()
                problems = data.get('data', {}).get('problems', [])

                # 성공 메트릭 기록
                api_request_total.labels(
                    stage=stage,
                    endpoint='set',
                    status='success'
                ).inc()

                # 생성된 문제 개수 기록
                problem_count.labels(stage=stage).inc(len(problems))

                # 통계 업데이트
                self.stats[stage]['success'] += 1

                self.print_success(f"문제 생성 성공! (응답 시간: {duration:.2f}초)")
                print(f"  생성된 문제 수: {len(problems)}")

                # 생성된 문제 상세 정보 출력
                for i, prob in enumerate(problems, 1):
                    if stage in ['4.1', '4.2']:
                        korean_char = prob.get('koreanChar', 'N/A')
                        phonemes = prob.get('phonemes', [])
                        print(f"  문제 {i}: '{korean_char}' - 음소: {phonemes}")

                return problems
            else:
                # 실패 메트릭 기록
                api_request_total.labels(
                    stage=stage,
                    endpoint='set',
                    status='fail'
                ).inc()

                self.stats[stage]['fail'] += 1
                self.print_error(f"문제 생성 실패: {response.status_code}")
                print(f"응답: {response.text}")
                return None

        except Exception as e:
            # 에러 메트릭 기록
            api_request_total.labels(
                stage=stage,
                endpoint='set',
                status='error'
            ).inc()

            self.stats[stage]['fail'] += 1
            self.print_error(f"문제 생성 중 오류: {str(e)}")
            logger.exception("Get problem set error")
            return None

    def test_stage(self, stage, count):
        """
        특정 스테이지의 전체 플로우를 테스트합니다

        테스트 순서:
        1. 스테이지 시작 (세션 ID 획득)
        2. 문제 세트 생성
        3. 성공률 메트릭 업데이트

        Args:
            stage (str): 스테이지 번호 ('4.1' 또는 '4.2')
            count (int): 생성할 문제 개수

        Returns:
            bool: 테스트 성공 여부
        """
        self.print_header(f"훈련 Stage {stage} 테스트")

        # 1단계: 스테이지 시작
        stage_session_id = self.start_stage(stage, count)
        if not stage_session_id:
            return False

        # 2단계: 문제 세트 생성 (핵심!)
        problems = self.get_problem_set(stage, count, stage_session_id)
        if not problems:
            return False

        # 3단계: 성공률 계산 및 메트릭 업데이트
        total = self.stats[stage]['success'] + self.stats[stage]['fail']
        if total > 0:
            # 성공률 = 성공 횟수 / 전체 시도 횟수
            success_rate = self.stats[stage]['success'] / total
            # Gauge 메트릭 업데이트 (Grafana에서 실시간으로 표시됨)
            api_success_rate.labels(stage=stage).set(success_rate)

        return True

    # --------------------------------------------------------
    # 테스트 시나리오 메서드들
    # --------------------------------------------------------

    def run_basic_test(self):
        """
        기본 기능 테스트: Stage 4.1과 4.2를 각각 1회씩 테스트

        Grafana에서 API가 정상 작동하는지 확인하기 위한 테스트
        """
        self.print_header("기본 기능 테스트 시작")

        # Stage 4.1 테스트
        self.test_stage('4.1', config.TEST_CONFIG['stage_4_1_count'])
        time.sleep(config.TEST_CONFIG['delay_between_requests'])

        # Stage 4.2 테스트
        self.test_stage('4.2', config.TEST_CONFIG['stage_4_2_count'])

    def run_load_test(self):
        """
        부하 테스트: API를 반복 호출하여 성능 측정

        목적:
        - 서버가 연속된 요청을 잘 처리하는지 확인
        - 응답 시간의 변화 추이를 Grafana에서 관찰
        - 메모리 누수, 성능 저하 등을 모니터링
        """
        self.print_header("부하 테스트 시작")
        iterations = config.TEST_CONFIG['load_test_iterations']

        self.print_info(f"{iterations}회 반복 테스트 진행...")

        for i in range(iterations):
            print(f"\n{Fore.MAGENTA}--- 반복 {i+1}/{iterations} ---{Style.RESET_ALL}")

            # Stage 4.1과 4.2를 번갈아가며 테스트
            # 짝수 번째는 4.1, 홀수 번째는 4.2
            stage = '4.1' if i % 2 == 0 else '4.2'
            count = config.TEST_CONFIG[f'stage_{stage.replace(".", "_")}_count']

            self.test_stage(stage, count)

            # 마지막 반복이 아니면 대기
            if i < iterations - 1:
                time.sleep(config.TEST_CONFIG['delay_between_requests'])

    def print_summary(self):
        """
        테스트 결과 요약을 출력합니다

        각 스테이지별 성공/실패 통계와
        Grafana/Prometheus 접속 정보를 표시
        """
        self.print_header("테스트 결과 요약")

        # 스테이지별 통계 출력
        for stage in ['4.1', '4.2']:
            success = self.stats[stage]['success']
            fail = self.stats[stage]['fail']
            total = success + fail

            if total > 0:
                success_rate = (success / total) * 100
                print(f"\n{Fore.CYAN}Stage {stage}:{Style.RESET_ALL}")
                print(f"  성공: {Fore.GREEN}{success}{Style.RESET_ALL}")
                print(f"  실패: {Fore.RED}{fail}{Style.RESET_ALL}")
                print(f"  성공률: {Fore.YELLOW}{success_rate:.1f}%{Style.RESET_ALL}")

        # Prometheus 메트릭 정보
        print(f"\n{Fore.CYAN}Prometheus 메트릭:{Style.RESET_ALL}")
        print(f"  메트릭 서버: http://localhost:{config.METRICS_PORT}/metrics")
        print(f"  확인 가능한 메트릭:")
        print(f"    - stage4_api_request_total (요청 총 횟수)")
        print(f"    - stage4_api_request_duration_seconds (응답 시간)")
        print(f"    - stage4_api_success_rate (성공률)")
        print(f"    - stage4_problem_generated_total (생성된 문제 수)")

        # Grafana 접속 정보
        print(f"\n{Fore.CYAN}Grafana 대시보드:{Style.RESET_ALL}")
        print(f"  URL: http://localhost:3000")
        print(f"  계정: admin / admin")
        print(f"  사용법: Prometheus 데이터소스에서 위 메트릭들을 쿼리하여 그래프 생성\n")


# ============================================================
# 메인 함수
# ============================================================
def main():
    """
    프로그램의 진입점

    실행 순서:
    1. Prometheus 메트릭 서버 시작
    2. 사용자 로그인
    3. 테스트 메뉴 표시 및 실행
    """
    # 타이틀 출력
    print(f"{Fore.CYAN}")
    print("╔═══════════════════════════════════════════════════════════╗")
    print("║     Reading Buddy - Stage 4 Monitoring Test Script      ║")
    print("║              Grafana/Prometheus Integration              ║")
    print("╚═══════════════════════════════════════════════════════════╝")
    print(f"{Style.RESET_ALL}")

    # Prometheus 메트릭 서버 시작
    # 이 서버가 메트릭을 HTTP로 노출하면 Prometheus가 주기적으로 수집함
    print(f"\n{Fore.YELLOW}Starting Prometheus metrics server on port {config.METRICS_PORT}...{Style.RESET_ALL}")
    start_http_server(config.METRICS_PORT)
    print(f"{Fore.GREEN}✓ Metrics server started at http://localhost:{config.METRICS_PORT}/metrics{Style.RESET_ALL}")

    # 테스터 객체 생성
    tester = TrainTester()

    # 로그인 (JWT 토큰 획득)
    if not tester.login():
        print(f"\n{Fore.RED}로그인 실패. config.py에서 계정 정보를 확인하세요.{Style.RESET_ALL}")
        return

    # 인터랙티브 메뉴
    while True:
        print(f"\n{Fore.CYAN}{'='*60}")
        print("테스트 옵션:")
        print(f"{'='*60}{Style.RESET_ALL}")
        print("1. 기본 기능 테스트 (Stage 4.1, 4.2 각 1회)")
        print("2. 부하 테스트 (여러 번 반복 - Grafana 메트릭 수집용)")
        print("3. Stage 4.1만 테스트")
        print("4. Stage 4.2만 테스트")
        print("5. 테스트 결과 요약 보기")
        print("0. 종료")

        choice = input(f"\n{Fore.YELLOW}선택: {Style.RESET_ALL}").strip()

        if choice == '1':
            tester.run_basic_test()
        elif choice == '2':
            tester.run_load_test()
        elif choice == '3':
            tester.test_stage('4.1', config.TEST_CONFIG['stage_4_1_count'])
        elif choice == '4':
            tester.test_stage('4.2', config.TEST_CONFIG['stage_4_2_count'])
        elif choice == '5':
            tester.print_summary()
        elif choice == '0':
            print(f"\n{Fore.CYAN}테스트를 종료합니다.{Style.RESET_ALL}")
            tester.print_summary()
            break
        else:
            print(f"{Fore.RED}잘못된 선택입니다. 다시 선택해주세요.{Style.RESET_ALL}")


# ============================================================
# 프로그램 시작
# ============================================================
if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        # Ctrl+C로 종료했을 때
        print(f"\n\n{Fore.YELLOW}사용자에 의해 중단되었습니다.{Style.RESET_ALL}")
    except Exception as e:
        # 예상치 못한 오류 발생 시
        print(f"\n{Fore.RED}오류 발생: {str(e)}{Style.RESET_ALL}")
        logger.exception("Unexpected error")
