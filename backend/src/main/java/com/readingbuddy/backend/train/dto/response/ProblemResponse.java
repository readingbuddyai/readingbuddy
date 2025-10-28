package com.readingbuddy.backend.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class ProblemResponse {

    private String problemWord;
    // 문제에 대한 이미지 경로 S3
    private String wordImageURL;
    // 문제에 대한 예시 음성 경로 S3
    private String wordVoiceURL;
    private Integer answer;
}
