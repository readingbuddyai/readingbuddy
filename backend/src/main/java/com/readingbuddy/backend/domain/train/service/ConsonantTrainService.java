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
public class ConsonantTrainService {

    private final PhonemesRepository phonemesRepository;
    private final WordsRepository wordsRepository;

    /**
     * 자음 기초 단계 문제 생성 (Stage 1.2.1)
     */
    public ProblemResult getBasicProblem() {
        // TODO: random 조회가 아닌 kc에 대한 mastery값 바탕으로 문제 생성
        // 정답 자음 1개 조회
        Phonemes answerConsonant = phonemesRepository.findOneRandomConsonantForQuestion();

        // 정답을 제외한 오답 자음 1개 조회
        Phonemes wrongConsonant = phonemesRepository.findRandomConsonant(answerConsonant.getId());

        // 선택지 리스트 생성
        List<Phonemes> options = new ArrayList<>();
        options.add(answerConsonant);
        options.add(wrongConsonant);

        // 선택지 섞기
        Collections.shuffle(options);

        // DTO로 변환
        List<Stage1_1Problem.OptionDto> optionDtos = options.stream()
                .map(consonant -> Stage1_1Problem.OptionDto.builder()
                        .id(consonant.getId())
                        .value(consonant.getValue())
                        .unicode(consonant.getUnicode())
                        .build()
                ).toList();

        return Stage1_1Problem.builder()
                .problemWord(answerConsonant.getValue())
                .imageUrl(answerConsonant.getImageUrl())
                .phonemeId(answerConsonant.getId())
                .voiceUrl(answerConsonant.getVoiceUrl())
                .options(optionDtos)
                .build();
    }

    /**
     * 자음 심화 단계 문제 생성 (Stage 1.2.2)
     */
    public ProblemResult getAdvancedProblem() {
        // TODO: random 조회가 아닌 kc에 대한 mastery값 바탕으로 문제 생성
        Phonemes targetPhoneme = phonemesRepository.findOneRandomConsonantForQuestion();
        char targetConsonant = targetPhoneme.getValue().charAt(0);

        List<Words> allWords = wordsRepository.findAll();
        Collections.shuffle(allWords);

        List<Words> selectedWords = new ArrayList<>();
        boolean foundCorrect = false;

        // 전체를 돌면서 처음 2개를 바로 선택, 정답은 따로 찾기
        for (Words word : allWords) {
            if (foundCorrect && selectedWords.size() == 3) {
                break;
            }

            if (selectedWords.size() < 2) {
                if (!checkWordContainsPhoneme(word.getWord(), targetConsonant)) {
                    selectedWords.add(word);
                }
            }

            // 정답을 아직 못 찾았으면 계속 찾기, 찾으면 추가
            else if(!foundCorrect && checkWordContainsPhoneme(word.getWord(), targetConsonant)) {
                foundCorrect = true;
                selectedWords.add(word);
            }
        }

        // 각 단어마다 isAnswer 플래그 설정
        List<Stage1_2Problem.OptionDto> options = new ArrayList<>();
        for (Words word : selectedWords) {
            boolean isAnswer = checkWordContainsPhoneme(word.getWord(), targetConsonant);
            options.add(Stage1_2Problem.OptionDto.builder()
                    .wordId(word.getId())
                    .word(word.getWord())
                    .voiceUrl(word.getVoiceUrl())
                    .isAnswer(isAnswer)
                    .build()
            );
        }

        Collections.shuffle(options);

        return Stage1_2Problem.builder()
                .problemWord(targetPhoneme.getValue())
                .phonemeId(targetPhoneme.getId())
                .targetPhoneme(targetPhoneme.getValue())
                .imageUrl(targetPhoneme.getImageUrl())
                .voiceUrl(targetPhoneme.getVoiceUrl())
                .options(options)
                .build();
    }

    /**
     * 단어가 특정 자음을 포함하는지 확인
     */
    private boolean checkWordContainsPhoneme(String word, char targetConsonant) {
        for (int i = 0; i < word.length(); i++) {
            int codePoint = word.codePointAt(i);

            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(codePoint);

            if (phonemes.contains(targetConsonant)) {
                return true;
            }
        }
        return false;
    }
}