# 저장소 가이드라인

## 프로젝트 구조 및 모듈 구성
저장소는 Unity 클라이언트용 `frontend/`와 Spring Boot API용 `backend/`로 나뉩니다. `frontend/unity`에는 2022.3.62f2 프로젝트가 있으며, `Assets/Scripts`는 이미 도메인(`Auth`, `Characters`, `Managers`, `Stage`, `UI`, `Utils`, `XR`)별로 정리되어 있으므로 새 MonoBehaviour는 가장 가까운 폴더에 추가하세요. 씬은 `Assets/Scenes`, 재사용 가능한 콘텐츠는 `Prefabs/`와 `ScriptableObjects/`에, 렌더 및 입력 기본값은 `_Core/`와 `UniversalRenderPipelineGlobalSettings.asset` 아래에 둡니다. 백엔드 소스는 `src/main/java/com/readingbuddy/backend`, 동일한 구조의 테스트는 `src/test/java/...`에 있고, `docker-compose.yml`과 Gradle 래퍼 같은 배포 자산은 모듈 루트에 있습니다.

## 빌드, 테스트 및 개발 명령어
- `cd backend && ./gradlew bootRun` - 개발 친화적인 설정으로 API를 시작합니다.
- `cd backend && ./gradlew test` - JUnit 5 테스트를 실행합니다; PR마다 수행하세요.
- `cd backend && docker-compose up -d` - `docker-compose.yml`에 정의된 Postgres와 pgAdmin을 기동합니다.
- `"<UnityInstall>/Editor/Unity.exe" -projectPath frontend/unity -quit -batchmode -runTests -testPlatform EditMode -testResults Logs/EditMode.xml` - 헤드리스 Unity EditMode 회귀 테스트를 실행합니다.
- `"<UnityInstall>/Editor/Unity.exe" -projectPath frontend/unity -quit -batchmode -buildWindows64 Builds/ReadingBuddy.exe -sceneList Assets/Scenes/Home.unity` - Windows 빌드를 생성합니다(필요 시 씬 목록 조정).
- XR 플레이 테스트는 에디터에서 `frontend/unity`를 열어 진행하고, 임시 빌드는 `frontend/unity/Builds/` 아래에 보관하세요.

## 코딩 스타일 및 네이밍 컨벤션
Unity 스크립트는 4칸 들여쓰기, PascalCase 클래스, camelCase private 멤버를 사용하며, 직렬화된 참조는 `[SerializeField] private`로 노출합니다. 생명주기 메서드(`Awake`, `Start`, `Update`)를 먼저 배치하고, 그다음 public API, 이후 코루틴을 둡니다. 큰 플로우는 기능별 스크립트로 나누어 해당 폴더에 둡니다. Java 파일은 Spring 기본값을 따르며 패키지는 `com.readingbuddy.backend` 아래에 두고, 컴포넌트는 `@RestController`, `@Service`, `@Repository`로 주석 처리합니다. 생성자 주입을 선호하고, 스테이징 전에 IDE 포매터를 실행하세요.

## 테스트 가이드라인
백엔드 테스트는 `src/test/java`에 두고 `FooServiceTests`, `FooControllerTests`처럼 명명합니다. `@SpringBootTest` 또는 `MockMvc`를 사용해 성공/실패 경로 모두를 커버하세요. Unity EditMode 테스트는 `Assets/Tests/EditMode`, PlayMode 테스트는 `Assets/Tests/PlayMode`에 두며, 둘 다 `MethodUnderTest_State_Result` 명명 규칙을 따릅니다. 자동화가 불완전할 경우 XR 수동 스모크 단계는 PR 설명에 기록하세요.

## 커밋 및 PR 가이드라인
히스토리는 `<Type>: <summary>` 형식(`Feat`, `Fix`, `Docs`, `Chore`, `Merge`)을 사용하므로, 명령형 요약을 ~60자 내로 유지하세요(예: `Feat: wire lobby stage flow`). 트래킹 이슈(`Closes #123`)를 참조하고, 수정한 씬이나 스크립트를 나열하며, 시각적 업데이트는 클립 또는 스크린샷을 첨부하세요(대용량 캡처는 `frontend/unity/Images` 또는 외부 링크에 저장). PR에는 테스트 증빙(Gradle, Unity EditMode/PlayMode, docker 스모크)을 포함하고, 스키마나 에셋 마이그레이션이 있다면 강조하세요.

## 보안 및 설정 팁
실제 자격 증명은 커밋하지 마세요. `docker-compose.yml`에 있는 기본 `admin/admin` 값은 환경 변수나 로컬 `.env`로 덮어씁니다. 모든 씬에 영향을 주는 공유 렌더 파이프라인이나 입력 업데이트가 `_Core/` 및 `ProjectSettings/` 안에 있으므로 해당 diff는 꼼꼼히 검토하세요.
