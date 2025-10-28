package com.readingbuddy.backend.domain.train.dto.request;

import lombok.*;
import org.springframework.web.multipart.MultipartFile;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class VoiceCheckRequest {

    private MultipartFile audio;
    private String problemId;
    private Long userId;
}
