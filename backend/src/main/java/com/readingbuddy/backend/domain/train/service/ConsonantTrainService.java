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
                .map(consonant -> new Stage1_1Problem.OptionDto(
                        consonant.getId(),
                        consonant.getValue(),
                        consonant.getUnicode()
                )).toList();

        return new Stage1_1Problem(
                answerConsonant.getValue(),
                answerConsonant.getId(),
                answerConsonant.getVoiceUrl(),
                answerConsonant.getImageUrl(),
                optionDtos
        );
    }
}