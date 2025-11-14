package com.readingbuddy.backend.domain.train.service;


import lombok.RequiredArgsConstructor;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class TrainManagerScheduler {

    private final TrainManager trainManager;

    @Scheduled(cron = "0 0 0 * * *")
    public void clearExpiredSessions() {
        trainManager.clearExpiredSessions();
    }

}
