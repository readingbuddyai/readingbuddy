# 빠른 시작 가이드

## 3분 안에 시작하기

### 1단계: 환경변수 설정 (1분)

```bash
# .env 파일 생성
cp .env.example .env

# .env 파일 수정 (vi, nano, 또는 텍스트 에디터 사용)
nano .env
```

**필수 설정 항목:**
```env
# 기존 PostgreSQL 정보
DB_HOST=localhost
DB_NAME=mydb
DB_USER=postgres
DB_PASSWORD=mypassword

# AWS S3 정보
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=your_secret_key
S3_BUCKET_NAME=my-bucket

# 테이블 정보
TABLE_NAME=words
TEXT_COLUMN=word
ID_COLUMN=id
```

### 2단계: 테이블 확인 (1분)

```sql
-- voice_url 컬럼이 있는지 확인
\d your_table_name

-- 없다면 추가
ALTER TABLE your_table_name ADD COLUMN voice_url TEXT;

-- 데이터 확인
SELECT id, word, voice_url FROM your_table_name LIMIT 5;
```

### 3단계: 실행 (1분)

#### Docker 사용
```bash
docker-compose up --build
```

#### 로컬 실행
```bash
pip install -r requirements.txt
python generate_voice.py
```

## 완료!

로그를 확인하면서 다음을 확인하세요:
- DB 연결 성공
- 단어 조회 완료
- 음성 파일 생성
- S3 업로드 완료
- DB 업데이트 완료

## 다음 단계

- README.md에서 고급 설정 확인
- WHERE_CLAUSE로 특정 데이터만 처리
- 여러 언어 지원 설정
