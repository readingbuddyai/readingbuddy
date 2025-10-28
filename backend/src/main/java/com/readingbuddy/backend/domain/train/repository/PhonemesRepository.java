package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.Phonemes;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;

public interface PhonemesRepository extends JpaRepository<Phonemes, Long> {

    /**
     * 카테고리별 조회 (모음, 자음)
     */
    List<Phonemes> findByCategory(String category);

    /**
     * 랜덤하게 하나의 모음 조회 (질문-정답용)
     * nativeQuery = true : Native SQL로 PostgreSQL 고유 함수 직접 활용
     */
    @Query(value = "SELECT * FROM phonemes WHERE category = 'vowel' ORDER BY RANDOM() LIMIT 1", nativeQuery = true)
    Phonemes findOneRandomVowelForQuestion();

    /**
     * 랜덤하게 N개의 모음 조회 (선택지용) - 1개
     */
    @Query(value = "SELECT * FROM phonemes WHERE category = 'vowel' AND id != :excludeId ORDER BY RANDOM() LIMIT 1", nativeQuery = true)
    Phonemes findRandomVowel(Long excludeId);
}
