# Backend 자원 사용률 모니터링 가이드

이 문서는 Reading Buddy 백엔드 애플리케이션의 자원 사용률을 Prometheus와 Grafana를 통해 모니터링하는 방법을 설명합니다.

## 개요

이 모니터링 스택은 다음과 같은 구성 요소로 이루어져 있습니다:

- **Spring Boot Actuator**: 애플리케이션 메트릭 수집
- **Micrometer**: 메트릭 측정 및 포맷팅
- **Prometheus**: 메트릭 수집 및 저장
- **Grafana**: 메트릭 시각화 및 대시보드

## 모니터링 가능한 메트릭

### JVM 메트릭
- 힙/비힙 메모리 사용량
- 가비지 컬렉션 통계
- 스레드 수 및 상태
- 클래스 로더 정보

### 시스템 메트릭
- CPU 사용률 (시스템 및 프로세스)
- 파일 디스크립터
- 시스템 메모리

### 애플리케이션 메트릭
- HTTP 요청 수 및 응답 시간
- 데이터베이스 커넥션 풀 상태
- API 엔드포인트별 성능 지표

## 설치 및 실행 방법

### 1. 의존성 확인

`backend/build.gradle` 파일에 다음 의존성이 추가되어 있는지 확인하세요:

```gradle
implementation 'org.springframework.boot:spring-boot-starter-actuator'
implementation 'io.micrometer:micrometer-registry-prometheus'
```

### 2. Spring Boot 애플리케이션 실행

백엔드 애플리케이션을 실행합니다:

```bash
cd backend
./gradlew bootRun
```

애플리케이션이 시작되면 다음 엔드포인트에서 메트릭을 확인할 수 있습니다:

- Health: http://localhost:8080/actuator/health
- Metrics: http://localhost:8080/actuator/metrics
- Prometheus: http://localhost:8080/actuator/prometheus

### 3. Prometheus와 Grafana 실행

모니터링 디렉토리로 이동하여 Docker Compose를 실행합니다:

```bash
cd monitoring
docker-compose up -d
```

서비스 상태 확인:

```bash
docker-compose ps
```

### 4. Grafana 대시보드 접속

1. 브라우저에서 http://localhost:3000 접속
2. 기본 로그인 정보:
   - Username: `admin`
   - Password: `admin`
3. 첫 로그인 시 비밀번호 변경 권장 (선택사항)

### 5. 대시보드 확인

Grafana에 자동으로 프로비저닝된 "Reading Buddy - Spring Boot Metrics" 대시보드를 확인합니다:

1. 왼쪽 메뉴에서 "Dashboards" 클릭
2. "Reading Buddy - Spring Boot Metrics" 선택

## 대시보드 패널 설명

### JVM Memory Used / Max
- 힙과 비힙 메모리의 현재 사용량과 최대값
- 메모리 누수나 과도한 메모리 사용을 감지하는데 유용

### CPU Usage
- 시스템 전체 CPU 사용률과 프로세스 CPU 사용률
- 높은 CPU 사용률은 성능 최적화가 필요함을 나타냄

### HTTP Requests Rate
- 초당 HTTP 요청 수 (메서드, URI, 상태코드별)
- 트래픽 패턴과 피크 시간대 파악

### HTTP Request Duration
- 95번째 백분위수 응답 시간
- 느린 API 엔드포인트 식별

### Garbage Collection Count
- GC 이벤트 발생 빈도
- 빈번한 GC는 메모리 튜닝이 필요함을 의미

### Thread Count
- 활성 스레드와 데몬 스레드 수
- 스레드 누수 감지

### Database Connection Pool
- HikariCP 커넥션 풀 상태
- 활성/유휴/최대 커넥션 수 모니터링

## 메트릭 조회 예제

### Prometheus 쿼리 예제

Prometheus UI (http://localhost:9090)에서 다음 쿼리를 실행할 수 있습니다:

```promql
# JVM 힙 메모리 사용률
jvm_memory_used_bytes{area="heap"} / jvm_memory_max_bytes{area="heap"} * 100

# 평균 응답 시간 (최근 5분)
rate(http_server_requests_seconds_sum[5m]) / rate(http_server_requests_seconds_count[5m])

# 분당 HTTP 500 에러 수
increase(http_server_requests_seconds_count{status="500"}[1m])
```

## 커스텀 메트릭 추가하기

애플리케이션에 비즈니스 로직 관련 커스텀 메트릭을 추가할 수 있습니다:

```java
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.stereotype.Service;

@Service
public class BookService {
    private final Counter bookReadCounter;

    public BookService(MeterRegistry registry) {
        this.bookReadCounter = registry.counter("books.read.total");
    }

    public void recordBookRead() {
        bookReadCounter.increment();
    }
}
```

## 알림 설정 (선택사항)

Grafana에서 특정 임계값을 초과하면 알림을 받도록 설정할 수 있습니다:

1. 대시보드 패널 편집
2. "Alert" 탭 선택
3. 조건 설정 (예: CPU 사용률 > 80%)
4. 알림 채널 설정 (이메일, Slack 등)

## 트러블슈팅

### Prometheus가 메트릭을 수집하지 못하는 경우

1. Spring Boot 애플리케이션이 실행 중인지 확인
2. http://localhost:8080/actuator/prometheus 접속 가능한지 확인
3. `prometheus.yml`의 타겟 설정 확인
4. Docker 컨테이너에서 `host.docker.internal` 접근 가능한지 확인

### Grafana 대시보드에 데이터가 표시되지 않는 경우

1. Prometheus 데이터소스 연결 확인 (Configuration > Data Sources)
2. Prometheus에 데이터가 수집되고 있는지 확인 (http://localhost:9090/targets)
3. 대시보드의 시간 범위 확인 (우측 상단)

### Docker Compose 서비스 재시작

```bash
docker-compose restart
```

### 로그 확인

```bash
# Prometheus 로그
docker-compose logs prometheus

# Grafana 로그
docker-compose logs grafana
```

## 모니터링 중단

모니터링 스택을 중단하려면:

```bash
cd monitoring
docker-compose down
```

데이터도 함께 삭제하려면:

```bash
docker-compose down -v
```

## 프로덕션 환경 권장사항

1. **보안**: Grafana 기본 비밀번호 변경 및 HTTPS 설정
2. **영속성**: 볼륨 백업 전략 수립
3. **알림**: 중요 메트릭에 대한 알림 설정
4. **리텐션**: Prometheus 데이터 보관 기간 설정 (`--storage.tsdb.retention.time`)
5. **리소스**: 컨테이너 리소스 제한 설정

## 참고 자료

- [Spring Boot Actuator 문서](https://docs.spring.io/spring-boot/docs/current/reference/html/actuator.html)
- [Micrometer 문서](https://micrometer.io/docs)
- [Prometheus 문서](https://prometheus.io/docs/introduction/overview/)
- [Grafana 문서](https://grafana.com/docs/)
