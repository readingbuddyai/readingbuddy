package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import org.springframework.data.jpa.repository.JpaRepository;
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

}
