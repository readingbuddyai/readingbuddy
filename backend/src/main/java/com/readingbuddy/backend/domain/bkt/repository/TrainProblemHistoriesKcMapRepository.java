package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.TrainProblemHistoriesKcMap;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface TrainProblemHistoriesKcMapRepository extends JpaRepository<TrainProblemHistoriesKcMap, TrainProblemHistoriesKcMap.TrainProblemHistoriesKcMapId> {
}
