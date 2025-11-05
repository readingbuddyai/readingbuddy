package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import lombok.Getter;

@Entity
@Getter
public class Letters {

    @Id
    private String id;

    @Column(nullable = false, unique = true)
    private String unicode;

    @Column(nullable = false, unique = true)
    private Integer unicodePoint;

    @Column(nullable = false)
    private Integer count;

    @Column(nullable = false)
    private String voiceUrl;

    @Column(nullable = false)
    private String slowVoiceUrl;
}
