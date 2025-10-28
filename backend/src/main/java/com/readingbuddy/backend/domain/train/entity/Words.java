package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;

@Entity
public class Words {

    @Id
    private Integer id;

    @Column(unique = true)
    private String word;

    @Column(nullable = false)
    private String voiceUrl;
}
