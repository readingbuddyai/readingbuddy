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

        // 모든 단어 조회
        List<Words> allWords = wordsRepository.findAll();
        Collections.shuffle(allWords);

        List<Words> selectedWords = new ArrayList<>();
        boolean foundCorrect = false;

        // 전체를 돌면서 처음 4개를 바로 선택, 정답은 따로 찾기
        for (Words word : allWords) {
            if (foundCorrect && selectedWords.size() == 5) {
                break;
            }

            if (selectedWords.size() < 4) {
                selectedWords.add(word);
                // 처음에 정답이 있을 수 있으니 있으면 5번째는 아무거나 들어와도 됨
                if (checkWordContainsPhoneme(word.getWord(), targetVowel)) {
                    foundCorrect = true;
                }
            }

            else {
                if (foundCorrect) {
                    selectedWords.add(word);
                    break;
                }
                // 정답을 아직 못 찾았으면 계속 찾기, 찾으면 추가
                else if(checkWordContainsPhoneme(word.getWord(), targetVowel)) {
                    selectedWords.add(word);
                    break;
                }
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
