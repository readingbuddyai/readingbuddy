// 녹음 관련 전역 변수
let jamoMediaRecorder = null;
let jamoAudioChunks = [];
let jamoRecordedBlob = null;
let jamoRecordingTimer = null;
let jamoRecordingStartTime = 0;

let syllableMediaRecorder = null;
let syllableAudioChunks = [];
let syllableRecordedBlob = null;
let syllableRecordingTimer = null;
let syllableRecordingStartTime = 0;

let wordMediaRecorder = null;
let wordAudioChunks = [];
let wordRecordedBlob = null;
let wordRecordingTimer = null;
let wordRecordingStartTime = 0;

// API URL 가져오기
function getApiUrl() {
    const apiUrl = document.getElementById('api-url').value.trim();
    return apiUrl || 'http://3.36.239.57:8000';
}

// 로딩 상태 표시
function showLoading() {
    document.getElementById('loading').classList.remove('hidden');
}

function hideLoading() {
    document.getElementById('loading').classList.add('hidden');
}

// 결과 표시 함수
function showResult(elementId, data, isSuccess = true) {
    const resultDiv = document.getElementById(elementId);
    resultDiv.className = 'result show ' + (isSuccess ? 'success' : 'error');

    let html = '<h3>' + (isSuccess ? '✅ 분석 완료' : '❌ 오류') + '</h3>';

    if (typeof data === 'object') {
        // 피드백이 있는 경우 강조 표시
        if (data.feedback) {
            const feedbackClass = data.is_correct ? 'correct' : 'incorrect';
            const icon = data.is_correct ? '🎉' : '💡';
            html += `<div class="feedback ${feedbackClass}">${icon} ${data.feedback}</div>`;
        }

        // 자모 비교 표시
        if (data.type === 'word' || data.type === 'syllable') {
            html += '<div class="comparison-section">';
            html += '<h4>📝 자모 비교</h4>';

            // 정답 자모
            if (data.syllables) {
                html += '<div class="jamo-display">';
                html += '<strong>정답:</strong> ';
                html += '<span class="jamo-box target">';
                if (data.type === 'word') {
                    // 단어인 경우 음절별로 표시 (음절 구분 없이)
                    html += data.syllables.map(syl => syl.join(' ')).join(' ');
                } else {
                    // 음절인 경우
                    html += data.decomposed ? data.decomposed.join(' ') : '';
                }
                html += '</span>';
                html += '</div>';
            }

            // 모델 출력 자모
            html += '<div class="jamo-display">';
            html += '<strong>인식:</strong> ';
            html += '<span class="jamo-box model">';
            html += data.decoded_tokens ? data.decoded_tokens.join(' ') : '';
            html += '</span>';
            html += '</div>';

            html += '</div>';
        }

        // 전체 JSON 데이터 (접기/펼치기 가능하게)
        html += '<details style="margin-top: 15px;">';
        html += '<summary style="cursor: pointer; color: #666;">📋 전체 데이터 보기</summary>';
        html += '<pre style="margin-top: 10px;">' + JSON.stringify(data, null, 2) + '</pre>';
        html += '</details>';
    } else {
        html += '<p>' + data + '</p>';
    }

    resultDiv.innerHTML = html;
}

// 1. 헬스체크
async function checkHealth() {
    const apiUrl = getApiUrl();
    showLoading();

    try {
        const response = await fetch(`${apiUrl}/health/`);
        const data = await response.json();

        if (response.ok) {
            showResult('health-result', data, true);
        } else {
            showResult('health-result', data, false);
        }
    } catch (error) {
        showResult('health-result', '서버에 연결할 수 없습니다: ' + error.message, false);
    } finally {
        hideLoading();
    }
}

// 2. 자모 단위 검사
async function checkJamo() {
    const apiUrl = getApiUrl();
    const target = document.getElementById('jamo-target').value.trim();
    const fileInput = document.getElementById('jamo-file');

    if (!target) {
        alert('검사할 자모를 입력하세요.');
        return;
    }

    // 녹음된 오디오 또는 업로드된 파일 확인
    let audioFile = null;
    if (jamoRecordedBlob) {
        // 녹음된 오디오를 WAV 형식의 File 객체로 변환
        audioFile = new File([jamoRecordedBlob], 'recorded_audio.webm', { type: 'audio/webm' });
    } else if (fileInput.files[0]) {
        audioFile = fileInput.files[0];
    } else {
        alert('오디오를 녹음하거나 파일을 선택하세요.');
        return;
    }

    const formData = new FormData();
    formData.append('file', audioFile);
    formData.append('target', target);

    showLoading();

    try {
        const response = await fetch(`${apiUrl}/check/jamo`, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (response.ok) {
            showResult('jamo-result', data, true);
        } else {
            showResult('jamo-result', data, false);
        }
    } catch (error) {
        showResult('jamo-result', '요청 실패: ' + error.message, false);
    } finally {
        hideLoading();
    }
}

// 3. 음절 단위 검사
async function checkSyllable() {
    const apiUrl = getApiUrl();
    const target = document.getElementById('syllable-target').value.trim();
    const fileInput = document.getElementById('syllable-file');

    if (!target) {
        alert('검사할 음절을 입력하세요.');
        return;
    }

    // 녹음된 오디오 또는 업로드된 파일 확인
    let audioFile = null;
    if (syllableRecordedBlob) {
        audioFile = new File([syllableRecordedBlob], 'recorded_audio.webm', { type: 'audio/webm' });
    } else if (fileInput.files[0]) {
        audioFile = fileInput.files[0];
    } else {
        alert('오디오를 녹음하거나 파일을 선택하세요.');
        return;
    }

    const formData = new FormData();
    formData.append('file', audioFile);
    formData.append('target', target);

    showLoading();

    try {
        const response = await fetch(`${apiUrl}/check/syllable`, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (response.ok) {
            showResult('syllable-result', data, true);
        } else {
            showResult('syllable-result', data, false);
        }
    } catch (error) {
        showResult('syllable-result', '요청 실패: ' + error.message, false);
    } finally {
        hideLoading();
    }
}

// 4. 단어 단위 검사
async function checkWord() {
    const apiUrl = getApiUrl();
    const target = document.getElementById('word-target').value.trim();
    const fileInput = document.getElementById('word-file');

    if (!target) {
        alert('검사할 단어를 입력하세요.');
        return;
    }

    // 녹음된 오디오 또는 업로드된 파일 확인
    let audioFile = null;
    if (wordRecordedBlob) {
        audioFile = new File([wordRecordedBlob], 'recorded_audio.webm', { type: 'audio/webm' });
    } else if (fileInput.files[0]) {
        audioFile = fileInput.files[0];
    } else {
        alert('오디오를 녹음하거나 파일을 선택하세요.');
        return;
    }

    const formData = new FormData();
    formData.append('file', audioFile);
    formData.append('target', target);

    showLoading();

    try {
        const response = await fetch(`${apiUrl}/check/word`, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (response.ok) {
            showResult('word-result', data, true);
        } else {
            showResult('word-result', data, false);
        }
    } catch (error) {
        showResult('word-result', '요청 실패: ' + error.message, false);
    } finally {
        hideLoading();
    }
}

// ========================================
// 녹음 기능 - 자모 단위
// ========================================

async function toggleJamoRecording() {
    if (jamoMediaRecorder && jamoMediaRecorder.state === 'recording') {
        stopJamoRecording();
    } else {
        await startJamoRecording();
    }
}

async function startJamoRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        jamoMediaRecorder = new MediaRecorder(stream);
        jamoAudioChunks = [];

        jamoMediaRecorder.ondataavailable = (event) => {
            jamoAudioChunks.push(event.data);
        };

        jamoMediaRecorder.onstop = () => {
            jamoRecordedBlob = new Blob(jamoAudioChunks, { type: 'audio/webm' });
            const audioUrl = URL.createObjectURL(jamoRecordedBlob);
            document.getElementById('jamo-audio-player').src = audioUrl;
            document.getElementById('jamo-audio-preview').classList.remove('hidden');

            // 스트림 정리
            stream.getTracks().forEach(track => track.stop());
        };

        jamoMediaRecorder.start();
        jamoRecordingStartTime = Date.now();

        // UI 업데이트
        document.getElementById('jamo-record-icon').textContent = '⏹️';
        document.getElementById('jamo-record-text').textContent = '녹음 중지';
        document.getElementById('jamo-record-btn').classList.add('recording');
        document.getElementById('jamo-record-timer').classList.remove('hidden');

        // 타이머 시작
        jamoRecordingTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - jamoRecordingStartTime) / 1000);
            const minutes = Math.floor(elapsed / 60);
            const seconds = elapsed % 60;
            document.getElementById('jamo-record-timer').textContent =
                `${minutes}:${seconds.toString().padStart(2, '0')}`;
        }, 1000);

    } catch (error) {
        console.error('마이크 접근 오류:', error);
        alert('마이크에 접근할 수 없습니다. 브라우저 권한을 확인해주세요.');
    }
}

function stopJamoRecording() {
    if (jamoMediaRecorder && jamoMediaRecorder.state === 'recording') {
        jamoMediaRecorder.stop();
        clearInterval(jamoRecordingTimer);

        // UI 업데이트
        document.getElementById('jamo-record-icon').textContent = '🎤';
        document.getElementById('jamo-record-text').textContent = '녹음 시작';
        document.getElementById('jamo-record-btn').classList.remove('recording');
        document.getElementById('jamo-record-timer').classList.add('hidden');
        document.getElementById('jamo-record-timer').textContent = '0:00';

        // 녹음 완료 후 자동으로 검사 실행
        // onstop 이벤트 후 실행되도록 setTimeout 사용
        setTimeout(() => {
            if (jamoRecordedBlob) {
                checkJamo();
            }
        }, 100);
    }
}

function handleJamoFileUpload() {
    // 파일이 업로드되면 녹음된 오디오 초기화
    jamoRecordedBlob = null;
    document.getElementById('jamo-audio-preview').classList.add('hidden');
}

// ========================================
// 녹음 기능 - 음절 단위
// ========================================

async function toggleSyllableRecording() {
    if (syllableMediaRecorder && syllableMediaRecorder.state === 'recording') {
        stopSyllableRecording();
    } else {
        await startSyllableRecording();
    }
}

async function startSyllableRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        syllableMediaRecorder = new MediaRecorder(stream);
        syllableAudioChunks = [];

        syllableMediaRecorder.ondataavailable = (event) => {
            syllableAudioChunks.push(event.data);
        };

        syllableMediaRecorder.onstop = () => {
            syllableRecordedBlob = new Blob(syllableAudioChunks, { type: 'audio/webm' });
            const audioUrl = URL.createObjectURL(syllableRecordedBlob);
            document.getElementById('syllable-audio-player').src = audioUrl;
            document.getElementById('syllable-audio-preview').classList.remove('hidden');

            stream.getTracks().forEach(track => track.stop());
        };

        syllableMediaRecorder.start();
        syllableRecordingStartTime = Date.now();

        document.getElementById('syllable-record-icon').textContent = '⏹️';
        document.getElementById('syllable-record-text').textContent = '녹음 중지';
        document.getElementById('syllable-record-btn').classList.add('recording');
        document.getElementById('syllable-record-timer').classList.remove('hidden');

        syllableRecordingTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - syllableRecordingStartTime) / 1000);
            const minutes = Math.floor(elapsed / 60);
            const seconds = elapsed % 60;
            document.getElementById('syllable-record-timer').textContent =
                `${minutes}:${seconds.toString().padStart(2, '0')}`;
        }, 1000);

    } catch (error) {
        console.error('마이크 접근 오류:', error);
        alert('마이크에 접근할 수 없습니다. 브라우저 권한을 확인해주세요.');
    }
}

function stopSyllableRecording() {
    if (syllableMediaRecorder && syllableMediaRecorder.state === 'recording') {
        syllableMediaRecorder.stop();
        clearInterval(syllableRecordingTimer);

        document.getElementById('syllable-record-icon').textContent = '🎤';
        document.getElementById('syllable-record-text').textContent = '녹음 시작';
        document.getElementById('syllable-record-btn').classList.remove('recording');
        document.getElementById('syllable-record-timer').classList.add('hidden');
        document.getElementById('syllable-record-timer').textContent = '0:00';

        // 녹음 완료 후 자동으로 검사 실행
        setTimeout(() => {
            if (syllableRecordedBlob) {
                checkSyllable();
            }
        }, 100);
    }
}

function handleSyllableFileUpload() {
    syllableRecordedBlob = null;
    document.getElementById('syllable-audio-preview').classList.add('hidden');
}

// ========================================
// 녹음 기능 - 단어 단위
// ========================================

async function toggleWordRecording() {
    if (wordMediaRecorder && wordMediaRecorder.state === 'recording') {
        stopWordRecording();
    } else {
        await startWordRecording();
    }
}

async function startWordRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        wordMediaRecorder = new MediaRecorder(stream);
        wordAudioChunks = [];

        wordMediaRecorder.ondataavailable = (event) => {
            wordAudioChunks.push(event.data);
        };

        wordMediaRecorder.onstop = () => {
            wordRecordedBlob = new Blob(wordAudioChunks, { type: 'audio/webm' });
            const audioUrl = URL.createObjectURL(wordRecordedBlob);
            document.getElementById('word-audio-player').src = audioUrl;
            document.getElementById('word-audio-preview').classList.remove('hidden');

            stream.getTracks().forEach(track => track.stop());
        };

        wordMediaRecorder.start();
        wordRecordingStartTime = Date.now();

        document.getElementById('word-record-icon').textContent = '⏹️';
        document.getElementById('word-record-text').textContent = '녹음 중지';
        document.getElementById('word-record-btn').classList.add('recording');
        document.getElementById('word-record-timer').classList.remove('hidden');

        wordRecordingTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - wordRecordingStartTime) / 1000);
            const minutes = Math.floor(elapsed / 60);
            const seconds = elapsed % 60;
            document.getElementById('word-record-timer').textContent =
                `${minutes}:${seconds.toString().padStart(2, '0')}`;
        }, 1000);

    } catch (error) {
        console.error('마이크 접근 오류:', error);
        alert('마이크에 접근할 수 없습니다. 브라우저 권한을 확인해주세요.');
    }
}

function stopWordRecording() {
    if (wordMediaRecorder && wordMediaRecorder.state === 'recording') {
        wordMediaRecorder.stop();
        clearInterval(wordRecordingTimer);

        document.getElementById('word-record-icon').textContent = '🎤';
        document.getElementById('word-record-text').textContent = '녹음 시작';
        document.getElementById('word-record-btn').classList.remove('recording');
        document.getElementById('word-record-timer').classList.add('hidden');
        document.getElementById('word-record-timer').textContent = '0:00';

        // 녹음 완료 후 자동으로 검사 실행
        setTimeout(() => {
            if (wordRecordedBlob) {
                checkWord();
            }
        }, 100);
    }
}

function handleWordFileUpload() {
    wordRecordedBlob = null;
    document.getElementById('word-audio-preview').classList.add('hidden');
}

// 페이지 로드 시 초기화
document.addEventListener('DOMContentLoaded', function() {
    console.log('발음 검사 API 테스터가 준비되었습니다.');

    // Enter 키로 폼 제출 방지
    document.querySelectorAll('input').forEach(input => {
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
            }
        });
    });
});
