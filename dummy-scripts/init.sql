-- 데이터베이스 초기화 SQL 스크립트
-- Docker Compose의 PostgreSQL 컨테이너에서 자동으로 실행됩니다.

-- words 테이블 생성
CREATE TABLE IF NOT EXISTS words (
    id SERIAL PRIMARY KEY,
    word VARCHAR(255) NOT NULL,
    voice_url TEXT,
    description TEXT,
    category VARCHAR(100),
    language VARCHAR(10) DEFAULT 'ko',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 인덱스 생성 (성능 최적화)
CREATE INDEX IF NOT EXISTS idx_words_voice_url ON words(voice_url);
CREATE INDEX IF NOT EXISTS idx_words_category ON words(category);
CREATE INDEX IF NOT EXISTS idx_words_language ON words(language);

-- updated_at 자동 업데이트 트리거 함수
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 트리거 생성
DROP TRIGGER IF EXISTS update_words_updated_at ON words;
CREATE TRIGGER update_words_updated_at
    BEFORE UPDATE ON words
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 샘플 데이터 삽입 (한국어)
INSERT INTO words (word, category, language) VALUES
    ('안녕하세요', 'greetings', 'ko'),
    ('감사합니다', 'greetings', 'ko'),
    ('환영합니다', 'greetings', 'ko'),
    ('사과', 'fruits', 'ko'),
    ('바나나', 'fruits', 'ko'),
    ('포도', 'fruits', 'ko'),
    ('고양이', 'animals', 'ko'),
    ('강아지', 'animals', 'ko'),
    ('토끼', 'animals', 'ko'),
    ('빨강', 'colors', 'ko'),
    ('파랑', 'colors', 'ko'),
    ('초록', 'colors', 'ko'),
    ('하나', 'numbers', 'ko'),
    ('둘', 'numbers', 'ko'),
    ('셋', 'numbers', 'ko')
ON CONFLICT DO NOTHING;

-- 영어 샘플 데이터 (선택사항)
INSERT INTO words (word, category, language) VALUES
    ('Hello', 'greetings', 'en'),
    ('Thank you', 'greetings', 'en'),
    ('Welcome', 'greetings', 'en'),
    ('Apple', 'fruits', 'en'),
    ('Banana', 'fruits', 'en'),
    ('Grape', 'fruits', 'en')
ON CONFLICT DO NOTHING;

-- 데이터 확인
SELECT
    COUNT(*) as total_words,
    COUNT(CASE WHEN voice_url IS NULL THEN 1 END) as pending_words,
    COUNT(CASE WHEN voice_url IS NOT NULL THEN 1 END) as processed_words
FROM words;

-- 카테고리별 통계
SELECT
    category,
    language,
    COUNT(*) as count
FROM words
GROUP BY category, language
ORDER BY category, language;
