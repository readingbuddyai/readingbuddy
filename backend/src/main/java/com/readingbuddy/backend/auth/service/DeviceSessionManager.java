package com.readingbuddy.backend.auth.service;

import com.readingbuddy.backend.auth.dto.DeviceSessionInfo;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.security.SecureRandom;
import java.time.LocalDateTime;
import java.util.List;
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

        if(!session.isAuthorized()){
            throw new RuntimeException("인증 처리되지 않았습니다.");
        }

        if(session.getExpiredAt().isBefore(LocalDateTime.now())) {
            deviceSessionMap.remove(deviceAuthCode);
            throw new RuntimeException("요청 시간이 만료되었습니다.");
        }

        deviceSessionMap.remove(deviceAuthCode);
        return session.getUserId();

    }

    public DeviceSessionInfo getSession(String deviceAuthCode) {
        DeviceSessionInfo session= deviceSessionMap.get(deviceAuthCode);

        checkSession(session);

        if(session.getExpiredAt().isBefore(LocalDateTime.now())) {
            throw new RuntimeException("시간이 만료되었습니다.");
        }

        return session;
    }

    public void clearExpiredSessions() {
        deviceSessionMap.entrySet().removeIf(
                entry -> entry.getValue().getExpiredAt().isBefore(LocalDateTime.now())
        );
    }

    private void checkSession(DeviceSessionInfo session) {
        if(session==null){
            throw new RuntimeException("해당 코드는 유효하지 않습니다.");
        }
    }
}
