import os
import io
import psycopg2
from psycopg2.extras import RealDictCursor
from gtts import gTTS
import boto3
from botocore.exceptions import ClientError
from dotenv import load_dotenv
import logging
from typing import List, Dict, Optional
import time
from pydub import AudioSegment

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# 환경변수 로드
load_dotenv()


class VoiceGenerator:
    """PostgreSQL에서 단어를 읽어 gTTS로 음성 파일을 생성하고 S3에 업로드하는 클래스"""

    def __init__(self):
        """초기화 및 연결 설정"""
        # PostgreSQL 연결 설정
        self.db_config = {
            'host': os.getenv('DB_HOST', 'localhost'),
            'port': os.getenv('DB_PORT', '5432'),
            'database': os.getenv('DB_NAME', 'postgres'),
            'user': os.getenv('DB_USER', 'postgres'),
            'password': os.getenv('DB_PASSWORD', '0810')
        }

        # S3 설정
        self.s3_client = boto3.client(
            's3',
            aws_access_key_id=os.getenv('AWS_ACCESS_KEY_ID'),
            aws_secret_access_key=os.getenv('AWS_SECRET_ACCESS_KEY'),
            region_name=os.getenv('AWS_REGION', 'ap-northeast-2')
        )
        self.bucket_name = os.getenv('S3_BUCKET_NAME')
        self.s3_prefix = os.getenv('S3_PREFIX', 'voices/')

        # gTTS 설정
        self.language = os.getenv('GTTS_LANGUAGE', 'ko')

        # 재생 속도 설정
        self.slow_speed = float(os.getenv('SLOW_SPEED', '0.75'))  # 느린 속도 배율 (0.75 = 75% 속도)

        # DB 연결
        self.conn = None

    def connect_db(self) -> bool:
        """PostgreSQL 데이터베이스에 연결"""
        try:
            self.conn = psycopg2.connect(**self.db_config)
            logger.info(f"데이터베이스 연결 성공: {self.db_config['database']}")
            return True
        except Exception as e:
            logger.error(f"데이터베이스 연결 실패: {str(e)}")
            return False

    def close_db(self):
        """데이터베이스 연결 종료"""
        if self.conn:
            self.conn.close()
            logger.info("데이터베이스 연결 종료")

    def fetch_words(self, table_name: str = 'letters',
                   text_column: str = 'unicode_point',
                   id_column: str = 'id',
                   where_clause: str = None) -> List[Dict]:
        """
        PostgreSQL에서 단어 데이터를 조회

        Args:
            table_name: 테이블 이름
            text_column: 텍스트가 저장된 컬럼명
            id_column: ID 컬럼명
            where_clause: WHERE 조건절 (예: "processed = false")

        Returns:
            단어 리스트 (딕셔너리 형태)
        """
        try:
            with self.conn.cursor(cursor_factory=RealDictCursor) as cursor:
                query = f"SELECT {id_column}, {text_column} FROM {table_name}"
                print(query)
                cursor.execute(query)
                words = cursor.fetchall()
                logger.info(f"{len(words)}개의 단어를 조회했습니다.")
                return words
        except Exception as e:
            logger.error(f"단어 조회 실패: {str(e)}")
            return []

    def generate_voice_file(self, text: str, slow: bool = False) -> Optional[bytes]:
        """
        gTTS를 사용하여 텍스트를 음성 파일로 변환 (MP3)

        Args:
            text: 변환할 텍스트
            slow: 느린 속도 여부

        Returns:
            음성 파일의 바이트 데이터 (MP3)
        """
        try:
            # gTTS 객체 생성
            tts = gTTS(text=text, lang=self.language, slow=slow)

            # 메모리 버퍼에 저장
            audio_buffer = io.BytesIO()
            tts.write_to_fp(audio_buffer)
            audio_buffer.seek(0)

            speed_type = "느린 속도" if slow else "정상 속도"
            logger.info(f"음성 파일 생성 완료: '{text}' ({speed_type})")
            return audio_buffer.read()
        except Exception as e:
            logger.error(f"음성 파일 생성 실패 (텍스트: '{text}'): {str(e)}")
            return None

    def change_speed(self, audio_data: bytes, speed: float = 0.75) -> Optional[bytes]:
        """
        음성 파일의 재생 속도를 변경 (피치 유지)

        Args:
            audio_data: 원본 오디오 바이트 데이터 (MP3)
            speed: 속도 배율 (0.75 = 75% 속도, 더 느림)

        Returns:
            속도 변경된 오디오 바이트 데이터 (MP3)
        """
        try:
            # MP3 데이터를 AudioSegment로 로드
            audio = AudioSegment.from_mp3(io.BytesIO(audio_data))

            # 속도 변경 (frame rate 조정)
            # 낮은 frame_rate = 느린 재생
            new_frame_rate = int(audio.frame_rate * speed)
            audio_slow = audio._spawn(audio.raw_data, overrides={'frame_rate': new_frame_rate})

            # 원래 frame rate로 되돌려서 피치 유지
            audio_slow = audio_slow.set_frame_rate(audio.frame_rate)

            # MP3로 출력
            output_buffer = io.BytesIO()
            audio_slow.export(output_buffer, format='mp3')
            output_buffer.seek(0)

            logger.info(f"재생 속도 변경 완료 (속도: {speed}x)")
            return output_buffer.read()
        except Exception as e:
            logger.error(f"속도 변경 실패: {str(e)}")
            return None

    def upload_to_s3(self, file_data: bytes, file_name: str,
                    content_type: str = 'audio/mpeg') -> Optional[str]:
        """
        S3에 파일 업로드

        Args:
            file_data: 업로드할 파일 데이터
            file_name: S3에 저장될 파일명
            content_type: 파일의 Content-Type

        Returns:
            S3 URL 또는 None
        """
        try:
            s3_key = f"{self.s3_prefix}{file_name}"

            self.s3_client.put_object(
                Bucket=self.bucket_name,
                Key=s3_key,
                Body=file_data,
                ContentType=content_type
            )

            # S3 URL 생성
            s3_url = f"https://{self.bucket_name}.s3.{os.getenv('AWS_REGION', 'ap-northeast-2')}.amazonaws.com/{s3_key}"
            logger.info(f"S3 업로드 완료: {s3_url}")
            return s3_url
        except ClientError as e:
            logger.error(f"S3 업로드 실패 (파일: {file_name}): {str(e)}")
            return None

    def update_db_record(self, table_name: str, id_column: str,
                        record_id: int,
                        normal_url: str = None,
                        normal_url_column: str = 'voice_url'):
        """
        데이터베이스 레코드에 S3 URL 업데이트

        Args:
            table_name: 테이블 이름
            id_column: ID 컬럼명
            record_id: 업데이트할 레코드의 ID
            normal_url: 정상 속도 S3 URL
            normal_url_column: 정상 속도 URL을 저장할 컬럼명
        """
        try:
            with self.conn.cursor() as cursor:
                # 업데이트할 컬럼 리스트 생성
                updates = []
                values = []

                if normal_url:
                    updates.append(f"{normal_url_column} = %s")
                    values.append(normal_url)

                if not updates:
                    logger.warning(f"업데이트할 URL이 없습니다 (ID: {record_id})")
                    return
                
                # 쿼리 생성
                query = f"""
                UPDATE {table_name}
                SET {', '.join(updates)}
                WHERE {id_column} = %s
                """
                print(query)
                values.append(record_id)

                cursor.execute(query, values)
                self.conn.commit()
                logger.info(f"데이터베이스 업데이트 완료 (ID: {record_id})")
        except Exception as e:
            logger.error(f"데이터베이스 업데이트 실패 (ID: {record_id}): {str(e)}")
            self.conn.rollback()

    def process_words(self, table_name: str = 'letters',
                     text_column: str = 'unicode_point',
                     id_column: str = 'id',
                     normal_url_column: str = 'voice_url',
                     slow_url_column: str = 'voice_url_slow',
                     where_clause: str = None,
                     batch_size: int = 100,
                     delay_seconds: float = 0.5):
        """
        전체 프로세스 실행: DB 조회 -> 음성 생성 (정상/느림) -> S3 업로드 -> DB 업데이트

        Args:
            table_name: 테이블 이름
            text_column: 텍스트 컬럼명
            id_column: ID 컬럼명
            normal_url_column: 정상 속도 URL 저장 컬럼명
            slow_url_column: 느린 속도 URL 저장 컬럼명
            where_clause: WHERE 조건절
            batch_size: 한 번에 처리할 레코드 수
            delay_seconds: 각 요청 사이의 지연 시간 (초)
        """
        # DB 연결
        if not self.connect_db():
            logger.error("데이터베이스 연결 실패로 작업을 중단합니다.")
            return

        try:
            # 단어 조회
            words = self.fetch_words(table_name, text_column, id_column, where_clause)

            if not words:
                logger.info("처리할 단어가 없습니다.")
                return

            total = len(words)
            success_count = 0
            fail_count = 0

            logger.info(f"총 {total}개의 단어 처리를 시작합니다...")
            logger.info(f"출력 포맷: MP3, 느린 속도: {self.slow_speed}x")
            
            print(words)
            for idx, record in enumerate(words, 1):
                record_id = record[id_column]
                text = chr(record[text_column])
                print(record_id, text)

                logger.info(f"[{idx}/{total}] 처리 중: '{text}' (ID: {record_id})")

                normal_url = None
                slow_url = None

                # 1. 정상 속도 음성 파일 생성
                logger.info(f"  → 정상 속도 파일 생성 중...")
                normal_audio = self.generate_voice_file(text, slow=False)

                if normal_audio:
                    # S3에 업로드
                    file_name = f"{record_id}_normal.mp3"
                    normal_url = self.upload_to_s3(normal_audio, file_name, 'audio/mpeg')

                # 2. 느린 속도 음성 파일 생성
                # logger.info(f"  → 느린 속도 파일 생성 중...")
                # slow_audio = self.generate_voice_file(text, slow=False)  # 일단 정상 속도로 생성

                # if slow_audio:
                #     # 속도 조절
                #     slow_audio = self.change_speed(slow_audio, self.slow_speed)

                #     if slow_audio:
                #         # S3에 업로드
                #         file_name = f"{record_id}_{text.replace(' ', '_')}_slow.mp3"
                #         slow_url = self.upload_to_s3(slow_audio, file_name, 'audio/mpeg')

                # 3. DB 업데이트
                if normal_url or slow_url:
                    self.update_db_record(
                        table_name, id_column, record_id,
                        normal_url, 
                        normal_url_column
                    )
                    success_count += 1
                else:
                    fail_count += 1

                # API 속도 제한을 위한 지연
                if idx < total:
                    time.sleep(delay_seconds)

            logger.info(f"""
=== 처리 완료 ===
총 처리: {total}개
성공: {success_count}개
실패: {fail_count}개
출력 포맷: MP3
            """)

        except Exception as e:
            logger.error(f"처리 중 오류 발생: {str(e)}")
        finally:
            self.close_db()


def main():
    """메인 실행 함수"""
    # 환경변수에서 설정 읽기
    table_name = os.getenv('TABLE_NAME', 'words')
    text_column = os.getenv('TEXT_COLUMN', 'word')
    id_column = os.getenv('ID_COLUMN', 'id')
    normal_url_column = os.getenv('NORMAL_URL_COLUMN', 'voice_url')
    slow_url_column = os.getenv('SLOW_URL_COLUMN', 'voice_url_slow')
    where_clause = os.getenv('WHERE_CLAUSE', None)

    # VoiceGenerator 인스턴스 생성 및 실행
    generator = VoiceGenerator()
    generator.process_words(
        table_name=table_name,
        text_column=text_column,
        id_column=id_column,
        normal_url_column=normal_url_column,
        slow_url_column=slow_url_column,
        where_clause=where_clause
    )


if __name__ == "__main__":
    main()
