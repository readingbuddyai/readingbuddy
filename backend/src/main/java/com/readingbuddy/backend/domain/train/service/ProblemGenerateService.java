package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage3Problem;
import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.repository.LettersRepository;
import jakarta.annotation.PostConstruct;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

@Service
@RequiredArgsConstructor
public class ProblemGenerateService {

    private final LettersRepository lettersRepository;

    private static final int START = 0xAC00; // '가'
    private static final int END   = 0xD7A3; // '힣'
    private static final int TOTAL = END - START + 1;

    private final Random random = new Random();

    /** 한글 완성형 5개 랜덤 추출 */
    public List<ProblemResult> extractLetters(Integer cnt) {
        List<Integer> unicodePoints = random.ints(0, TOTAL)
                .distinct()  // 중복 제거
                .limit(cnt)
                .mapToObj(i -> START + i)
                .toList();

        List<ProblemResult> results = new ArrayList<>();

        for (Integer unicodePoint : unicodePoints) {
            Letters letter = lettersRepository.findByUnicodePoint(unicodePoint)
                    .orElseThrow(() -> new IllegalStateException("Letter not found for unicode point: " + unicodePoint));

            // unicodePoint를 실제 한글 문자로 변환
            String koreanChar = String.valueOf((char) unicodePoint.intValue());

            results.add(
                    new Stage3Problem(koreanChar, letter.getVoiceUrl(), letter.getCount())
            );
        }

        return results;
    }

}
