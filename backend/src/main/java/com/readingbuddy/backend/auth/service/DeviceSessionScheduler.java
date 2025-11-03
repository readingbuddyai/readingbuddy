package com.readingbuddy.backend.auth.service;

import lombok.RequiredArgsConstructor;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class DeviceSessionScheduler {

    private final DeviceSessionManager deviceSessionManager;

    @Scheduled(cron = "0 0 * * * *")
    public void clearExpiredSessions() {
        deviceSessionManager.clearExpiredSessions();
    }

}
