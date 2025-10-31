package com.readingbuddy.backend.common.service;

import com.amazonaws.services.s3.AmazonS3;
import com.amazonaws.services.s3.model.ObjectMetadata;
import com.amazonaws.services.s3.model.PutObjectRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

@Slf4j
@Service
@RequiredArgsConstructor
public class S3Service {

    private final AmazonS3 s3Client;

    @Value("${cloud.aws.s3.bucket}")
    private String bucket;

    private static final DateTimeFormatter TIMESTAMP_FORMATTER = DateTimeFormatter.ofPattern("yyyyMMddHHmmss");

    /**
     * 음성 파일을 S3에 업로드하고 URL 반환
     * 파일명 형식: audio/{sessionId}/{userId}_{problemId}_{timestamp}.확장자
     */
    public String uploadAudioFile(MultipartFile file, String sessionId, Long userId, Integer problemId) {
        try {
            // 현재 시간을 타임스탬프로 변환
            String timestamp = LocalDateTime.now().format(TIMESTAMP_FORMATTER);

            // 파일명 생성: audio/{sessionId}/{userId}_{problemId}_{timestamp}.확장자
            String fileName = String.format("audio/%s/%d_%d_%s.%s",
                    sessionId,
                    userId,
                    problemId,
                    timestamp,
                    getFileExtension(file.getOriginalFilename()));

            // 메타데이터 설정
            ObjectMetadata metadata = new ObjectMetadata();
            metadata.setContentType(file.getContentType());
            metadata.setContentLength(file.getSize());

            // S3에 업로드
            s3Client.putObject(new PutObjectRequest(bucket, fileName, file.getInputStream(), metadata));

            // URL 반환
            String fileUrl = s3Client.getUrl(bucket, fileName).toString();
            log.info("S3 업로드 성공: userId={}, problemId={}, url={}", userId, problemId, fileUrl);

            return fileUrl;

        } catch (IOException e) {
            log.error("S3 업로드 실패: userId={}, problemId={}, error={}", userId, problemId, e.getMessage(), e);
            throw new RuntimeException("파일 업로드에 실패했습니다. ",e);
        }
    }

    /**
     * 파일 확장자 추출
     */
    private String getFileExtension(String fileName) {
        if (fileName == null || !fileName.contains(".")) {
            return "wav";  // 기본값
        }

        return fileName.substring(fileName.lastIndexOf(".") + 1);
    }
}
