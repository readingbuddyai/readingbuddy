package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface UserKcMasteryRepository extends JpaRepository<UserKcMastery, Long> {
    Optional<UserKcMastery> findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(Long userId, Long knowledgeComponentId);

    List<UserKcMastery> findByUser_IdAndKnowledgeComponent_IdIn(Long userId, List<Long> kcIds);

    UserKcMastery findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(Long userId, Long knowledgeComponentId);
}