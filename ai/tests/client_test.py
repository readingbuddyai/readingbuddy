import requests

url = "http://127.0.0.1:8000/check_phoneme"
audio_path = "sample.wav"  # 테스트용 음성 파일
target_key = "ㄱ+ㅏ"       # targets.json에 정의된 key 사용

with open(audio_path, "rb") as f:
    files = {"file": (audio_path, f, "audio/wav")}
    data = {"target_key": target_key}
    response = requests.post(url, files=files, data=data)

print(response.json())
