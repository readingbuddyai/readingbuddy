package com.readingbuddy.backend.auth.entity;

import com.readingbuddy.backend.domain.user.entity.User;
import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@AllArgsConstructor
@Builder
public class RefreshToken {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false, updatable = false)
    private User user;

    @Column(length = 255, nullable = false)
    private String token;

    @Column(length = 50)
    private String issuedIp;

    @Column(length = 255)
    private String issuedUserAgent;

    @Column(nullable = false)
    private LocalDateTime expired_At;

    public boolean isExpired() {
        return LocalDateTime.now().isAfter(this.expired_At);
    }

    public void rotate(String newToken, LocalDateTime newExpiredAt) {
        this.token = newToken;
        this.expired_At = newExpiredAt;
    }
}
