package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.bkt.enums.KcCategory;
import com.readingbuddy.backend.domain.dashboard.dto.response.PhonemesWrongRankResponse;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

public interface TrainedProblemHistoriesRepository extends JpaRepository<TrainedProblemHistories, Long> {

    /**
     * 세션 완료 시 통계 집계용
     */
    List<TrainedProblemHistories> findByTrainedStageHistories(TrainedStageHistories session);

    /**
     * 특정 user의 특정 stage에 대한 최신 문제 이력 조회 (candidateList 확인용)
     * Spring Data JPA 메서드 네이밍 규칙 사용 (First = 최신 1개)
     */
    Optional<TrainedProblemHistories> findFirstByTrainedStageHistories_User_IdAndTrainedStageHistories_StageOrderBySolvedAtDesc(
            Long userId, String stage);

    @Query(value = """
            SELECT tph.* FROM trained_problem_histories tph
            JOIN train_problem_histories_kc_map kc_map ON tph.id = kc_map.trained_problem_histories_id
            JOIN trained_stage_histories tsh ON tph.trained_stage_id = tsh.id
            WHERE tsh.user_id = :userId
            AND kc_map.knowledge_component_id = :kcId
            ORDER BY tph.solved_at DESC
            LIMIT 1
            """,
            nativeQuery = true)
    Optional<TrainedProblemHistories> findFirstKCProbleHistories(
            @Param("userId") Long userId, @Param("kcId") Long kcId);

    /**
     * 사용자별 틀린 음소 조회 (내림차순)
     */
    @Query(value = """
            SELECT p.id as phonemeId, p.value, p.category, COUNT(tph.id) as wrongCnt
            FROM trained_problem_histories tph
            JOIN trained_stage_histories tsh ON tph.trained_stage_id = tsh.id
            JOIN phonemes p ON tph.phoneme_id = p.id
            WHERE tsh.user_id = :userId
            AND tph.is_correct = false
            GROUP BY p.id, p.value, p.category
            ORDER BY wrongCnt DESC
            LIMIT :limit""",
            nativeQuery = true)
    List<Object[]> getWrongPhonemesRanking(@Param("userId") Long userId, @Param("limit") int limit);

    /**
     * 사용자별 시도 횟수가 많은 음소 조회 (내림차순)
     */
    @Query(value = """
          SELECT
              p.id as phonemeId,
              p.value,
              p.category,
              SUM(max_attempts.max_attempt_number) as tryCnt
          FROM (
              SELECT
                  tph.phoneme_id,
                  tph.trained_stage_id,
                  tph.problem_number,
                  MAX(tph.attempt_number) as max_attempt_number
              FROM trained_problem_histories tph
              JOIN trained_stage_histories tsh ON tph.trained_stage_id = tsh.id
              WHERE tsh.user_id = :userId
              GROUP BY tph.phoneme_id, tph.trained_stage_id, tph.problem_number
          ) max_attempts
          JOIN phonemes p ON max_attempts.phoneme_id = p.id
          GROUP BY p.id, p.value, p.category
          ORDER BY tryCnt DESC
          LIMIT :limit
          """,
            nativeQuery = true)
    List<Object[]> getTryPhonemesRanking(@Param("userId") Long userId, @Param("limit") int limit);


}
