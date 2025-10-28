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
    // 만약에 없으면 null
    private String wordImageURL;
    // 문제에 대한 예시 음성 경로 S3
    private String wordVoiceURL;
    // 답 이 없는 경우는 null
    private Integer answer;
}
