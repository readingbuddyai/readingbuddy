package com.readingbuddy.backend.auth.service;

import com.readingbuddy.backend.auth.repository.RefreshTokenRepository;
import com.readingbuddy.backend.auth.entity.RefreshToken;
import com.readingbuddy.backend.auth.dto.*;
import com.readingbuddy.backend.auth.jwt.JWTUtil;
import com.readingbuddy.backend.common.properties.JwtProperties;
import com.readingbuddy.backend.domain.dashboard.repository.AttendanceHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.AttendHistories;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Optional;

@Slf4j
@RequiredArgsConstructor
@Service
public class AuthService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JWTUtil jwtUtil;
    private final JwtProperties jwtProperties;
    private final RefreshTokenRepository refreshTokenRepository;
    private final DeviceSessionManager deviceSessionManager;
    private final AttendanceHistoriesRepository attendanceHistoriesRepository;

    @Transactional
    public TokenResponse login(LoginRequest request, HttpServletRequest servletRequest) {
        User user = userRepository.findByEmail(request.getEmail())
                .orElseThrow(() -> new IllegalArgumentException("로그인 정보에 일치하는 회원이 없습니다."));

        if (!passwordEncoder.matches(request.getPassword(), user.getPassword())) {
            throw new IllegalArgumentException("로그인 정보에 일치하는 회원이 없습니다.");
        }


        // 유효 시간 1시간
        String accessToken = createAccessToken(user);
        // 유효 시간 최대 7일
        String refreshToken = createRefreshToken(user);
        saveRefreshToken(user, refreshToken, servletRequest);

        return createTokenResponse(accessToken, refreshToken);
    }

    @Transactional
    public TokenResponse reissueToken(ReissueTokenRequest request) {
        String refreshTokenValue = request.getRefreshToken();
        if (jwtUtil.isExpired(refreshTokenValue)) {
            throw new IllegalArgumentException("만료된 리프레시 토큰입니다.");
        }

        RefreshToken refreshToken = refreshTokenRepository.findByToken(refreshTokenValue)
                .orElseThrow(() -> new IllegalArgumentException("유효하지 않은 리프레시 토큰입니다."));

        User user = refreshToken.getUser();

        String newAccessToken = createAccessToken(user);
        String newRefreshToken = createRefreshToken(user);

        refreshToken.rotate(newRefreshToken, jwtUtil.getExpiredAt(jwtProperties.getRefreshTokenValidityInMs()));

        return createTokenResponse(newAccessToken, newRefreshToken);
    }

    public DeviceLoginResponse createDeviceLoginResponse(){
        return DeviceLoginResponse.builder()
                .authCode(deviceSessionManager.generateDeviceCodeSession())
                .build();
    }

    public void authorizeDeviceCode(DeviceCodeRequest request, Long userId) {
        String deviceAuthCode = request.getDeviceAuthCode();

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("로그인 정보에 일치하는 회원이 없습니다."));

        DeviceSessionInfo deviceSessionInfo = deviceSessionManager.getSession(deviceAuthCode);

        deviceSessionManager.authorizeDevice(deviceSessionInfo,user.getId());

        checkAndCreateAttendance(user);
    }

    @Transactional
    public TokenResponse checkDeviceAuthorized(DeviceCodeRequest request, HttpServletRequest servletRequest) {
        Long userId = deviceSessionManager.checkAuthorizedDevice(request.getDeviceAuthCode());

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("로그인 정보에 일치하는 회원이 없습니다."));

        String accessToken = createAccessToken(user);
        String refreshToken = createRefreshToken(user);
        saveRefreshToken(user, refreshToken, servletRequest);

        return createTokenResponse(accessToken, refreshToken);
    }

    public void checkAttendance(Long userId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("로그인 정보에 일치하는 회원이 없습니다."));
        checkAndCreateAttendance(user);
    }

    private TokenResponse createTokenResponse(String accessToken, String refreshToken) {
        return TokenResponse.builder()
                .accessToken(accessToken)
                .refreshToken(refreshToken)
                .build();
    }

    private String createAccessToken(User user) {
        return jwtUtil.createAccessToken(
                String.valueOf(user.getId()),
                user.getEmail(),
                user.getNickname(),
                jwtProperties.getAccessTokenValidityInMs());
    }

    private String createRefreshToken(User user) {
        return jwtUtil.createRefreshToken(
                String.valueOf(user.getId()),
                jwtProperties.getRefreshTokenValidityInMs());
    }

    private String getIp(HttpServletRequest servletRequest) {
        String ip = servletRequest.getHeader("CF-Connecting-IP");

        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = servletRequest.getHeader("X-Forwarded-For");
        }
        // XFF 헤더에 IP가 여러 개일 경우
        if (ip != null && !ip.isEmpty() && !"unknown".equalsIgnoreCase(ip)) {
            ip = ip.split(",")[0];
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = servletRequest.getHeader("Proxy-Client-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = servletRequest.getHeader("WL-Proxy-Client-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = servletRequest.getRemoteAddr();
        }
        return ip;
    }

    private void saveRefreshToken(User user, String tokenValue, HttpServletRequest servletRequest) {
        log.info("userId: {}, IP: {}, userAgent: {}", user.getId(), getIp(servletRequest), servletRequest.getHeader("User-Agent"));
        Optional<RefreshToken> existingRefreshToken = refreshTokenRepository.findByUserAndIssuedIpAndIssuedUserAgent(user, getIp(servletRequest), servletRequest.getHeader("User-Agent"));
        LocalDateTime expiredAt = jwtUtil.getExpiredAt(jwtProperties.getRefreshTokenValidityInMs());

        if (existingRefreshToken.isPresent()) {
            RefreshToken refreshToken = existingRefreshToken.get();
            refreshToken.rotate(tokenValue, expiredAt);
            return;
        }
        refreshTokenRepository.save(RefreshToken.builder()
                .token(tokenValue)
                .user(user)
                .issuedUserAgent(servletRequest.getHeader("User-Agent"))
                .issuedIp(getIp(servletRequest))
                .expiredAt(expiredAt)
                .build());
    }

    private void checkAndCreateAttendance(User user) {
        LocalDate today = LocalDate.now();
        Optional<AttendHistories> existingAttendance =
                attendanceHistoriesRepository.findByUserIdAndDate(user.getId(), today);

        if (existingAttendance.isPresent()) {
            return;
        }

        AttendHistories newAttendance = AttendHistories.builder()
                .user(user)
                .attendDate(today)
                .playtime(0)
                .build();

        attendanceHistoriesRepository.save(newAttendance);
    }

}
