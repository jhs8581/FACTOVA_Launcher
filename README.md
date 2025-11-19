# FACTOVA Launcher

GMES 시스템을 위한 설정 관리 및 실행 도구

## 다운로드

최신 릴리즈: [Releases 페이지](https://github.com/jhs8581/FACTOVA_Launcher/releases)

### 설치 방법
1. Releases 페이지에서 최신 버전의 다음 파일들을 다운로드:
   - `FACTOVA_Launcher.exe` (메인 실행 파일)
   - `FACTOVA_Launcher.exe.config` (설정 파일)
   - `Newtonsoft.Json.dll` (필수 라이브러리)
   - `PatchNotes.md` (선택사항)
2. 모든 파일을 같은 폴더에 저장
3. `.exe` 파일 실행

> **중요**: `.exe`, `.exe.config`, `.dll` 파일 모두 같은 폴더에 있어야 합니다!

## 주요 기능

### 운영 모드
- Config 폴더별 설정 관리
- 버튼 클릭으로 간편한 실행
- 자동 백업 기능
- 우클릭 메뉴:
  - 백업 없이 바로 실행
  - 수동 백업
  - URL 커스터마이징

### 개발자 모드
- Ctrl + 타이틀 클릭으로 전환
- Config만 변경 (프로그램 실행 안 함)
- 불러오기, 백업, 설정, 패치노트 탭

### 설정
- GMES 버전 선택 (GMES 1.0 / GMES 2.0)
- 언어 선택 (한국어 / English)
- 자동 백업 활성화/비활성화
- 폰트 크기 조정
- 시작 시 개발자 모드 진입

## 시스템 요구사항

- Windows OS
- .NET Framework 4.5.2 이상
- GMES Shop Floor Control for LGE 설치

## 개발

### 빌드 방법
1. Visual Studio 2017 이상
2. .NET Framework 4.5.2 SDK
3. Solution 열기 및 빌드

### 릴리즈
자세한 내용은 [RELEASE.md](RELEASE.md) 참고

## 라이선스

? 2025. 김정환, 정해상. All rights reserved.  
Original Author: 송재헌

## 문의

Issues: https://github.com/jhs8581/FACTOVA_Launcher/issues
