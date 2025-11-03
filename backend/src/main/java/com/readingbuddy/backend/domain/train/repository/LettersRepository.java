package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface LettersRepository extends JpaRepository<Letters,Long> {

    Optional<Letters> findByUnicode(String unicode);

    Optional<Letters> findByUnicodePoint(Integer unicodePoint);

    @Query(value = "SELECT unicode_point FROM letters ORDER BY RANDOM() LIMIT :cnt", nativeQuery = true)
    List<Integer> findRandomLetters(Integer cnt);
}
