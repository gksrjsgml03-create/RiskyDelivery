# Windows 배포 운영

이 게임은 오프라인 싱글 플레이입니다. Docker, 게임 서버, 데이터베이스를 상시 운영할 필요가 없습니다. GitHub Releases에서 Windows ZIP을 배포합니다.

## 새 버전 배포

1. ProjectSettings와 ProjectTools의 버전을 함께 올리고 `Docs/releases/v버전.md`에 변경 사항과 알려진 문제를 작성합니다.
2. 변경 사항을 커밋하고 main에 푸시합니다.
3. Unity가 활성화된 Windows 개발 PC에서 다음 명령을 실행합니다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Publish-Release.ps1 -CapturePreview
```

비밀정보 검사 → 새 Unity Release 빌드 → 전체 게임 테스트 → 선택적 화면 검수용 캡처 → ZIP·SHA256·소스 커밋 메타데이터 → 태그·비공개 초안 업로드 → GitHub Actions 실행 순서입니다. GitHub의 별도 Windows 머신이 다운로드한 ZIP을 실제로 실행해 전체 챕터와 게임 기능을 검사한 뒤에만 공개합니다. 검사 중 업로드 파일이 바뀌면 공개를 중단합니다. Unity 계정 암호나 라이선스를 GitHub에 올리지 않습니다.

소스에서 Unity 빌드를 시작하는 단계는 이 PC의 명령으로 실행됩니다. main에 푸시하는 것만으로 클라우드 Unity 빌드가 실행되는 구성은 아닙니다. 클라우드 빌드까지 확장하려면 별도 Unity CI 활성화 설정이 필요합니다. Security checks는 push·PR·매주 자동 실행합니다.

## 실패 및 재시도

Actions의 `Release validation and publication`에서 실패한 단계를 확인합니다. 미검증 버전은 Draft로 남습니다. 네트워크 등 일시적인 문제는 동일 태그로 워크플로를 다시 실행합니다. 게임 코드 수정이 필요하면 버전을 올려 새 태그로 다시 배포합니다. 이미 공개한 파일은 같은 버전으로 덮어쓰지 않습니다.

## 장애 대응 및 이전 버전 안내

문제 버전을 발견하면 릴리스 설명에 증상과 영향 범위를 추가하고, 직전 정상 버전을 Latest로 지정합니다 (`gh release edit v이전버전 --latest`). 기존 플레이어에게는 정상 버전 ZIP을 별도 폴더에 풀도록 안내합니다. 배포 파일 교체만으로 이미 받은 실행 파일을 원격 변경할 수 없습니다. 자동 업데이트와 코드 서명은 아직 제공하지 않습니다.

## 제보·확인

공개 Issues 양식으로 버전·재현 순서를 수집합니다. 로그에는 PC 경로 등이 포함될 수 있어 원본을 무조건 공개 업로드하지 않습니다. 비밀정보·취약점은 Security의 비공개 제보를 사용합니다. Actions 실패 내역과 Releases 다운로드 수를 확인합니다. 게임 내부 원격 분석이나 개인정보 수집 기능은 추가하지 않았습니다.

## 보안 범위

Gitleaks로 Git 이력과 배포 대상 작업 파일을 검사하고, GitHub Secret scanning 및 Push protection을 사용합니다. `.githooks/pre-push`를 로컬에 연결하면 푸시 전에도 검사합니다. Unity 엔진 취약점, 모든 형태의 개인정보, 바이너리 내부의 모든 문제를 이 검사만으로 보증하지는 않습니다. 과거 커밋 작성자 이메일은 비밀키 검사와 별개이며 기존 공개 이력에 남아 있습니다.
