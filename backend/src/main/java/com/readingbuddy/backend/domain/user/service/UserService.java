package com.readingbuddy.backend.domain.user.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.user.dto.SignUpRequest;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;

@Slf4j
@RequiredArgsConstructor
@Service
public class UserService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final UserKcMasteryRepository userKcMasteryRepository;
    private final KnowledgeComponentRepository knowledgeComponentRepository;

    @Transactional
    public void signUp(SignUpRequest request) {
        log.info("회원가입 시작 - email: {}, nickname: {}", request.getEmail(), request.getNickname());

        // unique 필드 1차 검증
        String email = request.getEmail();
        String nickname = request.getNickname();

        validateEmailIsUnique(email);
        validateNicknameIsUnique(nickname);

        // 사용자 생성 및 저장
        User user = User.builder()
                .email(request.getEmail())
                .password(passwordEncoder.encode(request.getPassword()))
                .nickname(request.getNickname())
                .build();
        userRepository.save(user);

        // 모든 KnowledgeComponent 조회
        List<KnowledgeComponent> allKcs = knowledgeComponentRepository.findAll();

        // 각 KC에 대해 초기 UserKcMastery 생성
        LocalDateTime now = LocalDateTime.now();
        for (KnowledgeComponent kc : allKcs) {
            // 단계별 추측 확률 설정
            float guessProbability = getGuessProbabilityByStage(kc.getStage());

            UserKcMastery userKcMastery = UserKcMastery.builder()
                    .user(user)
                    .knowledgeComponent(kc)
                    .pLearn(0.0f)      // 초기 숙달 확률 (아직 학습하지 않음)
                    .pTrain(0.1f)      // 학습 확률 (한 번 연습할 때 배울 확률)
                    .pGuess(guessProbability)      // 추측 확률 (단계별로 다름)
                    .pSlip(0.1f)      // 실수 확률 (알지만 틀릴 확률)
                    .createdAt(now)
                    .updatedAt(now)
                    .build();
            userKcMasteryRepository.save(userKcMastery);
        }
    }

    private void validateEmailIsUnique(String email) {
        if (userRepository.existsByEmail(email)) {
            throw new IllegalArgumentException("이미 존재하는 이메일입니다.");
        }
    }

    private void validateNicknameIsUnique(String nickname) {
        if (userRepository.existsByNickname(nickname)) {
            throw new IllegalArgumentException("이미 존재하는 닉네임입니다.");
        }
    }

    /**
     * 단계별 추측 확률(p_g) 반환
     * @param stage 단계 (예: "1.1.1", "1.1.2", "1.2.1", "1.2.2", "3", "4")
     * @return 추측 확률
     */
    private float getGuessProbabilityByStage(String stage) {
        return switch (stage) {
            case "1.1.1" -> 0.5f;   // 모음 기초 (2지선다)
            case "1.1.2" -> 0.33f;  // 모음 심화 (3지선다)
            case "1.2.1" -> 0.5f;   // 자음 기초 (2지선다)
            case "1.2.2" -> 0.33f;  // 자음 심화 (3지선다)
            case "3" -> 0.2f;       // 3단계 (5지선다)
            case "4" -> 0.2f;       // 4단계 (5지선다)
            default -> 0.2f;        // 기본값
        };
    }

}
