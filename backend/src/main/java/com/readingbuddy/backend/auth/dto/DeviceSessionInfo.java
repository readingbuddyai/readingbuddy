package com.readingbuddy.backend.auth.dto;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Getter
@Setter
@Builder
public class DeviceSessionInfo {
    private String deviceCode;

    private boolean isAuthorized;

    private LocalDateTime expiredAt;

    private Long userId;
}
