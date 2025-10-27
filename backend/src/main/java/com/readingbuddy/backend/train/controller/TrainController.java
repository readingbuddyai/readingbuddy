package com.readingbuddy.backend.train.controller;

import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController("/api/train/check")
public class CheckController {

    @PostMapping("/voice")
    public String voice() {
        return "voice";
    }

    @PostMapping("/answer")
    public String answer() {
        return "answer";
    }

}
