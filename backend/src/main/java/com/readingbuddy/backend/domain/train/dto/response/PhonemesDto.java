package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class PhonemesDto {

    private Long id;
    private String value;
    private String unicode;
}
