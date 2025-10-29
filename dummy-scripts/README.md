# Voice Generator with PostgreSQL and S3

PostgreSQL 데이터베이스에서 단어를 읽어 Google Text-to-Speech(gTTS)로 음성 파일을 생성하고 AWS S3에 업로드하는 도구입니다.

## 주요 기능

- 기존 PostgreSQL 데이터베이스에서 단어 데이터 조회
- gTTS를 사용한 한국어/다국어 음성 파일 생성
- AWS S3에 자동 업로드
- 데이터베이스에 S3 URL 자동 업데이트
- Docker를 통한 간편한 배포
- 배치 처리 및 속도 제한
- 에러 처리 및 상세 로깅

## 요구사항

- Docker & Docker Compose
- AWS 계정 (S3 액세스용)
- 기존 PostgreSQL 데이터베이스 (데이터가 이미 있는 상태)

## 빠른 시작

### 1. 환경 설정

`.env.example` 파일을 복사하여 `.env` 파일을 생성하고 **실제 DB 정보와 AWS 정보**를 입력하세요:

```bash
cp .env.example .env
```

### 2. .env 파일 수정

```env
# 기존 PostgreSQL DB 정보
DB_HOST=your-db-host.amazonaws.com  # 또는 localhost
DB_PORT=5432
DB_NAME=your_database
DB_USER=your_user
DB_PASSWORD=your_password

# AWS S3 설정
AWS_ACCESS_KEY_ID=your_aws_access_key
AWS_SECRET_ACCESS_KEY=your_aws_secret_key
AWS_REGION=ap-northeast-2
S3_BUCKET_NAME=your-bucket-name
S3_PREFIX=voices/

# gTTS 설정
GTTS_LANGUAGE=ko

# DB 테이블 설정 (실제 테이블 구조에 맞게 수정)
TABLE_NAME=words
TEXT_COLUMN=word
ID_COLUMN=id
URL_COLUMN=voice_url

# 아직 음성이 없는 데이터만 처리
WHERE_CLAUSE=voice_url IS NULL
```

### 3. 데이터베이스 준비 (필요시)

만약 테이블에 `voice_url` 컬럼이 없다면 추가하세요:

```sql
-- voice_url 컬럼 추가
ALTER TABLE your_table_name
ADD COLUMN voice_url TEXT;

-- updated_at 컬럼 추가 (선택사항)
ALTER TABLE your_table_name
ADD COLUMN updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
```

### 4. Docker로 실행

```bash
# 빌드 및 실행
docker-compose up --build

# 백그라운드로 실행
docker-compose up -d

# 로그 확인
docker-compose logs -f voice-generator
```

### 5. 로컬에서 실행 (Docker 없이)

```bash
# 의존성 설치
pip install -r requirements.txt

# 실행
python generate_voice.py
```

## 프로젝트 구조

```
dummy-scripts/
├── generate_voice.py      # 메인 Python 스크립트
├── requirements.txt       # Python 패키지 의존성
├── Dockerfile            # Docker 이미지 빌드 파일
├── docker-compose.yml    # Docker Compose 설정 (기존 DB 사용)
├── .env.example          # 환경변수 템플릿
├── .gitignore            # Git 제외 파일
├── init.sql              # DB 초기화 SQL (참고용)
└── README.md             # 문서
```

## 환경변수 상세 설명

| 환경변수 | 설명 | 예시 |
|---------|------|------|
| `DB_HOST` | PostgreSQL 호스트 주소 | `localhost` 또는 `your-db.amazonaws.com` |
| `DB_PORT` | PostgreSQL 포트 | `5432` |
| `DB_NAME` | 데이터베이스 이름 | `mydb` |
| `DB_USER` | DB 사용자명 | `postgres` |
| `DB_PASSWORD` | DB 비밀번호 | `yourpassword` |
| `AWS_ACCESS_KEY_ID` | AWS 액세스 키 | `AKIA...` |
| `AWS_SECRET_ACCESS_KEY` | AWS 시크릿 키 | `your-secret` |
| `AWS_REGION` | AWS 리전 | `ap-northeast-2` |
| `S3_BUCKET_NAME` | S3 버킷 이름 | `my-voice-bucket` |
| `S3_PREFIX` | S3 파일 경로 접두사 | `voices/` |
| `GTTS_LANGUAGE` | 음성 언어 코드 | `ko`, `en`, `ja`, `zh` |
| `GTTS_SLOW` | 느린 음성 여부 | `True` 또는 `False` |
| `TABLE_NAME` | 데이터를 가져올 테이블명 | `words` |
| `TEXT_COLUMN` | 음성으로 변환할 텍스트 컬럼 | `word`, `text` |
| `ID_COLUMN` | Primary Key 컬럼 | `id`, `word_id` |
| `URL_COLUMN` | S3 URL을 저장할 컬럼 | `voice_url`, `audio_url` |
| `WHERE_CLAUSE` | SQL WHERE 조건 (선택사항) | `voice_url IS NULL` |

## 사용 예시

### 특정 조건의 데이터만 처리

```env
# 아직 음성이 생성되지 않은 단어만
WHERE_CLAUSE=voice_url IS NULL

# 특정 카테고리의 단어만
WHERE_CLAUSE=category = 'animals' AND voice_url IS NULL

# 특정 ID 범위
WHERE_CLAUSE=id BETWEEN 100 AND 200

# 모든 데이터 (조건 없음)
WHERE_CLAUSE=
```

### 다른 언어로 음성 생성

```env
GTTS_LANGUAGE=en  # 영어
GTTS_LANGUAGE=ja  # 일본어
GTTS_LANGUAGE=zh  # 중국어
```

### Python 코드로 직접 사용

```python
from generate_voice import VoiceGenerator

# 인스턴스 생성
generator = VoiceGenerator()

# 커스텀 설정으로 실행
generator.process_words(
    table_name='my_words',
    text_column='text',
    id_column='word_id',
    url_column='audio_url',
    where_clause="processed = false"
)
```

## S3 버킷 설정

S3 버킷에 대한 적절한 IAM 권한이 필요합니다:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::your-bucket-name",
        "arn:aws:s3:::your-bucket-name/*"
      ]
    }
  ]
}
```

## 동작 방식

1. PostgreSQL 데이터베이스에서 설정된 WHERE 조건에 맞는 데이터를 조회
2. 각 레코드의 텍스트를 gTTS로 음성 파일(MP3)로 변환
3. 생성된 MP3 파일을 S3에 업로드 (`{S3_PREFIX}{id}_{text}.mp3`)
4. 데이터베이스의 해당 레코드에 S3 URL을 업데이트
5. 다음 레코드로 이동 (API 속도 제한을 위해 0.5초 대기)

## 트러블슈팅

### 데이터베이스 연결 실패

```
데이터베이스 연결 실패: could not connect to server
```

**해결 방법:**
- `.env` 파일의 `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` 확인
- Docker 사용 시 `network_mode: host`가 설정되어 있는지 확인
- 방화벽 및 보안 그룹 설정 확인
- PostgreSQL이 실행 중인지 확인

### S3 업로드 실패

```
S3 업로드 실패: An error occurred (AccessDenied) when calling the PutObject operation
```

**해결 방법:**
- AWS 자격증명 확인 (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
- S3 버킷 권한 확인 (PutObject 권한 필요)
- 버킷 리전 설정 확인 (`AWS_REGION`)
- 버킷 이름 확인

### gTTS 오류

```
음성 파일 생성 실패: Connection error
```

**해결 방법:**
- 인터넷 연결 확인 (gTTS는 Google API를 사용)
- 언어 코드가 유효한지 확인
- 텍스트가 너무 길지 않은지 확인 (최대 5000자)
- 일시적 오류일 수 있으니 재시도

### 컬럼 없음 오류

```
column "voice_url" does not exist
```

**해결 방법:**
```sql
ALTER TABLE your_table_name ADD COLUMN voice_url TEXT;
```

## 로그 확인

### Docker 사용 시

```bash
# 실시간 로그 확인
docker-compose logs -f voice-generator

# 최근 100줄 로그
docker-compose logs --tail=100 voice-generator
```

### 로컬 실행 시

표준 출력으로 로그가 출력됩니다.

## 성능 최적화

- 기본적으로 각 요청 사이에 0.5초 지연이 있습니다 (gTTS API 보호)
- 대량의 데이터를 처리할 때는 WHERE 조건으로 배치를 나누는 것을 권장
- S3 업로드는 메모리 버퍼를 사용하여 디스크 I/O를 최소화

## 라이선스

MIT

## 기여

이슈 및 풀 리퀘스트를 환영합니다!
