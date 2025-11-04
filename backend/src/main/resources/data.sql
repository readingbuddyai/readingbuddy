-- KnowledgeComponent 테이블 데이터
INSERT INTO knowledge_component (id, category, stage) VALUES
(1, 'LABIAL', '1.2.2'),
(2, 'VELAR', '1.2.1'),
(3, 'ALVEOLAR', '1.2'),
(4, 'PALATAL', '1.2'),
(5, 'ALVEOLAR_FRICATIVE', '1.2'),
(6, 'GLOTTAL_AND_ALVEOLAR', '1.1'),
(7, 'LABIAL_ONSET', '1.2'),
(8, 'VELAR_ONSET', '1.2'),
(9, 'ALVEOLAR_ONSET', '1.2'),
(10, 'PALATAL_ONSET', '1.2'),
(11, 'ALVEOLAR_FRICATIVE_ONSET', '1.2'),
(12, 'GLOTTAL_AND_ALVEOLAR_ONSET', '1.2'),
(13, 'LABIAL_CODA', '1.3'),
(14, 'VELAR_CODA', '1.3'),
(15, 'ALVEOLAR_CODA', '1.3'),
(16, 'PALATAL_CODA', '1.3'),
(17, 'ALVEOLAR_FRICATIVE_CODA', '1.3'),
(18, 'GLOTTAL_AND_ALVEOLAR_CODA', '1.3'),
(19, 'MONOPHTHONG', '1.4'),
(20, 'DIPHTHONG', '1.4'),
(21, 'MONOPHTHONG_NUCLEUS', '1.5'),
(22, 'DIPHTHONG_NUCLEUS', '1.5'),
(23, 'CLOSED_SYLLABLE', '1.6'),
(24, 'OPEN_SYLLABLE', '1.6'),
ON CONFLICT (id) DO NOTHING;

-- Letters 테이블 데이터 (자음)
INSERT INTO letters (id, unicode, unicode_point, count, voice_url, slow_voice_url) VALUES
('ㄱ', 'U+3131', 12593, 1, '', ''),
('ㄲ', 'U+3132', 12594, 1, '', ''),
('ㄴ', 'U+3134', 12596, 1, '', ''),
('ㄷ', 'U+3137', 12599, 1, '', ''),
('ㄸ', 'U+3138', 12600, 1, '', ''),
('ㄹ', 'U+3139', 12601, 1, '', ''),
('ㅁ', 'U+3141', 12609, 1, '', ''),
('ㅂ', 'U+3142', 12610, 1, '', ''),
('ㅃ', 'U+3143', 12611, 1, '', ''),
('ㅅ', 'U+3145', 12613, 1, '', ''),
('ㅆ', 'U+3146', 12614, 1, '', ''),
('ㅇ', 'U+3147', 12615, 1, '', ''),
('ㅈ', 'U+3148', 12616, 1, '', ''),
('ㅉ', 'U+3149', 12617, 1, '', ''),
('ㅊ', 'U+314A', 12618, 1, '', ''),
('ㅋ', 'U+314B', 12619, 1, '', ''),
('ㅌ', 'U+314C', 12620, 1, '', ''),
('ㅍ', 'U+314D', 12621, 1, '', ''),
('ㅎ', 'U+314E', 12622, 1, '', '')
ON CONFLICT (id) DO NOTHING;

-- LetterKcMap 테이블 데이터 (자음과 KC 매핑)
INSERT INTO letter_kc_map (id, knowledge_component, letters) VALUES
(1, 2, 'ㄱ'),   -- VELAR
(2, 2, 'ㄲ'),   -- VELAR
(3, 3, 'ㄴ'),   -- ALVEOLAR
(4, 3, 'ㄷ'),   -- ALVEOLAR
(5, 3, 'ㄸ'),   -- ALVEOLAR
(6, 6, 'ㄹ'),   -- GLOTTAL_AND_ALVEOLAR
(7, 1, 'ㅁ'),   -- LABIAL
(8, 1, 'ㅂ'),   -- LABIAL
(9, 1, 'ㅃ'),   -- LABIAL
(10, 5, 'ㅅ'),  -- ALVEOLAR_FRICATIVE
(11, 5, 'ㅆ'),  -- ALVEOLAR_FRICATIVE
(12, 2, 'ㅇ'),  -- VELAR
(13, 4, 'ㅈ'),  -- PALATAL
(14, 4, 'ㅉ'),  -- PALATAL
(15, 4, 'ㅊ'),  -- PALATAL
(16, 2, 'ㅋ'),  -- VELAR
(17, 3, 'ㅌ'),  -- ALVEOLAR
(18, 1, 'ㅍ'),  -- LABIAL
(19, 6, 'ㅎ')   -- GLOTTAL_AND_ALVEOLAR
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

INSERT INTO phonemes (category, value, unicode, image_url, voice_url, letter_kc_map_id) VALUES
-- 기본 자음 (14개)
('consonant', 'ㄱ', 'U+3131', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B1.png', NULL, 1),   -- VELAR
('consonant', 'ㄴ', 'U+3134', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B4.png', NULL, 3),   -- ALVEOLAR
('consonant', 'ㄷ', 'U+3137', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B7.png', NULL, 4),   -- ALVEOLAR
('consonant', 'ㄹ', 'U+3139', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B9.png', NULL, 6),   -- GLOTTAL_AND_ALVEOLAR
('consonant', 'ㅁ', 'U+3141', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%81.png', NULL, 7),   -- LABIAL
('consonant', 'ㅂ', 'U+3142', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%82.png', NULL, 8),   -- LABIAL
('consonant', 'ㅅ', 'U+3145', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%85.png', NULL, 10),  -- ALVEOLAR_FRICATIVE
('consonant', 'ㅇ', 'U+3147', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%87.png', NULL, 12),  -- VELAR
('consonant', 'ㅈ', 'U+3148', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%88.png', NULL, 13),  -- PALATAL
('consonant', 'ㅊ', 'U+314A', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8A.png', NULL, 15),  -- PALATAL
('consonant', 'ㅋ', 'U+314B', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8B.png', NULL, 16),  -- VELAR
('consonant', 'ㅌ', 'U+314C', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8C.png', NULL, 17),  -- ALVEOLAR
('consonant', 'ㅍ', 'U+314D', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8D.png', NULL, 18),  -- LABIAL
('consonant', 'ㅎ', 'U+314E', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%8E.png', NULL, 19),  -- GLOTTAL_AND_ALVEOLAR
-- 쌍자음 (5개)
('consonant', 'ㄲ', 'U+3132', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B2.png', NULL, 2),   -- VELAR
('consonant', 'ㄸ', 'U+3138', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%84%B8.png', NULL, 5),   -- ALVEOLAR
('consonant', 'ㅃ', 'U+3143', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%83.png', NULL, 9),   -- LABIAL
('consonant', 'ㅆ', 'U+3146', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%86.png', NULL, 11),  -- ALVEOLAR_FRICATIVE
('consonant', 'ㅉ', 'U+3149', 'https://final-a206.s3.ap-northeast-2.amazonaws.com/consonantMouseImage/consonant_%E3%85%89.png', NULL, 14)   -- PALATAL
ON CONFLICT (value) DO NOTHING;
