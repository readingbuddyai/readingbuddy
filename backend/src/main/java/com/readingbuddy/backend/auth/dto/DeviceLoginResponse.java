package com.readingbuddy.backend.auth.dto;

import lombok.Builder;
import lombok.Getter;

@Getter
@Builder
public class DeviceLoginResponse {
    private String authCode;
}
