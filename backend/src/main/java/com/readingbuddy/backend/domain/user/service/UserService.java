package com.readingbuddy.backend.domain.user.service;

import com.readingbuddy.backend.domain.user.dto.SignUpRequest;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Slf4j
@RequiredArgsConstructor
@Service
public class UserService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;

    public void signUp(SignUpRequest request) {

        // unique 필드 1차 검증
        String email = request.getEmail();
        String nickname = request.getNickname();

        validateEmailIsUnique(email);
        validateNicknameIsUnique(nickname);

        User user = User.builder()
                .email(request.getEmail())
                .password(passwordEncoder.encode(request.getPassword()))
                .nickname(request.getNickname())
                .build();;
        userRepository.save(user);
    }

    private void validateEmailIsUnique(String email) {
        if (userRepository.existsByEmail(email)) {
            throw new RuntimeException();
        }
    }

    private void validateNicknameIsUnique(String nickname) {
        if (userRepository.existsByNickname(nickname)) {
            throw new RuntimeException();
        }
    }

}
