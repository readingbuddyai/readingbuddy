package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface TrainedStageHistoriesRepository extends JpaRepository<TrainedStageHistories, Long> {

    /**
     * User의 모든 세션
     */
    List<TrainedStageHistories> findByUserId(Long userId);

    /**
     * 최근 세션
     */
    Optional<TrainedStageHistories> findFirstByUserIdAndStageOrderByIdDesc(Long userId, String stage);

    /**
     * User와 Stage로 모든 세션 조회
     */
    List<TrainedStageHistories> findByUserIdAndStage(Long userId, String stage);

    /**
     * 마지막 플레이한 스테이지 조회
     */
    Optional<TrainedStageHistories> findFirstByUserIdOrderByStartedAtDesc(Long userId);

    /**
     * 특정 스테이지의 problem_number별 평균 시도 횟수 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @return 평균 시도 횟수
     */
    @Query(value = """
        SELECT AVG(max_attempts.max_attempt_number) as avg_try_count
        FROM (
            SELECT
                tph.problem_number,
                tph.trained_stage_id,
                MAX(tph.attempt_number) as max_attempt_number
            FROM trained_problem_histories tph
            JOIN trained_stage_histories tsh ON tph.trained_stage_id = tsh.id
            WHERE tsh.user_id = :userId
            AND tsh.stage = :stage
            GROUP BY tph.problem_number, tph.trained_stage_id
        ) max_attempts
        """,
        nativeQuery = true)
    Double getAverageTryCountPerProblem(@Param("userId") Long userId, @Param("stage") String stage);

}
