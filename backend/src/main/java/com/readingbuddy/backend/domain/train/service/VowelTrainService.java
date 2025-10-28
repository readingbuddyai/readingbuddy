package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.repository.PhonemesRepository;
import com.readingbuddy.backend.domain.train.dto.response.BasicLevelResponse;
import com.readingbuddy.backend.domain.train.dto.response.PhonemesDto;
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

    /**
     * 모음 기초 단계 문제 생성
     * - 정답 모음 1개
     * - 오답 모음 1개
     * - 총 2개 선택지를 랜덤 순서로 섞어서 반환
     */
    public BasicLevelResponse getBasicProblem() {
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
        List<PhonemesDto> optionDtos = options.stream()
                .map(vowel -> new PhonemesDto(
                        vowel.getId(),
                        vowel.getValue(),
                        vowel.getUnicode()
                )).toList();

        return BasicLevelResponse.builder()
                .questionId(answerVowel.getId())
                .value(answerVowel.getValue())
                .unicode(answerVowel.getUnicode())
                .voiceUrl(answerVowel.getVoiceUrl())
                .imageUrl(answerVowel.getImageUrl())
                .options(optionDtos)
                .build();
    }
}
