package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface TrainedProblemHistoriesRepository extends JpaRepository<TrainedProblemHistories, Long> {

    /**
     * 세션 완료 시 통계 집계용
     */
    List<TrainedProblemHistories> findByTrainedStageHistories(TrainedStageHistories session);

}
