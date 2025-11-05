package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.Getter;

import java.util.HashSet;
import java.util.Set;

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
