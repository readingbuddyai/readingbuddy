package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.Getter;

@Entity
@Getter
@Table(name = "phonemes",
       uniqueConstraints = @UniqueConstraint(columnNames = {"category", "value"}))
public class Phonemes {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String category;  // 자음 / 모음

    private String value;  // 자음 / 모음 값

    private String unicode;

    @Column(name = "image_url")
    private String imageUrl;

    @Column(name = "voice_url")
    private String voiceUrl;


}
