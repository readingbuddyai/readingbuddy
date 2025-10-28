package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.TrainHistories;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface TrainHistoriesRepository extends JpaRepository<TrainHistories, Long> {

}
