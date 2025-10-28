package com.readingbuddy.backend.domain.train.repository;

import com.readingbuddy.backend.domain.train.entity.Words;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface WordsRepository extends JpaRepository<Words,Long> {

}
