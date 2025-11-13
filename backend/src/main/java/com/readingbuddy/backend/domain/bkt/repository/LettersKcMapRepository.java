package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.LettersKcMap;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface LettersKcMapRepository extends JpaRepository<LettersKcMap, LettersKcMap.LettersKcMapId> {

    @Query("SELECT lkm FROM LettersKcMap lkm WHERE lkm.knowledgeComponent.id = :kcId")
    List<LettersKcMap> findByKnowledgeComponentId(@Param("kcId") Long kcId);

    @Query("SELECT lkm FROM LettersKcMap lkm WHERE lkm.letters.id = :letterId")
    List<LettersKcMap> findByLettersId(@Param("letterId") String letterId);
}
