package com.readingbuddy.backend.domain.user.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Schema(description = "회원가입 요청 DTO")
@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class SignUpRequest {

    @Schema(description = "email", example = "user@example.com")
    @NotBlank(message = "이메일 필수")
    @Email(message = "올바르지 않은 이메일 형식")
    @Size(max = 100, message = "이메일은 100자 이하")
    private String email;

    @Schema(description = "비밀번호: 영문과 숫자 필수 포함, 특수문자는 !@#$%&*만 사용 가능", example = "password!@123")
    // 특수문자 "^"는 예기치 않은 오류 및 보안 취약점이 될 수 있어서 제외
    @NotBlank(message = "비밀번호 필수")
    @Pattern(regexp = "^(?=.*[A-Za-z])(?=.*[0-9])[A-Za-z0-9!@#$%&*]+$",
            message = "비밀번호에 영문과 숫자 필수 포함, 특수문자는 !@#$%&*만 사용 가능"
    )
    @Size(min = 8, max = 100, message = "비밀번호 8자 이상, 100자 이하")
    private String password;

    @Schema(description = "닉네임", example = "리딩버디")
    @NotBlank(message = "닉네임 필수")
    @Size(min = 2, max = 10, message = "닉네임 2자 이상, 10자 이하")
    @Pattern(regexp = "^[ㄱ-ㅎ가-힣A-Za-z0-9]+$", message = "닉네임에 한글, 영문, 숫자만 가능")
    private String nickname;

}
