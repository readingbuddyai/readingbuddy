#!/bin/bash
# ========================================
# AI 서버 EC2 초기 설정 스크립트
# ========================================
# 사용법: GPU EC2 인스턴스 생성 후 SSH 접속하여 실행
# chmod +x setup-ec2.sh && ./setup-ec2.sh
# ========================================

set -e  # 에러 발생 시 스크립트 중단

echo "🚀 AI 서버 EC2 초기 설정을 시작합니다..."

# ========================================
# 1. 시스템 업데이트
# ========================================
echo "📦 시스템 패키지 업데이트 중..."
sudo apt-get update
sudo apt-get upgrade -y

# ========================================
# 2. Docker 설치
# ========================================
echo "🐳 Docker 설치 중..."
sudo apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Docker GPG 키 추가
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Docker 저장소 추가
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Docker 설치
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Docker 서비스 시작 및 자동 시작 설정
sudo systemctl start docker
sudo systemctl enable docker

# 현재 사용자를 docker 그룹에 추가
sudo usermod -aG docker $USER

echo "✅ Docker 설치 완료"

# ========================================
# 3. NVIDIA Docker (nvidia-docker2) 설치
# ========================================
echo "🎮 NVIDIA Docker 설치 중..."

# NVIDIA Docker 저장소 추가
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg
curl -s -L https://nvidia.github.io/libnvidia-container/$distribution/libnvidia-container.list | \
    sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
    sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

# NVIDIA Docker 설치
sudo apt-get update
sudo apt-get install -y nvidia-docker2

# Docker 재시작
sudo systemctl restart docker

echo "✅ NVIDIA Docker 설치 완료"

# ========================================
# 4. Docker Compose 설치
# ========================================
echo "🔧 Docker Compose 설치 중..."
sudo apt-get install -y docker-compose

echo "✅ Docker Compose 설치 완료"

# ========================================
# 5. 디렉토리 구조 생성
# ========================================
echo "📁 애플리케이션 디렉토리 생성 중..."
mkdir -p /home/ubuntu/app/ai/models
mkdir -p /home/ubuntu/app/ai/logs

echo "✅ 디렉토리 생성 완료"

# ========================================
# 6. 기타 유틸리티 설치
# ========================================
echo "🛠️ 기타 유틸리티 설치 중..."
sudo apt-get install -y \
    htop \
    vim \
    git \
    curl \
    wget \
    unzip

echo "✅ 유틸리티 설치 완료"

# ========================================
# 7. GPU 확인
# ========================================
echo "🎮 GPU 상태 확인 중..."
nvidia-smi || echo "⚠️ GPU를 찾을 수 없습니다. GPU 인스턴스인지 확인하세요."

# ========================================
# 8. Docker GPU 테스트
# ========================================
echo "🧪 Docker GPU 테스트 중..."
sudo docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi || echo "⚠️ Docker GPU 테스트 실패"

# ========================================
# 완료
# ========================================
echo ""
echo "========================================="
echo "✅ EC2 초기 설정이 완료되었습니다!"
echo "========================================="
echo ""
echo "다음 단계:"
echo "1. 로그아웃 후 재로그인하여 Docker 권한 적용"
echo "   logout (또는 exit)"
echo ""
echo "2. 모델 파일 업로드"
echo "   scp -r models/ ubuntu@EC2-IP:/home/ubuntu/app/ai/"
echo ""
echo "3. .env 파일 생성"
echo "   cd /home/ubuntu/app/ai"
echo "   vi .env"
echo ""
echo "4. docker-compose.yml 업로드 및 실행"
echo "   docker-compose up -d"
echo ""
echo "========================================="
