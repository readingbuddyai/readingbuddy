package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

@Repository
public interface UserKcMasteryRepository extends JpaRepository<UserKcMastery, Long> {
    Optional<UserKcMastery> findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(Long userId, Long knowledgeComponentId);

    List<UserKcMastery> findByUser_IdAndKnowledgeComponent_IdIn(Long userId, List<Long> kcIds);

    UserKcMastery findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(Long userId, Long knowledgeComponentId);

    // KC 숙련도 변화 추이 조회 (시간순 정렬)
    List<UserKcMastery> findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtAsc(Long userId, Long knowledgeComponentId);

    // KC 숙련도 변화 추이 조회 (기간 필터링, 시간순 정렬)
    List<UserKcMastery> findByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtAsc(
            Long userId, Long knowledgeComponentId, LocalDateTime startDateTime, LocalDateTime endDateTime);

    // 특정 기간 내 KC의 최신 숙련도 조회
    Optional<UserKcMastery> findFirstByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtDesc(
            Long userId, Long knowledgeComponentId, LocalDateTime startDateTime, LocalDateTime endDateTime);
}