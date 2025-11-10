"""
공통 API 클라이언트 모듈
Stage 테스트에서 사용하는 공통 API 호출 함수들
"""

import requests
import logging

logger = logging.getLogger(__name__)


class TrainAPIClient:
    """
    Train API 호출을 위한 클라이언트
    """

    def __init__(self, base_url):
        self.base_url = base_url
        self.session = requests.Session()
        self.token = None

    def signup(self, email, password, nickname=None):
        """
        회원가입

        Args:
            email: 이메일
            password: 비밀번호
            nickname: 닉네임 (선택, 없으면 이메일 @ 앞부분 사용)

        Returns:
            bool: 성공 여부
        """
        try:
            # 닉네임이 없으면 이메일의 @ 앞부분 사용
            if nickname is None:
                nickname = email.split('@')[0]

            response = self.session.post(
                f"{self.base_url}/api/user/signup",
                json={
                    'email': email,
                    'password': password,
                    'nickname': nickname
                }
            )

            if response.status_code in [200, 201]:
                return True
            elif response.status_code == 409 or "already exists" in response.text.lower():
                # 이미 존재하는 계정
                return True
            else:
                logger.error(f"회원가입 실패: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"회원가입 중 오류: {str(e)}")
            return False

    def login(self, email, password):
        """
        로그인하여 JWT 토큰 획득

        Args:
            email: 이메일
            password: 비밀번호

        Returns:
            bool: 성공 여부
        """
        try:
            response = self.session.post(
                f"{self.base_url}/api/user/login",
                json={
                    'email': email,
                    'password': password
                }
            )

            if response.status_code == 200:
                data = response.json()
                self.token = data.get('data', {}).get('accessToken')

                if self.token:
                    self.session.headers.update({
                        'Authorization': f'Bearer {self.token}'
                    })
                    return True
                else:
                    logger.error("응답에서 토큰을 찾을 수 없음")
                    return False
            else:
                logger.error(f"로그인 실패: {response.status_code}")
                return False

        except Exception as e:
            logger.error(f"로그인 중 오류: {str(e)}")
            return False

    def start_stage(self, stage, total_problems):
        """
        스테이지 시작하고 세션 ID 획득

        Args:
            stage: 스테이지 (예: '4.1', '4.2')
            total_problems: 전체 문제 수

        Returns:
            str or None: 스테이지 세션 ID
        """
        try:
            response = self.session.post(
                f"{self.base_url}/api/train/stage/start",
                params={
                    'stage': stage,
                    'totalProblems': total_problems
                }
            )

            if response.status_code in [200, 201]:
                data = response.json()
                return data.get('data', {}).get('stageSessionId')
            else:
                logger.error(f"Stage 시작 실패: {response.status_code}")
                return None

        except Exception as e:
            logger.error(f"Stage 시작 중 오류: {str(e)}")
            return None

    def get_problem_set(self, stage, count, stage_session_id):
        """
        문제 세트 생성

        Args:
            stage: 스테이지
            count: 문제 개수
            stage_session_id: 스테이지 세션 ID

        Returns:
            list or None: 문제 리스트
        """
        try:
            response = self.session.get(
                f"{self.base_url}/api/train/set",
                params={
                    'stage': stage,
                    'count': count,
                    'stageSessionId': stage_session_id
                }
            )

            if response.status_code in [200, 201]:
                data = response.json()
                return data.get('data', {}).get('problems', [])
            else:
                logger.error(f"문제 생성 실패: {response.status_code}")
                return None

        except Exception as e:
            logger.error(f"문제 생성 중 오류: {str(e)}")
            return None

    def submit_attempt(self, stage_session_id, stage, problem_number, attempt_number, answer, problem, is_correct):
        """
        문제 시도 결과를 서버에 기록

        Args:
            stage_session_id: 스테이지 세션 ID
            stage: 스테이지
            problem_number: 문제 번호
            attempt_number: 시도 번호
            answer: 답안
            problem: 문제 텍스트 (String)
            is_correct: 정답 여부

        Returns:
            bool: 성공 여부
        """
        try:
            # 토큰 확인 (디버그)
            if not self.token:
                logger.error("토큰이 없습니다! 로그인을 먼저 해주세요.")
                return False

            # answer를 명시적으로 문자열로 변환
            answer_str = str(answer) if answer is not None else ""

            request_data = {
                'stageSessionId': stage_session_id,
                'stage': stage,
                'problemNumber': problem_number,
                'answer': answer_str,
                'attemptNumber': attempt_number,
                'problem': problem,
                'isCorrect': is_correct
            }

            # 디버그: 전송할 데이터 로그
            logger.info(f"submit_attempt 요청 데이터: {request_data}")
            logger.info(f"answer 타입: {type(answer_str)}, 값: {answer_str}")

            response = self.session.post(
                f"{self.base_url}/api/train/attempt",
                json=request_data
            )

            if response.status_code in [200, 201]:
                return True
            else:
                logger.error(f"시도 기록 실패: {response.status_code} - {response.text}")
                logger.error(f"Authorization 헤더: {self.session.headers.get('Authorization', 'MISSING')}")
                return False

        except Exception as e:
            logger.error(f"시도 기록 중 오류: {str(e)}")
            return False

    def complete_stage(self, stage_session_id):
        """
        스테이지 완료 처리

        Args:
            stage_session_id: 스테이지 세션 ID

        Returns:
            bool: 성공 여부
        """
        try:
            response = self.session.post(
                f"{self.base_url}/api/train/stage/complete",
                params={
                    'stageSessionId': stage_session_id
                }
            )

            if response.status_code in [200, 201]:
                return True
            else:
                logger.error(f"Stage 완료 실패: {response.status_code}")
                return False

        except Exception as e:
            logger.error(f"Stage 완료 중 오류: {str(e)}")
            return False

    def get_token(self):
        """현재 토큰 반환"""
        return self.token

    def close(self):
        """세션 종료"""
        self.session.close()
