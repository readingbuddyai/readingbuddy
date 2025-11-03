package com.readingbuddy.backend.auth.service;

import com.readingbuddy.backend.auth.dto.DeviceSessionInfo;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.time.LocalDateTime;

import static org.junit.jupiter.api.Assertions.*;

@DisplayName("DeviceSessionManager 테스트")
class DeviceSessionManagerTest {

    private DeviceSessionManager deviceSessionManager;

    @BeforeEach
    void setUp() {
        deviceSessionManager = new DeviceSessionManager();
    }

    @Test
    @DisplayName("디바이스 코드 생성 성공")
    void generateDeviceCodeSession_Success() {
        // when
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();

        // then
        assertNotNull(deviceCode);
        assertEquals(10, deviceCode.length());

        // 생성된 코드가 유효한 문자만 포함하는지 확인
        assertTrue(deviceCode.matches("[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{10}"));
    }

    @Test
    @DisplayName("생성된 디바이스 코드는 세션 맵에 저장됨")
    void generateDeviceCodeSession_SavedInMap() {
        // when
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);

        // then
        assertNotNull(session);
        assertEquals(deviceCode, session.getDeviceCode());
        assertFalse(session.isAuthorized());
        assertNull(session.getUserId());
        assertNotNull(session.getExpiredAt());
    }

    @Test
    @DisplayName("디바이스 인증 성공")
    void authorizeDevice_Success() {
        // given
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);
        Long userId = 1L;

        // when
        deviceSessionManager.authorizeDevice(session, userId);

        // then
        assertTrue(session.isAuthorized());
        assertEquals(userId, session.getUserId());
    }

    @Test
    @DisplayName("인증된 디바이스 확인 성공")
    void checkAuthorizedDevice_Success() {
        // given
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);
        Long userId = 1L;
        deviceSessionManager.authorizeDevice(session, userId);

        // when
        Long resultUserId = deviceSessionManager.checkAuthorizedDevice(deviceCode);

        // then
        assertEquals(userId, resultUserId);
    }

    @Test
    @DisplayName("인증되지 않은 디바이스 확인 시 예외 발생")
    void checkAuthorizedDevice_NotAuthorized_ThrowsException() {
        // given
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();

        // when & then
        RuntimeException exception = assertThrows(RuntimeException.class, () -> {
            deviceSessionManager.checkAuthorizedDevice(deviceCode);
        });

        assertEquals("인증 처리되지 않았습니다.", exception.getMessage());
    }

    @Test
    @DisplayName("유효하지 않은 디바이스 코드로 세션 조회 시 예외 발생")
    void getSession_InvalidCode_ThrowsException() {
        // given
        String invalidCode = "INVALIDCODE";

        // when & then
        RuntimeException exception = assertThrows(RuntimeException.class, () -> {
            deviceSessionManager.getSession(invalidCode);
        });

        assertEquals("해당 코드는 유효하지 않습니다.", exception.getMessage());
    }

    @Test
    @DisplayName("유효하지 않은 디바이스 코드로 인증 확인 시 예외 발생")
    void checkAuthorizedDevice_InvalidCode_ThrowsException() {
        // given
        String invalidCode = "INVALIDCODE";

        // when & then
        RuntimeException exception = assertThrows(RuntimeException.class, () -> {
            deviceSessionManager.checkAuthorizedDevice(invalidCode);
        });

        assertEquals("해당 코드는 유효하지 않습니다.", exception.getMessage());
    }

    @Test
    @DisplayName("만료된 세션 삭제 성공")
    void clearExpiredSessions_Success() throws InterruptedException {
        // given
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();

        // 세션이 존재하는지 확인
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);
        assertNotNull(session);

        // 만료 시간을 과거로 설정
        session.setExpiredAt(LocalDateTime.now().minusMinutes(1));

        // when
        deviceSessionManager.clearExpiredSessions();

        // then
        // 만료된 세션이 삭제되어 조회 시 예외 발생
        RuntimeException exception = assertThrows(RuntimeException.class, () -> {
            deviceSessionManager.getSession(deviceCode);
        });

        assertEquals("해당 코드는 유효하지 않습니다.", exception.getMessage());
    }

    @Test
    @DisplayName("여러 디바이스 코드 생성 시 중복되지 않음")
    void generateDeviceCodeSession_Unique() {
        // when
        String code1 = deviceSessionManager.generateDeviceCodeSession();
        String code2 = deviceSessionManager.generateDeviceCodeSession();
        String code3 = deviceSessionManager.generateDeviceCodeSession();

        // then
        assertNotEquals(code1, code2);
        assertNotEquals(code2, code3);
        assertNotEquals(code1, code3);
    }

    @Test
    @DisplayName("인증 완료 후 세션은 맵에서 제거됨")
    void checkAuthorizedDevice_RemovesSessionAfterCheck() {
        // given
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);
        Long userId = 1L;
        deviceSessionManager.authorizeDevice(session, userId);

        // when
        deviceSessionManager.checkAuthorizedDevice(deviceCode);

        // then
        // 세션이 제거되어 다시 조회하면 예외 발생
        RuntimeException exception = assertThrows(RuntimeException.class, () -> {
            deviceSessionManager.getSession(deviceCode);
        });

        assertEquals("해당 코드는 유효하지 않습니다.", exception.getMessage());
    }

    @Test
    @DisplayName("세션 만료 시간 확인")
    void generateDeviceCodeSession_ExpirationTime() {
        // given
        LocalDateTime beforeGeneration = LocalDateTime.now();

        // when
        String deviceCode = deviceSessionManager.generateDeviceCodeSession();
        DeviceSessionInfo session = deviceSessionManager.getSession(deviceCode);

        // then
        LocalDateTime afterGeneration = LocalDateTime.now().plusMinutes(1);

        assertNotNull(session.getExpiredAt());
        assertTrue(session.getExpiredAt().isAfter(beforeGeneration));
        assertTrue(session.getExpiredAt().isBefore(afterGeneration.plusSeconds(5))); // 약간의 여유 시간
    }
}
