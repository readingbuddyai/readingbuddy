-- KnowledgeComponent 테이블 데이터
INSERT INTO knowledge_component (id, category, stage) VALUES
(1, 'LABIAL', '1.2'),
(2, 'VELAR', '1.2'),
(3, 'ALVEOLAR', '1.2'),
(4, 'PALATAL', '1.2'),
(5, 'ALVEOLAR_FRICATIVE', '1.2'),
(6, 'GLOTTAL_AND_ALVEOLAR', '1.2'),
(7, 'LABIAL_ONSET', '4'),
(8, 'VELAR_ONSET', '4'),
(9, 'ALVEOLAR_ONSET', '4'),
(10, 'PALATAL_ONSET', '4'),
(11, 'ALVEOLAR_FRICATIVE_ONSET', '4'),
(12, 'GLOTTAL_AND_ALVEOLAR_ONSET', '4'),
(13, 'LABIAL_CODA', '4'),
(14, 'VELAR_CODA', '4'),
(15, 'ALVEOLAR_CODA', '4'),
(16, 'PALATAL_CODA', '4'),
(17, 'ALVEOLAR_FRICATIVE_CODA', '4'),
(18, 'GLOTTAL_AND_ALVEOLAR_CODA', '4'),
(19, 'MONOPHTHONG', '1.1'),
(20, 'DIPHTHONG', '1.1'),
(21, 'MONOPHTHONG_NUCLEUS', '4'),
(22, 'DIPHTHONG_NUCLEUS', '4'),
(23, 'CLOSED_SYLLABLE', '3'),
(24, 'OPEN_SYLLABLE', '3')
ON CONFLICT (id) DO NOTHING;

INSERT INTO words (id, word, voice_url) VALUES
                                            (1, '사과', ''),
                                            (2, '바나나', ''),
                                            (3, '고양이', ''),
                                            (4, '강아지', ''),
                                            (5, '코끼리', ''),
                                            (6, '꽃', ''),
                                            (7, '정원', ''),
                                            (8, '집', ''),
                                            (9, '섬', ''),
                                            (10, '정글', ''),
                                            (11, '왕', ''),
                                            (12, '사자', ''),
                                            (13, '산', ''),
                                            (14, '밤', ''),
                                            (15, '바다', ''),
                                            (16, '피아노', ''),
                                            (17, '여왕', ''),
                                            (18, '강', ''),
                                            (19, '태양', ''),
                                            (20, '나무', ''),
                                            (21, '우산', ''),
                                            (22, '마을', ''),
                                            (23, '물', ''),
                                            (24, '노랑', ''),
                                            (25, '얼룩말', ''),
                                            (26, '책', ''),
                                            (27, '의자', ''),
                                            (28, '책상', ''),
                                            (29, '문', ''),
                                            (30, '지구', ''),
                                            (31, '불', ''),
                                            (32, '유리', ''),
                                            (33, '마음', ''),
                                            (34, '얼음', ''),
                                            (35, '기쁨', ''),
                                            (36, '열쇠', ''),
                                            (37, '빛', ''),
                                            (38, '달', ''),
                                            (39, '코', ''),
                                            (40, '오렌지', ''),
                                            (41, '종이', ''),
                                            (42, '조용함', ''),
                                            (43, '비', ''),
                                            (44, '눈', ''),
                                            (45, '테이블', ''),
                                            (46, '우주', ''),
                                            (47, '목소리', ''),
                                            (48, '바람', ''),
                                            (49, '해', ''),
                                            (50, '지역', ''),
                                            (51, '모험', ''),
                                            (52, '다리', ''),
                                            (53, '구름', ''),
                                            (54, '꿈', ''),
                                            (55, '에너지', ''),
                                            (56, '숲', ''),
                                            (57, '기타', ''),
                                            (58, '조화', ''),
                                            (59, '이미지', ''),
                                            (60, '여행', ''),
                                            (61, '지식', ''),
                                            (62, '편지', ''),
                                            (63, '음악', ''),
                                            (64, '자연', ''),
                                            (65, '기회', ''),
                                            (66, '평화', ''),
                                            (67, '질문', ''),
                                            (68, '무지개', ''),
                                            (69, '별', ''),
                                            (70, '시간', ''),
                                            (71, '단결', ''),
                                            (72, '승리', ''),
                                            (73, '지혜', ''),
                                            (74, '청춘', ''),
                                            (75, '정점', ''),
                                            (76, '균형', ''),
                                            (77, '용기', ''),
                                            (78, '운명', ''),
                                            (79, '감정', ''),
                                            (80, '자유', ''),
                                            (81, '우아함', ''),
                                            (82, '희망', ''),
                                            (83, '영감', ''),
                                            (84, '정의', ''),
                                            (85, '친절', ''),
                                            (86, '사랑', ''),
                                            (87, '기억', ''),
                                            (88, '고귀함', ''),
                                            (89, '열정', ''),
                                            (90, '현실', ''),
                                            (91, '정신', ''),
                                            (92, '진실', ''),
                                            (93, '가치', ''),
                                            (94, '경이', ''),
                                            (95, '탁월함', ''),
                                            (96, '믿음', ''),
                                            (97, '성장', ''),
                                            (98, '건강', ''),
                                            (99, '진실성', ''),
                                            (100, '힘', '')
    ON CONFLICT (id) DO NOTHING;

INSERT INTO phonemes (category, value, unicode, image_url, voice_url, knowledge_component_id) VALUES
-- 기본 자음 (14개)
('consonant', 'ㄱ', 'U+3131', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B1.png', NULL, 2),   -- VELAR
('consonant', 'ㄴ', 'U+3134', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B4.png', NULL, 3),   -- ALVEOLAR
('consonant', 'ㄷ', 'U+3137', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B7.png', NULL, 3),   -- ALVEOLAR
('consonant', 'ㄹ', 'U+3139', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B9.png', NULL, 6),   -- GLOTTAL_AND_ALVEOLAR
('consonant', 'ㅁ', 'U+3141', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%81.png', NULL, 1),   -- LABIAL
('consonant', 'ㅂ', 'U+3142', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%82.png', NULL, 1),   -- LABIAL
('consonant', 'ㅅ', 'U+3145', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%85.png', NULL, 5),   -- ALVEOLAR_FRICATIVE
('consonant', 'ㅇ', 'U+3147', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%87.png', NULL, 2),   -- VELAR
('consonant', 'ㅈ', 'U+3148', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%88.png', NULL, 4),   -- PALATAL
('consonant', 'ㅊ', 'U+314A', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8A.png', NULL, 4),   -- PALATAL
('consonant', 'ㅋ', 'U+314B', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8B.png', NULL, 2),   -- VELAR
('consonant', 'ㅌ', 'U+314C', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8C.png', NULL, 3),   -- ALVEOLAR
('consonant', 'ㅍ', 'U+314D', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8D.png', NULL, 1),   -- LABIAL
('consonant', 'ㅎ', 'U+314E', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8E.png', NULL, 6),   -- GLOTTAL_AND_ALVEOLAR
-- 쌍자음 (5개)
('consonant', 'ㄲ', 'U+3132', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B2.png', NULL, 2),   -- VELAR
('consonant', 'ㄸ', 'U+3138', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B8.png', NULL, 3),   -- ALVEOLAR
('consonant', 'ㅃ', 'U+3143', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%83.png', NULL, 1),   -- LABIAL
('consonant', 'ㅆ', 'U+3146', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%86.png', NULL, 5),   -- ALVEOLAR_FRICATIVE
('consonant', 'ㅉ', 'U+3149', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%89.png', NULL, 4)    -- PALATAL
ON CONFLICT (value) DO NOTHING;
