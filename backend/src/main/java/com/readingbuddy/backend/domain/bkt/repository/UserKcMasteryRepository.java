package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.enums.KcCategory;
import com.readingbuddy.backend.domain.dashboard.dto.response.DailyKcMasteryAvg;
import com.readingbuddy.backend.domain.dashboard.dto.response.DailyKcMasteryByDateResponse;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
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

    /**
     * 특정 카테고리 리스트에 해당하는 mastery의 날짜별 평균 계산
     */
    @Query("""
          SELECT DailyKcMasteryAvg(
            CAST(ukm.createdAt AS LocalDate),
            ROUND(AVG(ukm.pLearn), 2)
          )
          FROM UserKcMastery ukm
          JOIN ukm.knowledgeComponent kc
          WHERE ukm.user.id = :userId
          AND kc.category IN :categories
          AND ukm.createdAt BETWEEN :startDate AND :endDate
          GROUP BY CAST(ukm.createdAt AS LocalDate)
          ORDER BY CAST(ukm.createdAt AS LocalDate)
          """)
    List<DailyKcMasteryAvg> getDailyAverageMasteryByCategories(
            @Param("userId") Long userId,
            @Param("categories") List<KcCategory> categories,
            @Param("startDate") LocalDateTime startDate,
            @Param("endDate") LocalDateTime endDate);
}