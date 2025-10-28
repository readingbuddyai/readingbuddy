package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.Getter;

@Entity
@Getter
@Table(name = "words")
public class Words {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String word;

    @Column(name = "voice_url")
    private String voiceUrl;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "phoneme_id")
    private Phonemes phoneme;
}
