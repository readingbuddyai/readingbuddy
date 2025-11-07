package com.readingbuddy.backend.domain.user.controller;

import com.readingbuddy.backend.auth.dto.*;
import com.readingbuddy.backend.auth.service.AuthService;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.user.dto.SignUpRequest;
import com.readingbuddy.backend.domain.user.service.UserService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

@Tag(name = "회원 관리", description = "회원 관련 API")
@Slf4j
@RequestMapping("/api/user")
@RequiredArgsConstructor
@RestController
public class UserController {
    private final UserService userService;
    private final AuthService authService;

    @Operation(summary = "회원가입", description = "사용자의 정보를 받아 회원으로 등록합니다." +
            "이메일 형식 검증" +
            "비밀번호에 영문과 숫자 필수 포함, 특수문자는 !@#$%&*만 사용 가능")
    @PostMapping(value = "/signup", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ApiResponse<Void>> signUp(@RequestBody @Valid SignUpRequest request) {
        try{
            userService.signUp(request);
            return ResponseEntity
                    .status(HttpStatus.OK)
                    .body(ApiResponse.success("회원가입을 성공적으로 하였습니다.", null));
        }catch (IllegalArgumentException e){
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        }catch (Exception e){
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("회원가입 중 오류가 발생했습니다."+e.getMessage()));
        }

    }

    @Operation(summary = "로그인", description = "email과 password로 access 토큰과 refresh 토큰을 발행합니다.")
    @PostMapping("/login")
    public ResponseEntity<ApiResponse<TokenResponse>> login(@RequestBody LoginRequest request, HttpServletRequest servletRequest) {
        try{
            TokenResponse tokenResponse = authService.login(request, servletRequest);
            return ResponseEntity
                    .status(HttpStatus.OK)
                    .body(ApiResponse.success("로그인을 성공적으로 하였습니다.", tokenResponse));
        }catch (IllegalArgumentException e){
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        }catch (Exception e){
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("로그인 중 오류가 발생했습니다."+e.getMessage()));
        }
    }

    @Operation(summary = "access token 재발행", description = "refresh token으로 access 토큰과 refresh 토큰을 다시 발행합니다.")
    @PostMapping("/reissue-token")
    public ResponseEntity<ApiResponse<TokenResponse>> reissueToken(@RequestBody ReissueTokenRequest request) {

        try{
            TokenResponse tokenResponse = authService.reissueToken(request);
            return ResponseEntity
                    .status(HttpStatus.OK)
                    .body(ApiResponse.success("토큰을 성공적으로 발행 하였습니다.", tokenResponse));
        }catch (IllegalArgumentException e){
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        }catch (Exception e){
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("토큰 발행 중 오류가 발생했습니다."+e.getMessage()));
        }
    }



    @Operation(summary = "device code 생성 요청", description = "device code를 생성합니다.")
    @GetMapping("/activation")
    public ResponseEntity<ApiResponse<DeviceLoginResponse>> activationCode() {
        DeviceLoginResponse deviceLoginResponse = authService.createDeviceLoginResponse();
        return ResponseEntity.status(HttpStatus.CREATED)
                .body(ApiResponse.success("인증 코드가 생성 되었습니다.",deviceLoginResponse));
    }


    @Operation(summary = "device code 입력", description = "device code를 입력하여 검증합니다.")
    @PostMapping("/auth-device")
    public ResponseEntity<ApiResponse<Void>> authorizeDeviceCode(
            @RequestBody DeviceCodeRequest request,
            @AuthenticationPrincipal CustomUserDetails customUserDetails) {
        try{
            Long userId = customUserDetails.getId();
            authService.authorizeDeviceCode(request,userId);
            return ResponseEntity.status(HttpStatus.ACCEPTED)
                    .body(ApiResponse.success("기기가 인증 되었습니다.",null));

        }catch (Exception e){
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error(e.getMessage()));
        }

    }

    @Operation(summary = "device polling", description = "device 에서 지정된 주기마다 요청합니다. ")
    @PostMapping("/polling")
    public ResponseEntity<ApiResponse<TokenResponse>> authorizeDeviceCode(
            @RequestBody DeviceCodeRequest request,
            HttpServletRequest servletRequest) {
        try{
            TokenResponse tokenResponse = authService.checkDeviceAuthorized(request,servletRequest);
            return ResponseEntity.status(HttpStatus.ACCEPTED)
                    .body(ApiResponse.success("기기가 인증 되었습니다.",tokenResponse));
        }catch (Exception e){
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error(e.getMessage()));
        }
    }

    @Operation(summary = "출석 체크", description = "자정이 지나면 해당 API를 이용하여 출석체크를 합니다. ")
    @PostMapping("/attend")
    public ResponseEntity<ApiResponse<Void>> checkAttendance(
            @AuthenticationPrincipal CustomUserDetails customUserDetails) {
        try{
            Long userId = customUserDetails.getId();
            authService.checkAttendance(userId);
            return ResponseEntity.status(HttpStatus.ACCEPTED)
                    .body(ApiResponse.success("출석이 완료 되었습니다.",null));
        }catch (Exception e){
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error(e.getMessage()));
        }
    }
}
