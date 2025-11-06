package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.LettersKcMap;
import com.readingbuddy.backend.domain.bkt.repository.LettersKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.result.KcWithCorrectRate;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage3Problem;
import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.repository.LettersRepository;
import com.readingbuddy.backend.domain.train.repository.WordsRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.ArrayList;
import java.util.List;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("ProblemGenerateService 테스트")
class ProblemGenerateServiceTest {

    @Mock
    private LettersRepository lettersRepository;

    @Mock
    private WordsRepository wordsRepository;

    @Mock
    private BktService bktService;

    @Mock
    private LettersKcMapRepository lettersKcMapRepository;

    @InjectMocks
    private ProblemGenerateService problemGenerateService;

    private Long testUserId;
    private KnowledgeComponent testKc1;
    private KnowledgeComponent testKc2;
    private List<Letters> testLetters;

    @BeforeEach
    void setUp() {
        testUserId = 1L;

        // 테스트용 KnowledgeComponent 생성
        testKc1 = KnowledgeComponent.builder()
                .id(1L)
                .stage("3")
                .build();

        testKc2 = KnowledgeComponent.builder()
                .id(2L)
                .stage("3")
                .build();

        // 테스트용 Letters 생성 (5개)
        testLetters = new ArrayList<>();
        for (int i = 0; i < 5; i++) {
            Letters letter = Letters.builder()
                    .id(String.valueOf(44032 + i))  // '가', '각', '갂', '갃', '간'
                    .unicodePoint(44032 + i)
                    .voiceUrl("https://test.com/voice" + i + ".mp3")
                    .count(i + 2)  // 음소 개수 2, 3, 4, 5, 6
                    .build();
            testLetters.add(letter);
        }
    }

    @Test
    @DisplayName("generateStage3 - 정상 케이스: KC 2개로 5개 문제 생성")
    void generateStage3_Success_With2KCs() {
        // given
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f),  // 정답률 낮음 -> 3개
                new KcWithCorrectRate(testKc2, 0.5f)   // 정답률 중간 -> 2개
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(anyLong())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(anyLong(), anyLong())).thenReturn(0);  // 아직 출제된 문제 없음

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(5, results.size(), "첫 번째 KC에서 3개 + 두 번째 KC에서 2개 = 총 5개");

        // 모든 문제가 Stage3Problem 타입인지 확인
        assertTrue(results.stream().allMatch(r -> r instanceof Stage3Problem));

        // Stage3Problem으로 캐스팅하여 상세 검증
        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // 첫 3개 문제는 testKc1의 ID를 가져야 함
        for (int i = 0; i < 3; i++) {
            assertEquals(testKc1.getId(), stage3Problems.get(i).getKcId());
            assertNotNull(stage3Problems.get(i).getProblemVoiceUrl());
            assertNotNull(stage3Problems.get(i).getAnswerCnt());
            assertNotNull(stage3Problems.get(i).getCandidateList());
        }

        // 나머지 2개 문제는 testKc2의 ID를 가져야 함
        for (int i = 3; i < 5; i++) {
            assertEquals(testKc2.getId(), stage3Problems.get(i).getKcId());
        }

        // BKT 서비스 호출 검증
        verify(bktService, times(1)).getLowestCorrectRateKcsByStage(testUserId, "3");
        verify(bktService, times(2)).getCandidateBitMask(anyLong(), anyLong());
        verify(lettersKcMapRepository, times(2)).findByKnowledgeComponentId(anyLong());
    }

    @Test
    @DisplayName("generateStage3 - candidateList에 일부 문제가 출제된 경우")
    void generateStage3_WithPartialCandidateList() {
        // given
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        // 0번과 1번 인덱스가 이미 출제됨 (비트마스크: 0b00011 = 3)
        int existingCandidateList = 0b00011;

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(existingCandidateList);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(3, results.size());

        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // candidateList가 업데이트되었는지 확인
        for (Stage3Problem problem : stage3Problems) {
            assertNotNull(problem.getCandidateList());
            assertTrue(problem.getCandidateList() > existingCandidateList,
                "candidateList가 업데이트되어야 함");
        }

        // 모든 문제의 candidateList가 동일해야 함 (같은 KC의 문제들)
        int firstCandidateList = stage3Problems.get(0).getCandidateList();
        assertTrue(stage3Problems.stream()
                .allMatch(p -> p.getCandidateList().equals(firstCandidateList)));
    }

    @Test
    @DisplayName("generateStage3 - 모든 문제가 출제된 경우 (candidateList 리셋)")
    void generateStage3_WithFullCandidateList_ShouldReset() {
        // given
        // Letters가 3개만 있는 경우
        List<Letters> threeLetters = testLetters.subList(0, 3);

        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : threeLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        // 3개 모두 출제됨 (비트마스크: 0b111 = 7)
        int fullCandidateList = 0b111;

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(fullCandidateList);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(3, results.size());

        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // candidateList가 리셋되어 새로 시작해야 함
        for (Stage3Problem problem : stage3Problems) {
            assertNotNull(problem.getCandidateList());
            // 리셋되어 새로운 비트마스크가 설정됨
            assertTrue(problem.getCandidateList() >= 0);
        }
    }

    @Test
    @DisplayName("generateStage3 - KC가 1개만 있는 경우")
    void generateStage3_WithSingleKC() {
        // given
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(0);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(3, results.size(), "KC가 1개만 있으면 첫 번째 KC의 3개 문제만 생성");

        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // 모든 문제가 testKc1의 ID를 가져야 함
        assertTrue(stage3Problems.stream()
                .allMatch(p -> p.getKcId().equals(testKc1.getId())));
    }

    @Test
    @DisplayName("generateStage3 - available이 부족한 경우 추가 선택")
    void generateStage3_WithInsufficientAvailable() {
        // given
        // Letters가 2개만 있는 경우 (3개 필요한데 2개만 available)
        List<Letters> twoLetters = testLetters.subList(0, 2);

        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : twoLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(0);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        // Letters가 2개만 있으므로 2개만 생성됨 (중복 선택하지 않음)
        assertEquals(2, results.size());
    }

    @Test
    @DisplayName("generateStage3 - KC 리스트가 비어있는 경우")
    void generateStage3_WithEmptyKCList() {
        // given
        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(new ArrayList<>());

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertTrue(results.isEmpty(), "KC가 없으면 문제도 생성되지 않음");
    }

    @Test
    @DisplayName("generateStage3 - Letters가 비어있는 경우")
    void generateStage3_WithEmptyLetters() {
        // given
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(new ArrayList<>());
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(0);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertTrue(results.isEmpty(), "Letters가 없으면 문제도 생성되지 않음");
    }

    @Test
    @DisplayName("generateStage3 - 모든 Stage3Problem이 올바른 필드를 가지는지 검증")
    void generateStage3_ValidateAllFields() {
        // given
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(0);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        for (Stage3Problem problem : stage3Problems) {
            // 모든 필수 필드가 null이 아니어야 함
            assertNotNull(problem.getProblemWord(), "problemWord는 null이 아니어야 함");
            assertNotNull(problem.getProblemVoiceUrl(), "problemVoiceUrl은 null이 아니어야 함");
            assertNotNull(problem.getAnswerCnt(), "answerCnt는 null이 아니어야 함");
            assertNotNull(problem.getKcId(), "kcId는 null이 아니어야 함");
            assertNotNull(problem.getCandidateList(), "candidateList는 null이 아니어야 함");

            // answerCnt는 양수여야 함
            assertTrue(problem.getAnswerCnt() > 0, "answerCnt는 양수여야 함");

            // candidateList는 0 이상이어야 함
            assertTrue(problem.getCandidateList() >= 0, "candidateList는 0 이상이어야 함");

            // problemVoiceUrl은 빈 문자열이 아니어야 함
            assertFalse(problem.getProblemVoiceUrl().isEmpty(), "problemVoiceUrl은 빈 문자열이 아니어야 함");
        }
    }

    @Test
    @DisplayName("generateStage3 - available 2개, count 3개 요구 시 추가 선택")
    void generateStage3_WithTwoAvailableAndThreeRequired() {
        // given
        // 전체 Letters는 5개
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        // candidateList: 인덱스 2, 3, 4가 이미 출제됨 (비트마스크: 0b11100 = 28)
        // available은 인덱스 0, 1만 (2개만 available)
        int candidateListWith3Used = 0b11100;  // 28

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(candidateListWith3Used);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(3, results.size(), "available 2개 + 추가 1개 = 총 3개 문제 생성");

        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // candidateList가 리셋되었는지 확인 (부족해서 추가 선택했으므로)
        for (Stage3Problem problem : stage3Problems) {
            assertNotNull(problem.getCandidateList());
            // 새로운 라운드 시작으로 candidateList가 새로 설정됨
            // 3개의 비트가 켜져있어야 함 (3개 문제 선택)
            int bitCount = Integer.bitCount(problem.getCandidateList());
            assertEquals(3, bitCount, "3개 문제를 선택했으므로 3개의 비트가 켜져있어야 함");
        }

        // 모든 문제가 같은 candidateList를 가져야 함
        int firstCandidateList = stage3Problems.get(0).getCandidateList();
        assertTrue(stage3Problems.stream()
                .allMatch(p -> p.getCandidateList().equals(firstCandidateList)),
                "같은 KC의 문제들은 같은 candidateList를 가져야 함");

        // 모든 필드가 올바르게 설정되었는지 확인
        for (Stage3Problem problem : stage3Problems) {
            assertNotNull(problem.getProblemWord());
            assertNotNull(problem.getProblemVoiceUrl());
            assertNotNull(problem.getAnswerCnt());
            assertNotNull(problem.getKcId());
            assertEquals(testKc1.getId(), problem.getKcId());
        }
    }

    @Test
    @DisplayName("generateStage3 - available 1개, count 3개 요구 시 추가 선택")
    void generateStage3_WithOneAvailableAndThreeRequired() {
        // given
        // 전체 Letters는 5개
        List<KcWithCorrectRate> kcList = List.of(
                new KcWithCorrectRate(testKc1, 0.3f)
        );

        List<LettersKcMap> lettersKcMaps = new ArrayList<>();
        for (Letters letter : testLetters) {
            LettersKcMap map = LettersKcMap.builder()
                    .knowledgeComponent(testKc1)
                    .letters(letter)
                    .build();
            lettersKcMaps.add(map);
        }

        // candidateList: 인덱스 1, 2, 3, 4가 이미 출제됨 (비트마스크: 0b11110 = 30)
        // available은 인덱스 0만 (1개만 available)
        int candidateListWith4Used = 0b11110;  // 30

        when(bktService.getLowestCorrectRateKcsByStage(testUserId, "3")).thenReturn(kcList);
        when(lettersKcMapRepository.findByKnowledgeComponentId(testKc1.getId())).thenReturn(lettersKcMaps);
        when(bktService.getCandidateBitMask(testUserId, testKc1.getId())).thenReturn(candidateListWith4Used);

        // when
        List<ProblemResult> results = problemGenerateService.generateStage3(testUserId);

        // then
        assertNotNull(results);
        assertEquals(3, results.size(), "available 1개 + 추가 2개 = 총 3개 문제 생성");

        List<Stage3Problem> stage3Problems = results.stream()
                .map(r -> (Stage3Problem) r)
                .toList();

        // candidateList가 리셋되고 3개의 비트가 켜져있어야 함
        for (Stage3Problem problem : stage3Problems) {
            assertNotNull(problem.getCandidateList());
            int bitCount = Integer.bitCount(problem.getCandidateList());
            assertEquals(3, bitCount, "3개 문제를 선택했으므로 3개의 비트가 켜져있어야 함");
        }

        // 모든 문제가 중복되지 않는지 확인 (서로 다른 Letters여야 함)
        long uniqueProblems = stage3Problems.stream()
                .map(Stage3Problem::getProblemWord)
                .distinct()
                .count();
        assertEquals(3, uniqueProblems, "3개 문제는 모두 서로 다른 Letters여야 함");
    }
}
