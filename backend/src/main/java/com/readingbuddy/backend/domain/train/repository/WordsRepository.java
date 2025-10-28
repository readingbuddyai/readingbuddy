package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.entity.Words;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;

public interface WordsRepository extends JpaRepository<Words, Long> {

    /**
     * 랜덤으로 더 많은 단어 pool 조회 (20개)
     */
    @Query(value = "SELECT * FROM words ORDER BY RANDOM() LIMIT 20", nativeQuery = true)
    List<Words> findRandomWordsPool();
}
