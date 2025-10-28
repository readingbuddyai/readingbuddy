package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_1Problem;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_2Problem;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.entity.Words;
import com.readingbuddy.backend.domain.train.repository.PhonemesRepository;
import com.readingbuddy.backend.domain.train.repository.WordsRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Service
@Transactional
@RequiredArgsConstructor
public class VowelTrainService {

    private final PhonemesRepository phonemesRepository;
    private final WordsRepository wordsRepository;

    /**
     * 모음 기초 단계 문제 생성 (Stage 1.1)
     */
    public ProblemResult getBasicProblem() {
        // 정답 모음 1개 조회
        Phonemes answerVowel = phonemesRepository.findOneRandomVowelForQuestion();

        // 정답을 제외한 오답 모음 1개 조회
        Phonemes wrongVowel = phonemesRepository.findRandomVowel(answerVowel.getId());

        // 선택지 리스트 생성
        List<Phonemes> options = new ArrayList<>();
        options.add(answerVowel);
        options.add(wrongVowel);

        // 선택시 섞기
        Collections.shuffle(options);

        // DTO로 변환
        List<Stage1_1Problem.OptionDto> optionDtos = options.stream()
                .map(vowel -> new Stage1_1Problem.OptionDto(
                        vowel.getId(),
                        vowel.getValue(),
                        vowel.getUnicode()
                )).toList();

        return new Stage1_1Problem(
                answerVowel.getValue(),
                answerVowel.getId(),
                answerVowel.getVoiceUrl(),
                answerVowel.getImageUrl(),
                optionDtos
        );
    }

    /**
     * 모음 심화 단계 문제 생성 (Stage 1.2)
     */
    public ProblemResult getAdvancedProblem() {
        Phonemes targetPhoneme = phonemesRepository.findOneRandomVowelForQuestion();
        char targetVowel = targetPhoneme.getValue().charAt(0);

        // 더 많은 단어 pool에서 조회 (20개)
        List<Words> wordPool = wordsRepository.findRandomWordsPool();

        List<Words> correctWords = new ArrayList<>();
        List<Words> wrongWords = new ArrayList<>();

        // 단어를 정답/오답으로 분류
        for (Words word : wordPool) {
            if (checkWordContainsPhoneme(word.getWord(), targetVowel)) {
                correctWords.add(word);
            } else {
                wrongWords.add(word);
            }
        }

        List<Words> selectedWords = new ArrayList<>();

        // 정답 최소 1개 보장
        if (!correctWords.isEmpty()) {
            selectedWords.add(correctWords.get(0));
        }

        // 나머지는 랜덤으로 채우기 (정답/오답 섞여서)
        Collections.shuffle(wordPool);
        for (Words word : wordPool) {
            if (selectedWords.size() >= 5) break;
            if (!selectedWords.contains(word)) {
                selectedWords.add(word);
            }
        }

        // 각 단어마다 isAnswer 플래그 설정
        List<Stage1_2Problem.OptionDto> options = new ArrayList<>();
        for (Words word : selectedWords) {
            boolean isAnswer = checkWordContainsPhoneme(word.getWord(), targetVowel);
            options.add(new Stage1_2Problem.OptionDto(
                    word.getId(),
                    word.getWord(),
                    word.getVoiceUrl(),
                    isAnswer
            ));
        }

        Collections.shuffle(options);

        return new Stage1_2Problem(
                targetPhoneme.getValue(),
                targetPhoneme.getId(),
                targetPhoneme.getValue(),
                targetPhoneme.getVoiceUrl(),
                options
        );
    }

    /**
     * 단어가 특정 모음을 포함하는지 확인
     */
    private boolean checkWordContainsPhoneme(String word, char targetVowel) {
        for (int i = 0; i < word.length(); i++) {
            int codePoint = word.codePointAt(i);

            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(codePoint);

            if (phonemes.contains(targetVowel)) {
                return true;
            }
        }
        return false;
    }
}
