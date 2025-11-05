package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface UserKcMasteryRepository extends JpaRepository<UserKcMastery, Long> {
    UserKcMastery findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(Long userId, Long knowledgeComponentId);
}
