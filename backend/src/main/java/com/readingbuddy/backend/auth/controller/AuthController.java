package com.readingbuddy.backend.auth.controller;

import com.readingbuddy.backend.auth.dto.LoginRequest;
import com.readingbuddy.backend.auth.dto.ReissueTokenRequest;
import com.readingbuddy.backend.auth.dto.TokenResponse;
import com.readingbuddy.backend.auth.service.AuthService;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
@RestController
public class AuthController {

    private final AuthService authService;

    @Operation(summary = "로그인", description = "email과 password로 access 토큰과 refresh 토큰을 발행합니다.")
    @PostMapping("/login")
    public ResponseEntity<TokenResponse> login(@RequestBody LoginRequest request, HttpServletRequest servletRequest) {
        TokenResponse tokenResponse = authService.login(request, servletRequest);
        return ResponseEntity.ok(tokenResponse);
    }

    @Operation(summary = "access token 재발행", description = "refresh token으로 access 토큰과 refresh 토큰을 다시 발행합니다.")
    @PostMapping("/reissue-token")
    public ResponseEntity<TokenResponse> reissueToken(@RequestBody ReissueTokenRequest request) {
        TokenResponse tokenResponse = authService.reissueToken(request);
        return ResponseEntity.ok(tokenResponse);
    }
}
