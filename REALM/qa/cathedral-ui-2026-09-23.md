# 성당 모자이크 UI 재구성 — 2026-09-23

## 구조 분석과 수정

- 기존 CreateRoomPanel/JoinRoomPanel은 배경 Image와 자식 Frame이 같은 장식을 중복 표시했다. 글자 위를 덮던 자식 Frame을 비활성화하고 불투명한 남색 독서 영역으로 교체했다.
- SplashPanel에도 Button이 있으므로 전역 버튼 스킨이 성당 배경을 btn_secondary로 덮었다. RealmSplash와 RealmCard 하위는 명시적으로 제외했다. 여러 텍스트를 가진 직업 타일도 일반 버튼에서 제외한다.
- 버튼에 지정한 어두운 글자색을 후속 전체 텍스트 스타일이 밝게 덮었다. 제목/본문/입력/행동 라벨을 역할별로 설정하고 전체 텍스트 재색칠을 제거했다.
- HostNameInput은 생성·참가 모두 사용하는데 생성 패널에만 있었다. LobbyContent/PlayerIdentity로 옮겨 공유 입력임을 표시한다. 컨트롤러의 직렬화 참조와 이벤트를 유지한다.
- 대기방 진입 때 LobbyHeaderTemplate을 숨겨 두 헤더가 겹치지 않게 한다. 대기방 제목을 다시 활성화하고 10명 목록의 행 높이와 간격을 정한다.
- 실제 클라이언트는 온라인 API를 쓰므로 로비의 LAN/방장 PC 서버 안내를 수정했다.

## 시각 규칙

- 기존 교회 창문과 장미창 자산을 유지한다. 모자이크 조각은 패널 위쪽의 얇은 띠, 버튼 양끝의 작은 마름모에 배치한다.
- 보호된 본문 영역은 #0e1727, 본문은 #f5ebd6, 보조문은 #bcc6d8. 배경에 대한 색상 대비는 각각 약 15:1, 10:1 이상이다.
- 1920×1080 기준 제목 36, 본문 26, 입력 27, 버튼 최대 29. 1366×768에서도 본문 약 18px을 유지한다.
- 생성은 금색, 참가는 코발트, 닫기는 짙은 남색. 텍스트와 위치도 역할을 구분한다.
- 레이아웃은 Rebuild로 편집 씬에 저장하며 Start에서 동일하게 적용한다. 이후에는 새로 생긴 버튼만 스타일링한다.

## 실제 Unity 검증

- Unity 컴파일 오류 0건.
- 1920×1080 및 1366×768 게임 뷰에서 로비 캡처, TMP 텍스트 크기 검사: 넘침 없음.
- 이름 입력, 생성, 코드 입력, 참가, 인원 선택의 중앙 레이캐스트: 모두 해당 제어에 도달.
- 생성 버튼의 빈 이름 검증, 참가 버튼의 빈 코드 검증 정상. 실제 이벤트 리스너 호출로 확인.
- 드롭다운 5~10명 항목 표시와 선택 강조 확인.
- RealmSplash.Dismiss를 통한 실제 페이드 완료 및 비활성화 확인.
- 직업 도감 열기/닫기, 점수·이름·설명 배치 확인.
- EnterWaitingRoom과 OnStateUpdated에 로컬 10명 데이터를 전달: 로비 헤더 숨김, 10행 표시, 목록 범위 및 텍스트 넘침 없음.
- hostNameInput, roomCodeInput, createRoomButton, joinRoomButton, playerTargetDropdown, statusText의 직렬화 연결 확인. RealmUiSkin 컴포넌트 1개.
- 온라인 방 생성/참가 통신 자체는 이번 시각 검증에서 호출하지 않았다.

## 증거

- evidence/cathedral-lobby-final.png
- evidence/cathedral-lobby-1366-1.png
- evidence/cathedral-dropdown-final.png
- evidence/cathedral-waiting-final.png
- evidence/cathedral-role-review.png

게임 뷰 해상도는 검증 후 원래 Full HD로 복원했다. 테스트 참가자 데이터는 플레이 모드 종료와 함께 폐기한다.
