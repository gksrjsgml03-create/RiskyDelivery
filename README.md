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

## 바로 플레이하기 (현재 PC)

`Builds/Windows/RiskyDelivery.exe`를 더블클릭합니다.
실행 파일은 로컬에 생성되어 있으며 Git에는 포함되지 않습니다.
게임 창을 클릭한 뒤 WASD 또는 방향키로 이동하세요.
배달 구역에서 이동 키를 놓고 Space로 정차하면 성공합니다.

## Windows 실행 파일 만들기

Unity 메뉴 **Risky Delivery → Build Windows**를 실행합니다.
성공하면 `Builds/Windows/RiskyDelivery.exe`를 실행합니다.
다른 PC로 옮길 때는 exe만이 아니라 Windows 폴더 전체를 복사합니다.

## 검증 상태

2026-09-13, Unity 6000.3.18f1 / Windows:

- Unity 프로젝트 가져오기 및 Windows Development 빌드 성공.
- 실제 Windows 플레이어에서 물리 이동 → 목적지 정차 → 배달 성공 → 재시작 자동 검사 통과 (종료 코드 0).
- 일반 게임 창에서 도로, 카트, 택배, HUD 표시 확인. 확인 시점까지 실행 로그에 예외 없음.
- 카트의 기본 박스 마찰이 가속을 방해하던 문제를 전용 구름 마찰값으로 수정.
- Unity 에디터의 Play 모드와 키보드 조작 전체에 대한 수동 검증은 별도입니다.

`Tools/Check-Compile.ps1`은 설치된 Unity 참조 DLL로 C# 컴파일만 검사합니다.

## 개발 원칙

기능 단위로 구현, 검증, 결과 안내, 커밋과 푸시를 진행합니다.
Library, Temp, Logs, Builds 등 생성 파일은 Git에 포함하지 않습니다.

Windows 빌드 후 `RiskyDelivery.exe -batchmode -nographics -risky-smoke-test -logFile smoke.log`로
실제 물리 이동 → 배달 구역 정차 → 성공 → 재시작을 자동 검사할 수 있습니다.
성공 시 로그에 `RISKY_DELIVERY_SMOKE_OK`를 기록하고 종료 코드 0을 반환합니다.
2026-09-13 실행 결과: 통과. 로컬 상세 로그는 `Logs/smoke.log`, 빌드 로그는 `Logs/build.log`에 있습니다.
