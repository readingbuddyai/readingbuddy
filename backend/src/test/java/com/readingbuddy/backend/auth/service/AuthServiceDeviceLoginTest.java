package com.readingbuddy.backend.auth.service;

import com.readingbuddy.backend.auth.RefreshTokenRepository;
import com.readingbuddy.backend.auth.dto.*;
import com.readingbuddy.backend.auth.jwt.JWTUtil;
import com.readingbuddy.backend.common.properties.JwtProperties;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import jakarta.servlet.http.HttpServletRequest;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.test.util.ReflectionTestUtils;

import java.time.LocalDateTime;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("AuthService Device Login 테스트")
class AuthServiceDeviceLoginTest {

    @Mock
    private UserRepository userRepository;

    @Mock
    private PasswordEncoder passwordEncoder;

    @Mock
    private JWTUtil jwtUtil;

    @Mock
    private JwtProperties jwtProperties;

    @Mock
    private RefreshTokenRepository refreshTokenRepository;

    @Mock
    private DeviceSessionManager deviceSessionManager;

    @Mock
    private HttpServletRequest httpServletRequest;

    @InjectMocks
    private AuthService authService;

    private User testUser;
    private DeviceSessionInfo testDeviceSession;
    private String testDeviceCode;

    @BeforeEach
    void setUp() {
        testUser = User.builder()
                .id(1L)
                .email("test@example.com")
                .password("encodedPassword")
                .nickname("testuser")
                .build();

        testDeviceCode = "ABCD123456";

        testDeviceSession = DeviceSessionInfo.builder()
                .deviceCode(testDeviceCode)
                .isAuthorized(false)
                .expiredAt(LocalDateTime.now().plusMinutes(1))
                .build();
    }

    @Test
    @DisplayName("디바이스 로그인 응답 생성 성공")
    void createDeviceLoginResponse_Success() {
        // given
        when(deviceSessionManager.generateDeviceCodeSession()).thenReturn(testDeviceCode);

        // when
        DeviceLoginResponse response = authService.createDeviceLoginResponse();

        // then
        assertNotNull(response);
        assertEquals(testDeviceCode, response.getAuthCode());
        verify(deviceSessionManager, times(1)).generateDeviceCodeSession();
    }

    @Test
    @DisplayName("디바이스 코드 인증 성공")
    void authorizeDeviceCode_Success() {
        // given
        DeviceCodeRequest request = new DeviceCodeRequest();
        ReflectionTestUtils.setField(request, "deviceAuthCode", testDeviceCode);
        Long userId = 1L;

        when(userRepository.findById(userId)).thenReturn(Optional.of(testUser));
        when(deviceSessionManager.getSession(testDeviceCode)).thenReturn(testDeviceSession);

        // when
        authService.authorizeDeviceCode(request, userId);

        // then
        verify(userRepository, times(1)).findById(userId);
        verify(deviceSessionManager, times(1)).getSession(testDeviceCode);
        verify(deviceSessionManager, times(1)).authorizeDevice(testDeviceSession, testUser.getId());
    }

    @Test
    @DisplayName("디바이스 코드 인증 실패 - 존재하지 않는 사용자")
    void authorizeDeviceCode_UserNotFound_ThrowsException() {
        // given
        DeviceCodeRequest request = new DeviceCodeRequest();
        Long userId = 999L;

        when(userRepository.findById(userId)).thenReturn(Optional.empty());

        // when & then
        IllegalArgumentException exception = assertThrows(IllegalArgumentException.class, () -> {
            authService.authorizeDeviceCode(request, userId);
        });

        assertEquals("로그인 정보에 일치하는 회원이 없습니다.", exception.getMessage());
        verify(deviceSessionManager, never()).getSession(anyString());
        verify(deviceSessionManager, never()).authorizeDevice(any(), anyLong());
    }

    @Test
    @DisplayName("디바이스 인증 확인 및 토큰 발행 성공")
    void checkDeviceAuthorized_Success() {
        // given
        DeviceCodeRequest request = new DeviceCodeRequest();
        ReflectionTestUtils.setField(request, "deviceAuthCode", testDeviceCode);
        testDeviceSession.setAuthorized(true);
        testDeviceSession.setUserId(testUser.getId());

        when(deviceSessionManager.checkAuthorizedDevice(testDeviceCode)).thenReturn(testUser.getId());
        when(userRepository.findById(testUser.getId())).thenReturn(Optional.of(testUser));
        when(jwtUtil.createAccessToken(anyString(), anyString(), anyString(), anyLong()))
                .thenReturn("access-token");
        when(jwtUtil.createRefreshToken(anyString(), anyLong()))
                .thenReturn("refresh-token");
        when(jwtProperties.getAccessTokenValidityInMs()).thenReturn(3600000L);
        when(jwtProperties.getRefreshTokenValidityInMs()).thenReturn(604800000L);
        when(jwtUtil.getExpiredAt(anyLong())).thenReturn(LocalDateTime.now().plusDays(7));

        // Mock all headers used in getIp() method
        when(httpServletRequest.getHeader("CF-Connecting-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("X-Forwarded-For")).thenReturn(null);
        when(httpServletRequest.getHeader("Proxy-Client-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("WL-Proxy-Client-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("User-Agent")).thenReturn("test-agent");
        when(httpServletRequest.getRemoteAddr()).thenReturn("127.0.0.1");

        when(refreshTokenRepository.findByUserAndIssuedIpAndIssuedUserAgent(any(), anyString(), anyString()))
                .thenReturn(Optional.empty());

        // when
        TokenResponse tokenResponse = authService.checkDeviceAuthorized(request, httpServletRequest);

        // then
        assertNotNull(tokenResponse);
        assertEquals("access-token", tokenResponse.getAccessToken());
        assertEquals("refresh-token", tokenResponse.getRefreshToken());

        verify(deviceSessionManager, times(1)).checkAuthorizedDevice(testDeviceCode);
        verify(userRepository, times(1)).findById(testUser.getId());
        verify(jwtUtil, times(1)).createAccessToken(
                String.valueOf(testUser.getId()),
                testUser.getEmail(),
                testUser.getNickname(),
                jwtProperties.getAccessTokenValidityInMs()
        );
        verify(jwtUtil, times(1)).createRefreshToken(
                String.valueOf(testUser.getId()),
                jwtProperties.getRefreshTokenValidityInMs()
        );
    }

    @Test
    @DisplayName("디바이스 인증 확인 실패 - 존재하지 않는 사용자")
    void checkDeviceAuthorized_UserNotFound_ThrowsException() {
        // given
        DeviceCodeRequest request = new DeviceCodeRequest();
        ReflectionTestUtils.setField(request, "deviceAuthCode", testDeviceCode);

        when(deviceSessionManager.checkAuthorizedDevice(testDeviceCode)).thenReturn(999L);
        when(userRepository.findById(999L)).thenReturn(Optional.empty());

        // when & then
        IllegalArgumentException exception = assertThrows(IllegalArgumentException.class, () -> {
            authService.checkDeviceAuthorized(request, httpServletRequest);
        });

        assertEquals("로그인 정보에 일치하는 회원이 없습니다.", exception.getMessage());
        verify(deviceSessionManager, times(1)).checkAuthorizedDevice(testDeviceCode);
        verify(jwtUtil, never()).createAccessToken(anyString(), anyString(), anyString(), anyLong());
        verify(jwtUtil, never()).createRefreshToken(anyString(), anyLong());
    }

    @Test
    @DisplayName("디바이스 로그인 전체 플로우 테스트")
    void deviceLoginFlow_Success() {
        // Step 1: 디바이스 코드 생성
        when(deviceSessionManager.generateDeviceCodeSession()).thenReturn(testDeviceCode);
        DeviceLoginResponse loginResponse = authService.createDeviceLoginResponse();
        assertEquals(testDeviceCode, loginResponse.getAuthCode());

        // Step 2: 사용자가 앱에서 디바이스 코드 인증
        DeviceCodeRequest authRequest = new DeviceCodeRequest();
        ReflectionTestUtils.setField(authRequest, "deviceAuthCode", testDeviceCode);
        when(userRepository.findById(testUser.getId())).thenReturn(Optional.of(testUser));
        when(deviceSessionManager.getSession(testDeviceCode)).thenReturn(testDeviceSession);

        authService.authorizeDeviceCode(authRequest, testUser.getId());
        verify(deviceSessionManager, times(1)).authorizeDevice(testDeviceSession, testUser.getId());

        // Step 3: 디바이스에서 polling으로 인증 확인 및 토큰 발행
        testDeviceSession.setAuthorized(true);
        testDeviceSession.setUserId(testUser.getId());

        when(deviceSessionManager.checkAuthorizedDevice(testDeviceCode)).thenReturn(testUser.getId());
        when(jwtUtil.createAccessToken(anyString(), anyString(), anyString(), anyLong()))
                .thenReturn("access-token");
        when(jwtUtil.createRefreshToken(anyString(), anyLong()))
                .thenReturn("refresh-token");
        when(jwtProperties.getAccessTokenValidityInMs()).thenReturn(3600000L);
        when(jwtProperties.getRefreshTokenValidityInMs()).thenReturn(604800000L);
        when(jwtUtil.getExpiredAt(anyLong())).thenReturn(LocalDateTime.now().plusDays(7));

        // Mock all headers used in getIp() method
        when(httpServletRequest.getHeader("CF-Connecting-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("X-Forwarded-For")).thenReturn(null);
        when(httpServletRequest.getHeader("Proxy-Client-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("WL-Proxy-Client-IP")).thenReturn(null);
        when(httpServletRequest.getHeader("User-Agent")).thenReturn("test-agent");
        when(httpServletRequest.getRemoteAddr()).thenReturn("127.0.0.1");

        when(refreshTokenRepository.findByUserAndIssuedIpAndIssuedUserAgent(any(), anyString(), anyString()))
                .thenReturn(Optional.empty());

        TokenResponse tokenResponse = authService.checkDeviceAuthorized(authRequest, httpServletRequest);

        assertNotNull(tokenResponse);
        assertEquals("access-token", tokenResponse.getAccessToken());
        assertEquals("refresh-token", tokenResponse.getRefreshToken());
    }
}
