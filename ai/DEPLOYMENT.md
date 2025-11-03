# AI 서버 배포 가이드

## 📋 목차
- [사전 준비사항](#사전-준비사항)
- [EC2 인스턴스 설정](#ec2-인스턴스-설정)
- [배포 방법](#배포-방법)
- [GitLab CI/CD 설정](#gitlab-cicd-설정)
- [트러블슈팅](#트러블슈팅)

---

## 🔧 사전 준비사항

### 1. AWS 리소스
- **EC2 인스턴스**
  - 인스턴스 타입: `g4dn.xlarge` 이상 (GPU 필요)
  - AMI: Deep Learning AMI (Ubuntu 22.04)
  - 스토리지: 50GB 이상
  - Elastic IP: 할당 권장

- **Security Group 설정**
  ```
  Inbound Rules:
  - SSH (22): My IP
  - Custom TCP (8000): Backend 서버 IP
  - HTTPS (443): 0.0.0.0/0 (선택사항)
  ```

### 2. 로컬 준비물
- EC2 키 페어 (.pem 파일)
- Git 저장소 접근 권한
- 모델 파일 (2.4GB)

---

## 🚀 EC2 인스턴스 설정

### Step 1: EC2 인스턴스 생성
1. AWS Console → EC2 → Launch Instance
2. Deep Learning AMI (Ubuntu 22.04) 선택
3. g4dn.xlarge 인스턴스 타입 선택
4. 키 페어 생성 또는 선택
5. Security Group 설정
6. Elastic IP 할당

### Step 2: SSH 접속
```bash
# Windows PowerShell / Git Bash
ssh -i "your-key.pem" ubuntu@YOUR-EC2-IP
```

### Step 3: 초기 설정 스크립트 실행
```bash
# EC2에 setup-ec2.sh 업로드
# 로컬에서 실행
scp -i "your-key.pem" setup-ec2.sh ubuntu@YOUR-EC2-IP:/home/ubuntu/

# EC2에서 실행
ssh -i "your-key.pem" ubuntu@YOUR-EC2-IP
chmod +x setup-ec2.sh
./setup-ec2.sh

# 완료 후 재로그인 (Docker 권한 적용)
logout
```

### Step 4: 모델 파일 업로드
```bash
# 로컬에서 실행 (프로젝트 루트에서)
scp -i "your-key.pem" -r ai/models ubuntu@YOUR-EC2-IP:/home/ubuntu/app/ai/
```

### Step 5: 환경변수 설정
```bash
# EC2에서 실행
cd /home/ubuntu/app/ai
vi .env
```

`.env` 파일 내용:
```env
# AI Model Configuration
MODEL_PATH=./models/slplab_wav2vec2_korean

# Server Configuration
HOST=0.0.0.0
PORT=8000

# Audio Processing
MAX_AUDIO_LENGTH_SECONDS=30
MAX_FILE_SIZE_MB=10
```

### Step 6: Docker Compose 파일 업로드
```bash
# 로컬에서 실행
scp -i "your-key.pem" ai/docker-compose.yml ubuntu@YOUR-EC2-IP:/home/ubuntu/app/ai/
scp -i "your-key.pem" ai/Dockerfile ubuntu@YOUR-EC2-IP:/home/ubuntu/app/ai/
```

---

## 🔄 배포 방법

### 방법 1: 수동 배포

```bash
# EC2에 접속
ssh -i "your-key.pem" ubuntu@YOUR-EC2-IP

# 애플리케이션 디렉토리로 이동
cd /home/ubuntu/app/ai

# 최신 코드 가져오기 (Git 사용 시)
git pull origin master

# Docker 이미지 빌드
docker build -t korean-pronunciation-ai:latest .

# 컨테이너 실행
docker-compose up -d

# 로그 확인
docker-compose logs -f

# 헬스체크
curl http://localhost:8000/health
```

### 방법 2: GitLab CI/CD 자동 배포

1. **GitLab CI/CD Variables 설정**
   - Settings → CI/CD → Variables

   필요한 변수:
   ```
   AI_SERVER_IP: EC2 Public IP
   AI_SERVER_USER: ubuntu
   AI_SSH_PRIVATE_KEY: SSH 개인키 (.pem 파일 내용)
   ```

2. **코드 Push**
   ```bash
   git add .
   git commit -m "feat: AI server deployment setup"
   git push origin master
   ```

3. **GitLab에서 파이프라인 확인**
   - CI/CD → Pipelines
   - `deploy-ai` 작업 수동 실행

---

## ⚙️ GitLab CI/CD 설정

### GitLab Runner 등록 (선택사항)

EC2에 GitLab Runner를 설치하면 더 빠른 배포가 가능합니다.

```bash
# EC2에서 실행
curl -L https://packages.gitlab.com/install/repositories/runner/gitlab-runner/script.deb.sh | sudo bash
sudo apt-get install gitlab-runner

# Runner 등록
sudo gitlab-runner register \
  --url https://gitlab.com/ \
  --token YOUR_REGISTRATION_TOKEN \
  --executor docker \
  --docker-image docker:latest \
  --docker-volumes /var/run/docker.sock:/var/run/docker.sock

# 서비스 시작
sudo systemctl enable gitlab-runner
sudo systemctl start gitlab-runner
```

### CI/CD Variables

| 변수명 | 설명 | 예시 |
|--------|------|------|
| `AI_SERVER_IP` | EC2 Public IP | `13.125.123.45` |
| `AI_SERVER_USER` | SSH 사용자명 | `ubuntu` |
| `AI_SSH_PRIVATE_KEY` | SSH 개인키 | `.pem` 파일 전체 내용 |

---

## 🐛 트러블슈팅

### 1. GPU를 찾을 수 없음
```bash
# GPU 상태 확인
nvidia-smi

# 드라이버 재설치 (필요 시)
sudo apt-get install --reinstall nvidia-driver-525
```

### 2. Docker가 GPU를 인식하지 못함
```bash
# NVIDIA Docker 재설치
sudo apt-get purge nvidia-docker2
sudo apt-get install nvidia-docker2
sudo systemctl restart docker

# 테스트
docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi
```

### 3. 모델 파일을 찾을 수 없음
```bash
# 경로 확인
ls -la /home/ubuntu/app/ai/models/

# 권한 확인
sudo chown -R ubuntu:ubuntu /home/ubuntu/app/ai/models/
```

### 4. 포트가 이미 사용 중
```bash
# 포트 사용 확인
sudo lsof -i :8000

# 기존 컨테이너 중지
docker-compose down

# 모든 컨테이너 확인
docker ps -a
```

### 5. 메모리 부족
```bash
# 시스템 리소스 확인
htop
free -h
df -h

# Docker 정리
docker system prune -a
```

### 6. CI/CD SSH 연결 실패
- GitLab Variables에서 `AI_SSH_PRIVATE_KEY`가 올바른지 확인
- EC2 Security Group에서 SSH 포트(22)가 열려있는지 확인
- Elastic IP가 할당되어 있는지 확인

---

## 📊 모니터링

### 로그 확인
```bash
# 컨테이너 로그
docker-compose logs -f

# 최근 100줄
docker-compose logs --tail=100

# 특정 시간 이후
docker-compose logs --since="2024-01-01T10:00:00"
```

### 리소스 모니터링
```bash
# GPU 사용량
watch -n 1 nvidia-smi

# Docker 컨테이너 리소스
docker stats

# 시스템 리소스
htop
```

### 헬스체크
```bash
# API 헬스체크
curl http://localhost:8000/health

# 컨테이너 상태
docker-compose ps
```

---

## 🔐 보안 체크리스트

- [ ] EC2 Security Group에서 불필요한 포트 닫기
- [ ] SSH 키 파일 권한 설정 (600)
- [ ] .env 파일 gitignore 확인
- [ ] GitLab CI/CD Variables에 민감정보 저장
- [ ] CORS 설정에서 allow_origins 제한
- [ ] HTTPS 적용 (프로덕션 환경)

---

## 📝 체크리스트

배포 전:
- [ ] EC2 인스턴스 생성 완료
- [ ] Elastic IP 할당
- [ ] Security Group 설정
- [ ] SSH 접속 테스트
- [ ] GPU 확인 (nvidia-smi)

배포 중:
- [ ] setup-ec2.sh 실행
- [ ] 모델 파일 업로드
- [ ] .env 파일 생성
- [ ] Docker 이미지 빌드
- [ ] 컨테이너 실행

배포 후:
- [ ] 헬스체크 성공
- [ ] API 응답 테스트
- [ ] 로그 확인
- [ ] 모니터링 설정

---

## 📞 도움이 필요하신가요?

문제가 발생하면 다음을 확인하세요:
1. EC2 인스턴스가 실행 중인지
2. Security Group 설정이 올바른지
3. 모델 파일이 올바른 경로에 있는지
4. .env 파일이 제대로 설정되었는지
5. Docker 로그에서 에러 메시지 확인
