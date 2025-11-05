package com.readingbuddy.backend.domain.bkt.repository;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface KnowledgeComponentRepository extends JpaRepository<KnowledgeComponent, Long> {
}
