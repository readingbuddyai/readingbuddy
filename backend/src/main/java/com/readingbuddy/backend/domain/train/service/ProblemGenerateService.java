package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.bkt.entity.LettersKcMap;
import com.readingbuddy.backend.domain.bkt.repository.LettersKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.result.*;
import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.entity.Words;
import com.readingbuddy.backend.domain.train.repository.LettersRepository;
import com.readingbuddy.backend.domain.train.repository.WordsRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

@Service
@RequiredArgsConstructor
public class ProblemGenerateService {

    private final LettersRepository lettersRepository;
    private final WordsRepository wordsRepository;
    private final BktService bktService;
    private final LettersKcMapRepository lettersKcMapRepository;

    private static final int START = 0xAC00; // '가'
    private static final int END   = 0xD7A3; // '힣'
    private static final int TOTAL = END - START + 1;

    private final Random random = new Random();

    public List<ProblemResult> extractLetters(String stage, Integer cnt, Long userId) {
        List<ProblemResult> results = null;
        
        // TODO: 문제 뽑는 건 각각의 generateStage 안으로 삽입
        List<Integer> unicodePoints = lettersRepository.findRandomLetters(cnt);

        if (stage.equals("3")) {
            results = generateStage3(userId);
        } else if (stage.equals("4")) {
            results = generateStage4(unicodePoints);
        }
        return results;
    }

    public List<ProblemResult> generateStage3(Long userId) {
        List<ProblemResult> results = new ArrayList<>();

        List<KcWithCorrectRate> kcList = bktService.getLowestCorrectRateKcsByStage(userId, "3");

        // 해당 kc를 토대로 문제 구성
        for (KcWithCorrectRate kcWithRate : kcList) {
            Long kcId = kcWithRate.getKnowledgeComponent().getId();

            // 해당 KC에 매핑된 Letters 조회
            List<LettersKcMap> lettersKcMaps = lettersKcMapRepository.findByKnowledgeComponentId(kcId);

            if (!lettersKcMaps.isEmpty()) {
                // 매핑된 Letters 중 랜덤으로 하나 선택
                LettersKcMap selectedMap = lettersKcMaps.get(random.nextInt(lettersKcMaps.size()));
                Letters letter = selectedMap.getLetters();

                // unicodePoint를 실제 한글 문자로 변환
                String koreanChar = String.valueOf((char) letter.getUnicodePoint().intValue());

                results.add(
                        new Stage3Problem(koreanChar, letter.getVoiceUrl(), letter.getCount())
                );
            }
        }

        return results;
    }

    public List<ProblemResult> generateStage4(List<Integer> unicodePoints) {
        List<ProblemResult> results = new ArrayList<>();

        // TODO: userId와 stage로 해당 stage에서 부족한 KC를 뽑는다.

        // TODO: 해당 kc를 토대로 문제 구성

        for (Integer unicodePoint : unicodePoints) {
            Letters letter = lettersRepository.findByUnicodePoint(unicodePoint)
                    .orElseThrow(() -> new IllegalStateException("Letter not found for unicode point: " + unicodePoint));

            // unicodePoint를 실제 한글 문자로 변환
            String koreanChar = String.valueOf((char) unicodePoint.intValue());

            // PhonemeCounter를 사용하여 음소 분해
            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(unicodePoint);

            results.add(
                    new Stage4Problem(koreanChar, letter.getSlowVoiceUrl(), letter.getVoiceUrl(), letter.getCount(), phonemes)
            );
        }
        return results;
    }

    public List<ProblemResult> extractWords(Integer cnt) {
        List<ProblemResult> results = new ArrayList<>();

        List<Integer> wordsList = random.ints(0, 100)
                .distinct()  // 중복 제거
                .limit(cnt)
                .boxed()
                .toList();

        for (Integer word : wordsList) {
            Words words = wordsRepository.findById((long) word)
                    .orElseThrow(() -> new IllegalStateException("Word not found for word: " + word));
            results.add(
                    new Stage2Problem(words.getWord(), words.getVoiceUrl(), words.getWord().length())
            );
        }

        return results;
    }
}
