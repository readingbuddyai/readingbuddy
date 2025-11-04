#!/bin/bash
# ========================================
# AI 서버 빠른 배포 스크립트 (로컬에서 실행)
# ========================================
# 사용법: ./deploy.sh
# 권한 설정: chmod +x deploy.sh
# ========================================

set -e

# ========================================
# 설정 (여기를 수정하세요!)
# ========================================
EC2_KEY="your-key.pem"
EC2_IP="3.36.239.57"
EC2_USER="ubuntu"
APP_DIR="/home/ubuntu/app/ai"

# ========================================
# 색상 출력 설정
# ========================================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# ========================================
# 함수 정의
# ========================================
print_step() {
    echo -e "${GREEN}[$(date +%T)]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# ========================================
# 사전 검사
# ========================================
print_step "배포 사전 검사 중..."

if [ ! -f "$EC2_KEY" ]; then
    print_error "EC2 키 파일을 찾을 수 없습니다: $EC2_KEY"
    exit 1
fi

if [ -z "$EC2_IP" ] || [ "$EC2_IP" == "your-ec2-ip" ]; then
    print_error "EC2_IP를 설정해주세요!"
    exit 1
fi

print_step "✅ 사전 검사 완료"

# ========================================
# EC2 연결 테스트
# ========================================
print_step "EC2 연결 테스트 중..."
if ssh -i "$EC2_KEY" -o ConnectTimeout=5 $EC2_USER@$EC2_IP "echo 'Connected'" > /dev/null 2>&1; then
    print_step "✅ EC2 연결 성공"
else
    print_error "EC2 연결 실패. IP와 키 파일을 확인하세요."
    exit 1
fi

# ========================================
# 배포 옵션 선택
# ========================================
echo ""
echo "========================================="
echo "배포 옵션을 선택하세요:"
echo "========================================="
echo "1. 전체 배포 (모델 + 코드 + Docker)"
echo "2. 코드만 업데이트"
echo "3. Docker 재시작만"
echo "4. 모델 파일만 업로드"
echo "========================================="
read -p "선택 (1-4): " choice

case $choice in
    1)
        DEPLOY_TYPE="full"
        ;;
    2)
        DEPLOY_TYPE="code"
        ;;
    3)
        DEPLOY_TYPE="restart"
        ;;
    4)
        DEPLOY_TYPE="model"
        ;;
    *)
        print_error "잘못된 선택입니다."
        exit 1
        ;;
esac

# ========================================
# 배포 실행
# ========================================

# 1. 모델 파일 업로드
if [ "$DEPLOY_TYPE" == "full" ] || [ "$DEPLOY_TYPE" == "model" ]; then
    print_step "📦 모델 파일 업로드 중... (2.4GB, 시간이 걸릴 수 있습니다)"

    # EC2에 디렉토리 생성
    ssh -i "$EC2_KEY" $EC2_USER@$EC2_IP "mkdir -p $APP_DIR/models"

    # 모델 파일 전송
    scp -i "$EC2_KEY" -r models/ $EC2_USER@$EC2_IP:$APP_DIR/

    print_step "✅ 모델 파일 업로드 완료"
fi

# 2. 코드 및 설정 파일 업로드
if [ "$DEPLOY_TYPE" == "full" ] || [ "$DEPLOY_TYPE" == "code" ]; then
    print_step "📤 코드 및 설정 파일 업로드 중..."

    # 필요한 파일들만 전송
    scp -i "$EC2_KEY" requirements.txt $EC2_USER@$EC2_IP:$APP_DIR/
    scp -i "$EC2_KEY" run.py $EC2_USER@$EC2_IP:$APP_DIR/
    scp -i "$EC2_KEY" Dockerfile $EC2_USER@$EC2_IP:$APP_DIR/
    scp -i "$EC2_KEY" docker-compose.yml $EC2_USER@$EC2_IP:$APP_DIR/
    scp -i "$EC2_KEY" -r app/ $EC2_USER@$EC2_IP:$APP_DIR/

    # .env 파일 확인 (없으면 경고)
    if [ -f ".env" ]; then
        print_warning ".env 파일이 로컬에 있습니다. 업로드하시겠습니까? (y/N)"
        read -p "> " upload_env
        if [ "$upload_env" == "y" ] || [ "$upload_env" == "Y" ]; then
            scp -i "$EC2_KEY" .env $EC2_USER@$EC2_IP:$APP_DIR/
        fi
    else
        print_warning ".env 파일이 없습니다. EC2에서 직접 생성하세요."
    fi

    print_step "✅ 코드 업로드 완료"
fi

# 3. Docker 이미지 빌드 및 실행
if [ "$DEPLOY_TYPE" == "full" ] || [ "$DEPLOY_TYPE" == "code" ] || [ "$DEPLOY_TYPE" == "restart" ]; then
    print_step "🐳 Docker 컨테이너 재시작 중..."

    ssh -i "$EC2_KEY" $EC2_USER@$EC2_IP << 'ENDSSH'
        set -e
        cd /home/ubuntu/app/ai

        echo "🛑 기존 컨테이너 중지 중..."
        docker-compose down 2>/dev/null || true

        echo "🔨 Docker 이미지 빌드 중..."
        docker build -t korean-pronunciation-ai:latest .

        echo "🚀 새 컨테이너 실행 중..."
        docker-compose up -d

        echo "⏳ 컨테이너 시작 대기 중..."
        sleep 15

        echo "🔍 헬스체크 중..."
        if curl -f http://localhost:8000/health > /dev/null 2>&1; then
            echo "✅ 서버가 정상적으로 실행되었습니다!"
        else
            echo "⚠️  헬스체크 실패. 로그를 확인하세요."
            docker-compose logs --tail=50
            exit 1
        fi
ENDSSH

    print_step "✅ Docker 컨테이너 시작 완료"
fi

# ========================================
# 배포 완료
# ========================================
echo ""
echo "========================================="
echo -e "${GREEN}🎉 배포가 완료되었습니다!${NC}"
echo "========================================="
echo "서버 주소: http://$EC2_IP:8000"
echo "헬스체크: http://$EC2_IP:8000/health"
echo ""
echo "로그 확인: ssh -i $EC2_KEY $EC2_USER@$EC2_IP 'cd $APP_DIR && docker-compose logs -f'"
echo "========================================="
