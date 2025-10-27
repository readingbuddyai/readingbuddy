package com.readingbuddy.backend.auth;

import com.readingbuddy.backend.auth.domain.RefreshToken;
import com.readingbuddy.backend.domain.user.entity.User;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface RefreshTokenRepository extends JpaRepository<RefreshToken, String> {

    Optional<RefreshToken> findByToken(String token);
    Optional<RefreshToken> findByUserAndIssuedIpAndIssuedUserAgent(User user, String issuedIp, String issuedUserAgent);
}

