package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.PhonemesKcMap;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.PhonemesKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_1Problem;
import com.readingbuddy.backend.domain.train.dto.result.Stage1_2Problem;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.entity.Words;
import com.readingbuddy.backend.domain.train.repository.PhonemesRepository;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
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
public class ConsonantTrainService {

    private final PhonemesRepository phonemesRepository;
    private final WordsRepository wordsRepository;
    private final BktService bktService;
    private final KnowledgeComponentRepository knowledgeComponentRepository;
    private final Random random = new Random();

    /**
     * 자음 기초 단계 문제 생성 (Stage 1.2.1)
     */
    public List<ProblemResult> getBasicProblem(Long userId,int count) {
        final String STAGE = "1.2.1";
        List<ProblemResult> problemList = new ArrayList<>();

        // 1. 해당 단계에 해당하는 KC 모두 가져오기
        List<KnowledgeComponent> stageKcs = knowledgeComponentRepository.findByStage(STAGE);
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

        // 3. Count 만큼 문제 생성
        for(int i=0; i<count; i++) {
            List<KnowledgeComponent> levelFilteredKcs=null;

            // 3-1. 수준별로 고르게 지식단위 문제 편성. 수준에 해당하는 지식단위(들)을 뽑아옴
            if(count%3==0){
                levelFilteredKcs =filterByLevel("EASY",stageKcs,kcCorrectRateMap);
            }else if(count%3==1){
                levelFilteredKcs =filterByLevel("MEDIUM",stageKcs,kcCorrectRateMap);
            }else{
                levelFilteredKcs =filterByLevel("HARD",stageKcs,kcCorrectRateMap);
            }

            // 3-2. 지식단위(들)가 뽑힌것이 없다면, 랜덤으로 가져오기 위해 해당 단계의 지식단위 다 담기
            if(levelFilteredKcs == null || levelFilteredKcs.isEmpty() ){
                levelFilteredKcs = stageKcs;
            }

            // 3-3. 지식단위들에서 랜덤 선택
            KnowledgeComponent selectedKc = levelFilteredKcs.get(random.nextInt(levelFilteredKcs.size()));

            // 3-4. 비트마스킹을 이용해 선택된 지식단위에서 문제 가져오기
            Phonemes answerConsonant= bktService.selectPhonemeUsingBitMask(userId,selectedKc.getId());

            // 3-5. 정답 제외 음소에서 오답 가져오기
            Phonemes wrongConsonant = phonemesRepository.findRandomConsonant(answerConsonant.getId());

            // 3-6. 선택지 리스트 생성
            List<Phonemes> options = new ArrayList<>();
            options.add(answerConsonant);
            options.add(wrongConsonant);

            // 3-7. 선택지 섞기
            Collections.shuffle(options);

            // 3-8. DTO로 변환
            List<Stage1_1Problem.OptionDto> optionDtos = options.stream()
                    .map(consonant -> Stage1_1Problem.OptionDto.builder()
                            .id(consonant.getId())
                            .value(consonant.getValue())
                            .unicode(consonant.getUnicode())
                            .build()
                    ).toList();

            // 3-9. N번 문제에 담기
            problemList.add(Stage1_1Problem.builder()
                    .problemWord(answerConsonant.getValue())
                    .imageUrl(answerConsonant.getImageUrl())
                    .phonemeId(answerConsonant.getId())
                    .voiceUrl(answerConsonant.getVoiceUrl())
                    .options(optionDtos)
                    .build()
            );
        }
        return problemList;
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

    private List<KnowledgeComponent> filterByLevel(String level,List<KnowledgeComponent> stageKcs,Map<Long, Float> kcCorrectRateMap){

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