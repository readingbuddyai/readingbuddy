package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.Letters;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface LettersRepository extends JpaRepository<Letters,Long> {

    Optional<Letters> findByUnicode(String unicode);

    Optional<Letters> findByUnicodePoint(Integer unicodePoint);
}
