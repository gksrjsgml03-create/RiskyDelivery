# Risky Delivery — 위험한 택배 배달

Unity 6.3 LTS **6000.3.18f1** 기반의 소규모 3D 카트 배달 게임입니다.
외부 그래픽 에셋 없이 도로, 창고, 카트와 택배를 기본 도형으로 생성합니다.

## 첫 기능

- WASD / 방향키: 카트 이동
- Space: 감속 (누른 채 이동하면 저속 운행)
- R: 처음부터 다시 시작
- 가속과 감속에 반응하는 택배 흔들림
- 민트색 배달 구역 안에서 정차하면 성공 및 배달 시간 표시

현재 택배 흔들림은 시각 효과이며, 파손과 낙하는 아직 구현하지 않았습니다.

## Unity에서 실행

1. Unity Hub 로그인 후 라이선스를 활성화합니다.
2. Projects → Add → Add project from disk에서 이 폴더를 선택합니다.
3. Unity 6000.3.18f1로 프로젝트를 엽니다.
4. 상단 메뉴 **Risky Delivery → Prepare Project**를 한 번 실행합니다.
5. `Assets/Scenes/Delivery.unity`를 열고 Play를 누릅니다.
6. Game 화면을 클릭한 뒤 W 또는 위쪽 방향키로 출발합니다.
7. 도로 끝 민트색 구역에서 이동 키를 놓고 Space로 정차합니다.

## Windows 실행 파일 만들기

Unity 메뉴 **Risky Delivery → Build Windows**를 실행합니다.
성공하면 `Builds/Windows/RiskyDelivery.exe`를 실행합니다.
다른 PC로 옮길 때는 exe만이 아니라 Windows 폴더 전체를 복사합니다.

## 검증 상태

2026-09-13: 설치된 Unity 6000.3.18f1 참조 DLL을 사용한 전체 C# 컴파일 검사를 통과했습니다.
Unity 실행은 로컬 라이선스 미활성화 오류(exit code 198)로 차단됐습니다.
에디터 Play 테스트와 Windows 빌드는 아직 완료되지 않았습니다.
`Tools/Check-Compile.ps1`은 설치된 Unity 참조 DLL로 C# 컴파일만 검사합니다.

## 개발 원칙

기능 단위로 구현, 검증, 결과 안내, 커밋과 푸시를 진행합니다.
Library, Temp, Logs, Builds 등 생성 파일은 Git에 포함하지 않습니다.

Windows 빌드 후 `RiskyDelivery.exe -batchmode -nographics -risky-smoke-test -logFile smoke.log`로
실제 물리 이동 → 배달 구역 정차 → 성공 → 재시작을 자동 검사할 수 있습니다.
성공 시 로그에 `RISKY_DELIVERY_SMOKE_OK`를 기록하고 종료 코드 0을 반환합니다.
이 통합 검사는 아직 실행하지 못했습니다.
