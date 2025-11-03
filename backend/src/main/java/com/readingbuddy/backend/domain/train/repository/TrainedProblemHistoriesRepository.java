package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.dashboard.dto.response.PhonemesWrongRankResponse;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface TrainedProblemHistoriesRepository extends JpaRepository<TrainedProblemHistories, Long> {

    /**
     * 세션 완료 시 통계 집계용
     */
    List<TrainedProblemHistories> findByTrainedStageHistories(TrainedStageHistories session);


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
}
