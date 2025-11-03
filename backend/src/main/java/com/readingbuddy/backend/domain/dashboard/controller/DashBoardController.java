package com.readingbuddy.backend.domain.dashboard.controller;

import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.service.DashBoardService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/dashboard")
@RequiredArgsConstructor
public class DashBoardController {

    private final DashBoardService dashBoardService;

    @GetMapping(value = "/stage/info")
    public ResponseEntity<ApiResponse<?>> stageInfo(@RequestParam String stage) {

        return ResponseEntity.status(HttpStatus.OK).body(ApiResponse.success("a"));
    }
}
