package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "train_history")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class TrainHistory {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private Integer problemCnt;

    @Column(nullable = false)
    private Integer correctCnt;

    @Column(nullable = false)
    private Integer wrongCnt;

    @Column(nullable = false)
    private Integer turnedCnt;
}
