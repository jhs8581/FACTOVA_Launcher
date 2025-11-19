# FACTOVA Launcher 릴리즈 가이드

## 자동 릴리즈 프로세스

이 프로젝트는 GitHub Actions를 통해 자동으로 릴리즈를 생성합니다.

### 릴리즈 생성 방법

1. **코드 커밋 및 푸시**
   ```bash
   git add .
   git commit -m "Release v1.0.0"
   git push origin master
   ```

2. **태그 생성**
   ```bash
   git tag v1.0.0
   ```
   또는 주석이 있는 태그:
   ```bash
   git tag -a v1.0.0 -m "FACTOVA Launcher v1.0.0 - 첫 릴리즈"
   ```

3. **태그 푸시**
   ```bash
   git push origin v1.0.0
   ```

4. **자동 빌드 및 릴리즈**
   - GitHub Actions가 자동으로 시작됩니다
   - 빌드가 완료되면 GitHub Releases에 자동으로 업로드됩니다
   - https://github.com/jhs8581/FACTOVA_Launcher/releases 에서 확인

### 릴리즈 파일 구성

GitHub Releases에 업로드되는 파일:
- `FACTOVA_Launcher.exe` - 메인 실행 파일
- `FACTOVA_Launcher.exe.config` - 설정 파일
- `Newtonsoft.Json.dll` - JSON 처리 라이브러리
- `PatchNotes.md` - 패치 노트

> **사용자는 4개 파일 모두 다운로드해야 합니다!**

### 버전 번호 규칙

Semantic Versioning을 따릅니다:
- **v1.0.0**: 주 버전.부 버전.패치 버전
- **v1.0.0**: 초기 릴리즈
- **v1.1.0**: 새로운 기능 추가
- **v1.0.1**: 버그 수정

### 릴리즈 확인

1. GitHub 저장소 방문: https://github.com/jhs8581/FACTOVA_Launcher
2. 상단 메뉴에서 "Releases" 클릭
3. 최신 릴리즈에서 필요한 파일 다운로드

### 수동 릴리즈 (선택사항)

자동 릴리즈를 사용하지 않고 수동으로 하려면:

1. Visual Studio에서 Release 모드로 빌드
2. `bin/Release` 폴더에서 필요한 파일 선택
   - FACTOVA_Launcher.exe
   - FACTOVA_Launcher.exe.config
   - Newtonsoft.Json.dll
   - PatchNotes.md
3. GitHub Releases에서 수동으로 업로드

### 릴리즈 전 체크리스트

- [ ] PatchNotes.md 업데이트
- [ ] 버전 번호 결정
- [ ] 모든 변경사항 커밋
- [ ] Release 모드 빌드 테스트
- [ ] 태그 생성 및 푸시

### 문제 해결

**빌드 실패 시:**
- GitHub Actions 탭에서 빌드 로그 확인
- 로컬에서 Release 빌드가 성공하는지 확인

**릴리즈가 생성되지 않을 때:**
- 태그 이름이 `v`로 시작하는지 확인 (예: v1.0.0)
- GitHub Actions 워크플로우가 활성화되어 있는지 확인

### 예제

```bash
# 현재 작업 저장
git add .
git commit -m "feat: 자동 백업 기능 추가"

# 마스터에 푸시
git push origin master

# 릴리즈 태그 생성
git tag -a v1.0.0 -m "첫 공식 릴리즈"

# 태그 푸시 (자동 빌드 및 릴리즈 시작)
git push origin v1.0.0
```

이후 GitHub Actions가 자동으로:
1. 코드 체크아웃
2. .NET Framework 빌드 환경 설정
3. NuGet 패키지 복원
4. Release 빌드
5. 필요한 파일들 준비
6. GitHub Release 페이지에 업로드

### 사용자 다운로드 방법

사용자에게 안내할 내용:
1. Releases 페이지 방문
2. 4개 파일 모두 다운로드
   - `FACTOVA_Launcher.exe`
   - `FACTOVA_Launcher.exe.config`
   - `Newtonsoft.Json.dll`
   - `PatchNotes.md`
3. 같은 폴더에 저장
4. `.exe` 파일 실행
