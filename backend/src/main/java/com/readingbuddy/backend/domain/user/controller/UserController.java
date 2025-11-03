package com.readingbuddy.backend.domain.user.controller;

import com.readingbuddy.backend.auth.dto.LoginRequest;
import com.readingbuddy.backend.auth.dto.ReissueTokenRequest;
import com.readingbuddy.backend.auth.dto.TokenResponse;
import com.readingbuddy.backend.auth.service.AuthService;
import com.readingbuddy.backend.domain.user.dto.SignUpRequest;
import com.readingbuddy.backend.domain.user.service.UserService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

@Tag(name = "회원 관리", description = "회원 관련 API")
@Slf4j
@RequestMapping("/api/user")
@RequiredArgsConstructor
@RestController
public class UserController {
    private final UserService userService;
    private final AuthService authService;

    @Operation(summary = "회원가입", description = "사용자의 정보(JSON)와 프로필 이미지(파일)를 받아 회원으로 등록합니다.")
    @PostMapping(value = "/signup", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Void> signUp(@RequestBody @Valid SignUpRequest request) {
        userService.signUp(request);
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

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
