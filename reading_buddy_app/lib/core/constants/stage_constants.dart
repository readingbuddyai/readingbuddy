/// 스테이지 설정 정보
class StageConfig {
  final String id;          // API 호출용 ID (예: "1.1.1", "4.2")
  final String displayName; // 사용자에게 보여질 이름 (예: "모음 기초")
  final String category;    // 카테고리 (예: "모음", "자음")

  const StageConfig({
    required this.id,
    required this.displayName,
    required this.category,
  });

  /// 스테이지 ID로 설정 찾기
  static StageConfig? findById(String id) {
    try {
      return StageConstants.allStages.firstWhere((stage) => stage.id == id);
    } catch (e) {
      return null;
    }
  }

  /// 카테고리별 스테이지 목록 가져오기
  static List<StageConfig> getByCategory(String category) {
    return StageConstants.allStages.where((stage) => stage.category == category).toList();
  }

  /// "Stage 1.1.1" 형식의 전체 이름
  String get fullName => 'Stage $id';

  /// "모음 기초 (1.1.1)" 형식
  String get displayWithId => '$displayName ($id)';
}

/// 스테이지 상수 정의
class StageConstants {
  // 모음 단계
  static const vowelBasic = StageConfig(
    id: '1.1.1',
    displayName: '모음 기초',
    category: '모음',
  );

  static const vowelAdvanced = StageConfig(
    id: '1.1.2',
    displayName: '모음 심화',
    category: '모음',
  );

  // 자음 단계
  static const consonantBasic = StageConfig(
    id: '1.2.1',
    displayName: '자음 기초',
    category: '자음',
  );

  static const consonantAdvanced = StageConfig(
    id: '1.2.2',
    displayName: '자음 심화',
    category: '자음',
  );

  // 단어 단계
  static const wordSplit = StageConfig(
    id: '2',
    displayName: '단어 나누기',
    category: '단어',
  );

  // 글자 단계
  static const letterSplit = StageConfig(
    id: '3',
    displayName: '글자 쪼개기',
    category: '글자',
  );

  // 읽기 단계
  static const readingSlow = StageConfig(
    id: '4.1',
    displayName: '천천히 읽기',
    category: '읽기',
  );

  static const readingFast = StageConfig(
    id: '4.2',
    displayName: '빠르게 읽기',
    category: '읽기',
  );

  /// 모든 스테이지 목록 (순서대로)
  static const List<StageConfig> allStages = [
    vowelBasic,
    vowelAdvanced,
    consonantBasic,
    consonantAdvanced,
    wordSplit,
    letterSplit,
    readingSlow,
    readingFast,
  ];

  /// 스테이지 ID 목록 (API 호출용)
  static const List<String> allStageIds = [
    '1.1.1',
    '1.1.2',
    '1.2.1',
    '1.2.2',
    '2',
    '3',
    '4.1',
    '4.2',
  ];

  /// 카테고리 목록
  static const List<String> categories = [
    '모음',
    '자음',
    '단어',
    '글자',
    '읽기',
  ];

  /// KC 데이터가 있는 스테이지 목록 (학습 추이 분석 대상)
  /// Knowledge Component가 정의된 스테이지만 포함
  static const List<String> kcEnabledStages = [
    '1.1.1', // 모음 기초
    '1.1.2', // 모음 심화
    '1.2.1', // 자음 기초
    '1.2.2', // 자음 심화
    '4.1',   // 천천히 읽기
    '4.2',   // 빠르게 읽기
  ];
}

/// 편의 메서드를 위한 extension
extension StageConfigList on List<StageConfig> {
  /// ID로 스테이지 찾기
  StageConfig? findById(String id) {
    try {
      return firstWhere((stage) => stage.id == id);
    } catch (e) {
      return null;
    }
  }

  /// 표시 이름 목록
  List<String> get displayNames => map((stage) => stage.displayName).toList();

  /// ID 목록
  List<String> get ids => map((stage) => stage.id).toList();
}
