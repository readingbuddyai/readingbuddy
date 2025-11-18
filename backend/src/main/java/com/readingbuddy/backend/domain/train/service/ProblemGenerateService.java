package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import com.readingbuddy.backend.domain.bkt.entity.LettersKcMap;
import com.readingbuddy.backend.domain.bkt.repository.LettersKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.result.*;
import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.entity.Words;
import com.readingbuddy.backend.domain.train.repository.LettersRepository;
import com.readingbuddy.backend.domain.train.repository.WordsRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.math.BigInteger;
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

    // Letters와 원본 인덱스를 함께 관리하는 내부 클래스
    private static class LetterWithIndex {
        final Letters letter;
        final int index;

        LetterWithIndex(Letters letter, int index) {
            this.letter = letter;
            this.index = index;
        }
    }

    public List<ProblemResult> extractLetters(String stage, Integer cnt, Long userId) {
        List<ProblemResult> results = null;

        if (stage.equals("3")) {
            results = generateStage3(userId, cnt);
        } else if (stage.equals("4.1") || stage.equals("4.2")) {
            results = generateStage4(userId, stage, cnt);
        }
        return results;
    }

    public List<ProblemResult> generateStage3(Long userId, Integer cnt) {
        List<ProblemResult> results = new ArrayList<>();

        // 정답률이 낮은 순으로 정렬된 KC 목록 가져오기
        List<KcWithCorrectRate> kcList = bktService.getLowestCorrectRateKcsByStage(userId, "3");

        // KC별 문제 개수: 첫 번째 KC는 3개, 두 번째 KC는 2개
        int[] problemCounts = {cnt / 2 + 1, cnt / 2};

        for (int idx = 0; idx < Math.min(kcList.size(), problemCounts.length); idx++) {
            KcWithCorrectRate kcWithRate = kcList.get(idx);
            Long kcId = kcWithRate.getKnowledgeComponent().getId();

            // 해당 KC에 매핑된 Letters 조회
            List<Letters> letters = lettersKcMapRepository.findByKnowledgeComponentId(kcId).stream()
                    .map(LettersKcMap::getLetters)
                    .toList();

            // 현재 candidateList 가져오기
            String candidateList = bktService.getCandidateBitMask(userId, kcId);

            // 1. 사용 가능한 Letters 필터링 (비트마스크 기반)
            List<LetterWithIndex> availableLetters = filterAvailableLetters(letters, candidateList);
            boolean wasReset = availableLetters.size() == letters.size() && !candidateList.equals("0");

            // 2. 랜덤으로 N개 선택 (부족하면 전체 letters에서 추가 선택)
            int problemCount = problemCounts[idx];
            List<LetterWithIndex> selectedLetters = selectRandomLetters(availableLetters, letters, problemCount);

            // 3. candidateList 업데이트 (부족해서 추가 선택한 경우 리셋)
            boolean needsReset = wasReset || selectedLetters.size() > availableLetters.size();
            String updatedCandidateList = updateCandidateList(selectedLetters, candidateList, needsReset);

            // 4. Stage3Problem 생성 및 추가
            results.addAll(createStage3Problems(selectedLetters, kcId, updatedCandidateList));
        }

        return results;
    }

    /**
     * candidateList를 기반으로 사용 가능한 Letters 필터링
     * @param letters 전체 Letters 리스트
     * @param candidateList 비트마스크 (BigInteger String)
     * @return 사용 가능한 LetterWithIndex 리스트
     */
    private List<LetterWithIndex> filterAvailableLetters(List<Letters> letters, String candidateList) {
        BigInteger bitmask = new BigInteger(candidateList);
        List<LetterWithIndex> available = new ArrayList<>();
        for (int i = 0; i < letters.size(); i++) {
            // i번째 비트가 0이면 아직 출제되지 않은 문제
            if (!bitmask.testBit(i)) {
                available.add(new LetterWithIndex(letters.get(i), i));
            }
        }

        // 사용 가능한 Letters가 없으면 전체 Letters 반환 (라운드 리셋)
        if (available.isEmpty()) {
            for (int i = 0; i < letters.size(); i++) {
                available.add(new LetterWithIndex(letters.get(i), i));
            }
        }

        return available;
    }

    /**
     * 랜덤으로 N개의 Letters 선택 (중복 없이)
     * available에서 먼저 선택하고, 부족하면 전체 letters에서 추가 선택
     * @param available 사용 가능한 LetterWithIndex 리스트
     * @param allLetters 전체 Letters 리스트 (부족한 경우 사용)
     * @param count 선택할 개수
     * @return 선택된 LetterWithIndex 리스트
     */
    private List<LetterWithIndex> selectRandomLetters(List<LetterWithIndex> available, List<Letters> allLetters, int count) {
        List<LetterWithIndex> selected = new ArrayList<>();

        // 1단계: available에서 선택
        List<Integer> availableIndices = new ArrayList<>();
        for (int i = 0; i < available.size(); i++) {
            availableIndices.add(i);
        }

        int selectCount = Math.min(count, available.size());
        for (int i = 0; i < selectCount; i++) {
            int randomIndex = random.nextInt(availableIndices.size());
            int selectedIdx = availableIndices.remove(randomIndex);
            selected.add(available.get(selectedIdx));
        }

        // 2단계: 부족하면 전체 letters에서 추가 선택
        if (selected.size() < count) {
            // 이미 선택된 Letters를 제외한 나머지 Letters
            List<LetterWithIndex> remaining = new ArrayList<>();
            for (int i = 0; i < allLetters.size(); i++) {
                Letters letter = allLetters.get(i);
                boolean alreadySelected = selected.stream()
                        .anyMatch(lwi -> lwi.letter.equals(letter));
                if (!alreadySelected) {
                    remaining.add(new LetterWithIndex(letter, i));
                }
            }

            // 부족한 개수만큼 추가 선택
            int needCount = count - selected.size();
            List<Integer> remainingIndices = new ArrayList<>();
            for (int i = 0; i < remaining.size(); i++) {
                remainingIndices.add(i);
            }

            int additionalCount = Math.min(needCount, remaining.size());
            for (int i = 0; i < additionalCount; i++) {
                int randomIndex = random.nextInt(remainingIndices.size());
                int selectedIdx = remainingIndices.remove(randomIndex);
                selected.add(remaining.get(selectedIdx));
            }
        }

        return selected;
    }

    /**
     * 선택된 Letters의 인덱스를 candidateList에 반영
     * @param selectedLetters 선택된 LetterWithIndex 리스트
     * @param candidateList 기존 candidateList (BigInteger String)
     * @param wasReset candidateList가 리셋되었는지 여부
     * @return 업데이트된 candidateList (BigInteger String)
     */
    private String updateCandidateList(List<LetterWithIndex> selectedLetters, String candidateList, boolean wasReset) {
        // 리셋되었으면 0부터 시작
        BigInteger updated = wasReset ? BigInteger.ZERO : new BigInteger(candidateList);

        for (LetterWithIndex letterWithIndex : selectedLetters) {
            updated = updated.setBit(letterWithIndex.index);
        }

        return updated.toString();
    }

    /**
     * LetterWithIndex 리스트로 Stage3Problem 리스트 생성
     * @param selectedLetters 선택된 LetterWithIndex 리스트
     * @param kcId Knowledge Component ID
     * @param candidateList 업데이트된 candidateList (BigInteger String)
     * @return Stage3Problem 리스트
     */
    private List<ProblemResult> createStage3Problems(List<LetterWithIndex> selectedLetters, Long kcId, String candidateList) {
        List<ProblemResult> problems = new ArrayList<>();
        for (LetterWithIndex letterWithIndex : selectedLetters) {
            problems.add(new Stage3Problem(
                    letterWithIndex.letter.getId(),
                    letterWithIndex.letter.getVoiceUrl(),
                    letterWithIndex.letter.getCount(),
                    kcId,
                    candidateList
            ));
        }
        return problems;
    }

    /**
     * LetterWithIndex 리스트로 Stage4Problem 리스트 생성
     * @param selectedLetters 선택된 LetterWithIndex 리스트
     * @param kcId Knowledge Component ID
     * @param candidateList 업데이트된 candidateList (BigInteger String)
     * @return Stage4Problem 리스트
     */
    private List<ProblemResult> createStage4Problems(List<LetterWithIndex> selectedLetters, Long kcId, String candidateList) {
        List<ProblemResult> problems = new ArrayList<>();
        for (LetterWithIndex letterWithIndex : selectedLetters) {
            Letters letter = letterWithIndex.letter;

            // unicodePoint를 실제 한글 문자로 변환
            String koreanChar = String.valueOf((char) letter.getUnicodePoint().intValue());

            // PhonemeCounter를 사용하여 음소 분해
            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(letter.getUnicodePoint());

            problems.add(new Stage4Problem(
                    koreanChar,
                    letter.getSlowVoiceUrl(),
                    letter.getVoiceUrl(),
                    letter.getCount(),
                    phonemes,
                    kcId,
                    candidateList
            ));
        }
        return problems;
    }

    public List<ProblemResult> generateStage4(Long userId, String stage, Integer cnt) {
        // 4.1 stage의 경우 특정 단어로만 제한
        if (stage.equals("4.1")) {
            return generateStage4_1(userId, cnt);
        }

        List<ProblemResult> results = new ArrayList<>();

        // 정답률이 낮은 순으로 정렬된 KC 목록 가져오기
        List<KcWithCorrectRate> kcList = bktService.getLowestCorrectRateKcsByStage(userId, stage);

        // Stage 4: KC당 1개씩, 최대 Cnt KC
        int maxKcCount = cnt;
        int problemPerKc = 1;

        for (int idx = 0; idx < Math.min(kcList.size(), maxKcCount); idx++) {
            KcWithCorrectRate kcWithRate = kcList.get(idx);
            Long kcId = kcWithRate.getKnowledgeComponent().getId();

            // 해당 KC에 매핑된 Letters 조회
            List<Letters> letters = lettersKcMapRepository.findByKnowledgeComponentId(kcId).stream()
                    .map(LettersKcMap::getLetters)
                    .toList();

            // 현재 candidateList 가져오기
            String candidateList = bktService.getCandidateBitMask(userId, kcId);

            // 1. 사용 가능한 Letters 필터링 (비트마스크 기반)
            List<LetterWithIndex> availableLetters = filterAvailableLetters(letters, candidateList);
            boolean wasReset = availableLetters.size() == letters.size() && !candidateList.equals("0");

            // 2. 랜덤으로 1개 선택 (부족하면 전체 letters에서 선택)
            List<LetterWithIndex> selectedLetters = selectRandomLetters(availableLetters, letters, problemPerKc);

            // 3. candidateList 업데이트 (부족해서 추가 선택한 경우 리셋)
            boolean needsReset = wasReset || selectedLetters.size() > availableLetters.size();
            String updatedCandidateList = updateCandidateList(selectedLetters, candidateList, needsReset);

            // 4. Stage4Problem 생성 및 추가
            results.addAll(createStage4Problems(selectedLetters, kcId, updatedCandidateList));
        }

        return results;
    }

    /**
     * Stage 4.1 전용 문제 생성 (특정 단어로만 제한)
     * @param userId 사용자 ID
     * @param cnt 문제 개수
     * @return Stage4Problem 리스트
     */
    private List<ProblemResult> generateStage4_1(Long userId, Integer cnt) {
        cnt = 1;
        // 4.1에서 사용할 특정 단어 목록 (갈, 간, 남, 널, 달, 곰, 밤, 번, 살, 선, 잘, 전)
        String[] allowedWords = {"달"};
        List<Integer> unicodePoints = new ArrayList<>();

        for (String word : allowedWords) {
            unicodePoints.add((int) word.charAt(0));
        }

        // 허용된 단어에 해당하는 Letters 조회
        List<Letters> allowedLetters = lettersRepository.findByUnicodePointIn(unicodePoints);

        if (allowedLetters.isEmpty()) {
            throw new IllegalStateException("4.1 stage에 사용할 Letters를 찾을 수 없습니다.");
        }

        // 중복 없이 랜덤 선택 (cnt개)
        List<Letters> shuffledLetters = new ArrayList<>(allowedLetters);
        java.util.Collections.shuffle(shuffledLetters, random);

        // cnt가 allowedLetters 크기보다 크면 모든 단어 사용
        int selectCount = Math.min(cnt, shuffledLetters.size());
        List<Letters> selectedLetters = shuffledLetters.subList(0, selectCount);

        List<ProblemResult> results = new ArrayList<>();
        for (Letters selectedLetter : selectedLetters) {
            // 해당 Letter에 매핑된 KC 중 하나를 랜덤으로 선택
            List<LettersKcMap> kcMaps = lettersKcMapRepository.findByLettersId(selectedLetter.getId());
            Long selectedKcId = null;
            String candidateList = "0";

            if (!kcMaps.isEmpty()) {
                LettersKcMap selectedKcMap = kcMaps.get(random.nextInt(kcMaps.size()));
                selectedKcId = selectedKcMap.getKnowledgeComponent().getId();
            }

            // unicodePoint를 실제 한글 문자로 변환
            String koreanChar = String.valueOf((char) selectedLetter.getUnicodePoint().intValue());

            // PhonemeCounter를 사용하여 음소 분해
            List<Character> phonemes = PhonemeCounter.getPhonemesForCodePoint(selectedLetter.getUnicodePoint());

            results.add(new Stage4Problem(
                    koreanChar,
                    selectedLetter.getSlowVoiceUrl(),
                    selectedLetter.getVoiceUrl(),
                    selectedLetter.getCount(),
                    phonemes,
                    selectedKcId, // 해당 음절에 매핑된 KC 중 하나를 랜덤으로 선택
                    candidateList
            ));
        }

        return results;
    }

    public List<ProblemResult> extractWords(Integer cnt) {
        List<ProblemResult> results = new ArrayList<>();

        List<Integer> wordsList = random.ints(0, 100)
                .filter(i -> i == 12)
                .distinct()  // 중복 제거
                .limit(1)
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
