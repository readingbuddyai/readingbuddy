package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.result.PhonemeWithKcIdAndCandidate;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_1Problem;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_2Problem;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.entity.Words;
import com.readingbuddy.backend.domain.train.repository.PhonemesRepository;
import com.readingbuddy.backend.domain.train.repository.WordsRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.*;

@Slf4j
@Service
@Transactional
@RequiredArgsConstructor
public class VowelTrainService {

    private final PhonemesRepository phonemesRepository;
    private final WordsRepository wordsRepository;
    private final BktService bktService;
    private final KnowledgeComponentRepository knowledgeComponentRepository;
    private final Random random = new Random();


    /**
     * 모음 기초 단계 문제 생성 (Stage 1.1.1) - BKT 적용
     */
    public List<ProblemResult> getBasicProblem(Long userId, int count) {
        final String stage = "1.1.1";
        List<ProblemResult> problemList = new ArrayList<>();

        // 1. 유저 숙련도 기반 정답 Phonemes 뽑기
        List<PhonemeWithKcIdAndCandidate> phonemeWithKcs = getBasedUserMasteryPhonemes(userId, count, stage);

        // 2. 문제 생성
        for (PhonemeWithKcIdAndCandidate phonemeWithKc : phonemeWithKcs) {
            Phonemes answerPhoneme = phonemeWithKc.getPhonemes();

            // 2-1. 보기 생성
            Phonemes wrongVowel = phonemesRepository.findRandomVowel(answerPhoneme.getId());

            List<Phonemes> options = new ArrayList<>();
            options.add(answerPhoneme);
            options.add(wrongVowel);

            // 선택지 섞기
            Collections.shuffle(options);

            // DTO로 변환
            List<Stage1_1Problem.OptionDto> optionDtos = options.stream()
                    .map(vowel -> Stage1_1Problem.OptionDto.builder()
                            .id(vowel.getId())
                            .value(vowel.getValue())
                            .unicode(vowel.getUnicode())
                            .build()
                    ).toList();

            // N번 문제에 담기
            problemList.add(Stage1_1Problem.builder()
                    .problemWord(answerPhoneme.getValue())
                    .imageUrl(answerPhoneme.getImageUrl())
                    .phonemeId(answerPhoneme.getId())
                    .voiceUrl(answerPhoneme.getVoiceUrl())
                    .options(optionDtos)
                    .kcId(phonemeWithKc.getKcId())
                    .candidateList(phonemeWithKc.getCandidateList())
                    .build()
            );
        }
        return problemList;
    }

    /**
     * 모음 심화 단계 문제 생성 (Stage 1.1.2) - BKT 적용
     */
    public List<ProblemResult> getAdvancedProblem(Long userId, int count) {
        final String stage = "1.1.2";
        List<ProblemResult> problemList = new ArrayList<>();
        List<PhonemeWithKcIdAndCandidate> phonemeWithKcs = getBasedUserMasteryPhonemes(userId, count, stage);
        List<Words> allWords = wordsRepository.findAll();

        for (PhonemeWithKcIdAndCandidate phonemeWithKc : phonemeWithKcs) {
            Collections.shuffle(allWords);
            Phonemes targetPhoneme = phonemeWithKc.getPhonemes();

            List<Words> selectedWords = new ArrayList<>();
            boolean foundCorrect = false;
            char targetVowel = targetPhoneme.getValue().charAt(0);

            for (Words word : allWords) {
                if (selectedWords.size() == 3) {
                    break;
                }

                boolean isContain = checkWordContainsPhoneme(word.getWord(), targetVowel);

                // 1) 아직 정답 단어를 찾지 못한 경우: 포함하는 단어를 정답으로 추가
                if (!foundCorrect && isContain) {
                    selectedWords.add(word);
                    foundCorrect = true;
                    continue;
                }
                // 2) 정답 단어를 찾은 후: 포함하지 않는 단어만 추가
                if (foundCorrect && !isContain) {
                    selectedWords.add(word);
                    continue;
                }
                // 3) 정답을 못 찾았고 포함하지 않는 단어이며, 2개 미만일 때: 추가
                if (!isContain && selectedWords.size() < 2) {
                    selectedWords.add(word);
                }
                // 이 구간 오는 단어 : 정답 찾았을 때 포함하는 단어인 경우
            }

            // 각 단어마다 isAnswer 플래그 설정
            List<Stage1_2Problem.OptionDto> options = new ArrayList<>();
            for (Words word : selectedWords) {
                boolean isAnswer = checkWordContainsPhoneme(word.getWord(), targetVowel);
                options.add(Stage1_2Problem.OptionDto.builder()
                        .wordId(word.getId())
                        .word(word.getWord())
                        .voiceUrl(word.getVoiceUrl())
                        .isAnswer(isAnswer)
                        .build()
                );
            }

            Collections.shuffle(options);

            problemList.add(Stage1_2Problem.builder()
                    .problemWord(targetPhoneme.getValue())
                    .phonemeId(targetPhoneme.getId())
                    .targetPhoneme(targetPhoneme.getValue())
                    .imageUrl(targetPhoneme.getImageUrl())
                    .voiceUrl(targetPhoneme.getVoiceUrl())
                    .options(options)
                    .kcId(phonemeWithKc.getKcId())
                    .candidateList(phonemeWithKc.getCandidateList())
                    .build()
            );
        }
        return problemList;
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

    /**
     * 유저 숙련도 기반으로 Phonemes 선택
     */
    private List<PhonemeWithKcIdAndCandidate> getBasedUserMasteryPhonemes(Long userId, int count, String stage) {
        List<PhonemeWithKcIdAndCandidate> phonemeWithKcs = new ArrayList<>();

        // 1. 해당 단계에 해당하는 KC 모두 가져오기
        List<KnowledgeComponent> stageKcs = knowledgeComponentRepository.findByStage(stage);

        // 2. 각 KC에 대한 정답률 계산 (BKT 기반)
        Map<Long, Float> kcCorrectRateMap = new HashMap<>();
        for (KnowledgeComponent kc : stageKcs) {
            try {
                Float correctRate = bktService.getCorrectAnswerRate(userId, kc.getId());
                kcCorrectRateMap.put(kc.getId(), correctRate);
            } catch (Exception e) {
                kcCorrectRateMap.put(kc.getId(), 0.0f);
            }
        }

        // 3. Count 만큼 문제 생성 (중복 방지)
        Set<Long> selectedPhonemeIds = new HashSet<>();

        for (int i = 0; i < count; i++) {
            List<KnowledgeComponent> levelFilteredKcs = null;

            // 3-1. 수준별로 고르게 지식단위 문제 편성
            if (i % 3 == 0) {
                levelFilteredKcs = filterByLevel("EASY", stageKcs, kcCorrectRateMap);
            } else if (i % 3 == 1) {
                levelFilteredKcs = filterByLevel("MEDIUM", stageKcs, kcCorrectRateMap);
            } else {
                levelFilteredKcs = filterByLevel("HARD", stageKcs, kcCorrectRateMap);
            }

            // 3-2. 지식단위(들)가 뽑힌것이 없다면, 랜덤으로 가져오기 위해 해당 단계의 지식단위 다 담기
            if (levelFilteredKcs == null || levelFilteredKcs.isEmpty()) {
                levelFilteredKcs = stageKcs;
            }

            // 3-3. 지식단위들에서 랜덤 선택
            KnowledgeComponent selectedKc = levelFilteredKcs.get(random.nextInt(levelFilteredKcs.size()));

            // 3-4. 비트마스킹을 이용해 선택된 지식단위에서 문제 가져오기 (이미 선택된 Phoneme 제외)
            PhonemeWithKcIdAndCandidate answerVowel = bktService.selectPhonemeUsingBitMask(userId, selectedKc.getId(), selectedPhonemeIds);

            // 3-5. 선택된 Phoneme ID 추가
            selectedPhonemeIds.add(answerVowel.getPhonemes().getId());
            phonemeWithKcs.add(answerVowel);
        }
        return phonemeWithKcs;
    }

    /**
     * 정답률에 따라 KC 필터링
     */
    private List<KnowledgeComponent> filterByLevel(String level, List<KnowledgeComponent> stageKcs, Map<Long, Float> kcCorrectRateMap) {
        float minRate, maxRate;

        switch (level.toUpperCase()) {
            case "EASY":
                minRate = 0.0f;
                maxRate = 0.3f;
                break;
            case "MEDIUM":
                minRate = 0.3f;
                maxRate = 0.7f;
                break;
            case "HARD":
                minRate = 0.7f;
                maxRate = 1.0f;
                break;
            default:
                throw new IllegalArgumentException("잘못된 level 값입니다. (EASY, MEDIUM, HARD 중 하나여야 함)");
        }

        return stageKcs.stream()
                .filter(kc -> {
                    float rate = kcCorrectRateMap.getOrDefault(kc.getId(), 0.0f);
                    return rate >= minRate && rate < maxRate;
                })
                .toList();
    }
}
