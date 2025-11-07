package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.PhonemesKcMap;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface PhonemesKcMapRepository extends JpaRepository<PhonemesKcMap, PhonemesKcMap.phonemesKcMapId> {
    List<PhonemesKcMap> findByKnowledgeComponent_Id(Long kcId);
}
