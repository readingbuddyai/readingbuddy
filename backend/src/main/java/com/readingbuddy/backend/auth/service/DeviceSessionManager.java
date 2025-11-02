package com.readingbuddy.backend.auth.service;

import com.readingbuddy.backend.auth.dto.DeviceLoginResponse;
import com.readingbuddy.backend.auth.dto.DeviceSessionInfo;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.security.SecureRandom;
import java.time.LocalDateTime;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor
public class DeviceSessionManager {

    private final Map<String, DeviceSessionInfo> deviceSessionMap = new ConcurrentHashMap<>();
    private static final String CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 혼동되는 문자 제외
    private final SecureRandom random = new SecureRandom();
    private final int DEVICE_CODE_LENGTH = 10;

    public String generateDeviceCodeSession() {
        // 1. 10자리 코드 생성
        StringBuilder sb = new StringBuilder(DEVICE_CODE_LENGTH);
        for (int i = 0; i < DEVICE_CODE_LENGTH; i++) {
            sb.append(CHARS.charAt(random.nextInt(CHARS.length())));
        }

        String deviceAuthCode =sb.toString();

        DeviceSessionInfo deviceSessionInfo= DeviceSessionInfo.builder()
                .deviceCode(deviceAuthCode)
                .isAuthorized(false)
                .expiredAt(LocalDateTime.now().plusMinutes(1))
                .build();

        deviceSessionMap.put(deviceAuthCode, deviceSessionInfo);

        return deviceAuthCode;
    }

    public void authorizeDevice(DeviceSessionInfo session,Long userId) {
        session.setAuthorized(true);
        session.setUserId(userId);
    }

    public Long checkAuthorizedDevice(String deviceAuthCode) {
        DeviceSessionInfo session= deviceSessionMap.get(deviceAuthCode);

        checkSession(session);

        if(session.isAuthorized()){
            deviceSessionMap.remove(deviceAuthCode);
            return session.getUserId();
        }

        if(session.getExpiredAt().isBefore(LocalDateTime.now())) {
            deviceSessionMap.remove(deviceAuthCode);
            throw new RuntimeException("Device code session expired");
        }
        throw new RuntimeException("Device session is Unauthorized");

    }

    public DeviceSessionInfo getSession(String deviceAuthCode) {
        DeviceSessionInfo session= deviceSessionMap.get(deviceAuthCode);

        checkSession(session);

        if(session.getExpiredAt().isBefore(LocalDateTime.now())) {
            throw new RuntimeException("시간이 만료되었습니다.");
        }

        return session;
    }

    private void checkSession(DeviceSessionInfo session) {
        if(session==null){
            throw new RuntimeException("해당 코드는 유효하지 않습니다.");
        }
    }
}
