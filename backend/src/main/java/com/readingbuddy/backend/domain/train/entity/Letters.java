package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class Letters {

    @Id
    private String id;

    @Column(nullable = false, unique = true)
    private String unicode;

    @Column(nullable = false, unique = true)
    private Integer unicodePoint;

    @Column(name = "\"count\"", nullable = false)
    private Integer count;

    @Column(nullable = false)
    private String voiceUrl;

    @Column(nullable = false)
    private String slowVoiceUrl;
}
