package com.readingbuddy.backend.domain.dashboard.repository;

import com.readingbuddy.backend.domain.user.entity.AttendHistories;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

@Repository
public interface AttendanceHistoriesRepository extends JpaRepository<AttendHistories, Long> {

    /**
     * 특정 사용자의 기간별 출석 기록 조회
     */
    @Query("SELECT ah FROM AttendHistories ah WHERE ah.user.id = :userId " +
            "AND ah.attendDate BETWEEN :startDate AND :endDate " +
            "ORDER BY ah.attendDate ASC")
    List<AttendHistories> findByUserIdAndDateRange(
            @Param("userId") Long userId,
            @Param("startDate") LocalDate startDate,
            @Param("endDate") LocalDate endDate);


    /**
     * 특정 사용자의 특정 날짜 출석 기록 조회
     */
    @Query("SELECT ah FROM AttendHistories ah WHERE ah.user.id = :userId " +
            "AND ah.attendDate = :date")
    Optional<AttendHistories> findByUserIdAndDate(
            @Param("userId") Long userId,
            @Param("date") LocalDate date);

}
