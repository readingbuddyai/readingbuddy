package com.readingbuddy.backend.train.repository;

import com.readingbuddy.backend.train.entity.TrainHistory;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface TrainHistoryRepository extends JpaRepository<TrainHistory, Long> {

}
