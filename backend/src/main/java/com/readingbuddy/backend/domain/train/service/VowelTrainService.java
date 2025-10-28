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

        // 랜덤 단어 5개 조회
        List<Words> randomWords = wordsRepository.findRandomWords(5);

        // 각 단어의 음소 분해 및 정답 여부 판단
        List<Stage1_2Problem.OptionDto> options = new ArrayList<>();

        // 단어의 각 글자를 음소 분해
        for (Words word : randomWords) {
            boolean containsTargetPhoneme = false;

            for (int i=0; i<word.getWord().length(); i++) {
                int codePoint = word.getWord().codePointAt(i);

                // 음소 분해
                List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(codePoint);

                // phonemes 리스트에 목표 모음이 포함되어 있는지 확인
                if (phonemes.contains(targetVowel)) {
                    containsTargetPhoneme = true;
                    break;
                }
            }

            options.add(new Stage1_2Problem.OptionDto(
                    word.getId(),
                    word.getWord(),
                    word.getVoiceUrl(),
                    containsTargetPhoneme  // 해당 모음이 포함되어 있으면 정답 후보
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
}
