package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.train.dto.result.Stage4Problem;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage3Problem;
import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.repository.LettersRepository;
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

    public List<ProblemResult> extractLetters(String stage, Integer cnt) {
        List<ProblemResult> results = null;

        List<Integer> unicodePoints = random.ints(0, TOTAL)
                .distinct()  // 중복 제거
                .limit(cnt)
                .mapToObj(i -> START + i)
                .toList();

        if (stage.equals("3")) {
            results = generateStage3(unicodePoints);
        } else if (stage.equals("4")) {
            results = generateStage4(unicodePoints);
        }
        return results;
    }

    public List<ProblemResult> generateStage3(List<Integer> unicodePoints) {
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

    public List<ProblemResult> generateStage4(List<Integer> unicodePoints) {
        List<ProblemResult> results = new ArrayList<>();

        for (Integer unicodePoint : unicodePoints) {
            Letters letter = lettersRepository.findByUnicodePoint(unicodePoint)
                    .orElseThrow(() -> new IllegalStateException("Letter not found for unicode point: " + unicodePoint));

            // unicodePoint를 실제 한글 문자로 변환
            String koreanChar = String.valueOf((char) unicodePoint.intValue());

            // PhonemeCounter를 사용하여 음소 분해
            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(unicodePoint);

            results.add(
                    new Stage4Problem(koreanChar, letter.getSlowVoiceUrl(), letter.getCount(), phonemes)
            );
        }
        return results;
    }
}
