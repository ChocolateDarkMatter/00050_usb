# Tasks: USB-over-IP Solution for Windows

**Input**: Design documents from `/specs/001-usb-over-ip/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: Tests are included as the plan mentions xUnit testing.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- **Shared Library**: `src/UsbOverIp.Shared/`
- **Server Service**: `src/UsbOverIp.BackendService/`
- **Client App**: `src/UsbOverIp.ClientAgent/`
- **Tests**: `tests/UsbOverIp.*.Tests/`

---

## Phase 1: Setup (Project Structure)

**Purpose**: Initialize solution structure and configure build

- [X] T001 Create solution file at src/UsbOverIp.sln
- [X] T002 [P] Create UsbOverIp.Shared class library project at src/UsbOverIp.Shared/UsbOverIp.Shared.csproj
- [X] T003 [P] Create UsbOverIp.BackendService worker service project at src/UsbOverIp.BackendService/UsbOverIp.BackendService.csproj
- [X] T004 [P] Create UsbOverIp.ClientAgent WPF project at src/UsbOverIp.ClientAgent/UsbOverIp.ClientAgent.csproj
- [X] T005 [P] Create test project at tests/UsbOverIp.Shared.Tests/UsbOverIp.Shared.Tests.csproj
- [X] T006 [P] Create test project at tests/UsbOverIp.BackendService.Tests/UsbOverIp.BackendService.Tests.csproj
- [X] T007 [P] Create test project at tests/UsbOverIp.ClientAgent.Tests/UsbOverIp.ClientAgent.Tests.csproj
- [X] T008 Add project references (Shared to BackendService and ClientAgent)
- [X] T009 [P] Configure .editorconfig for code style at .editorconfig
- [X] T010 [P] Create .gitignore for .NET projects at .gitignore

---

## Phase 2: Foundational (Shared Models & Infrastructure)

**Purpose**: Core infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Shared Models (required by all stories)

- [ ] T011 [P] Create UsbDevice model at src/UsbOverIp.Shared/Models/UsbDevice.cs
- [ ] T012 [P] Create ServerInfo model at src/UsbOverIp.Shared/Models/ServerInfo.cs
- [ ] T013 [P] Create ServerAnnouncement model at src/UsbOverIp.Shared/Models/ServerAnnouncement.cs
- [ ] T014 [P] Create AttachmentInfo model with AttachmentState enum at src/UsbOverIp.Shared/Models/AttachmentInfo.cs
- [ ] T015 [P] Create ServerConfiguration model at src/UsbOverIp.Shared/Models/ServerConfiguration.cs
- [ ] T016 [P] Create ClientPreferences model at src/UsbOverIp.Shared/Models/ClientPreferences.cs
- [ ] T017 [P] Create API response DTOs (HealthResponse, DeviceListResponse, ErrorResponse) at src/UsbOverIp.Shared/Models/ApiResponses.cs
- [ ] T018 [P] Create API request DTOs (AttachRequest) at src/UsbOverIp.Shared/Models/ApiRequests.cs

### Shared Contracts/Interfaces

- [ ] T019 [P] Create IUsbipdClient interface at src/UsbOverIp.Shared/Contracts/IUsbipdClient.cs
- [ ] T020 [P] Create IDeviceManager interface at src/UsbOverIp.Shared/Contracts/IDeviceManager.cs

### Shared Utilities

- [ ] T021 Create NetworkHelper utility (get local IPs, subnet broadcast) at src/UsbOverIp.Shared/Utilities/NetworkHelper.cs

### Logging Infrastructure

- [ ] T022 Configure Serilog in BackendService Program.cs at src/UsbOverIp.BackendService/Program.cs
- [ ] T023 [P] Create appsettings.json for BackendService with logging config at src/UsbOverIp.BackendService/appsettings.json
- [ ] T024 [P] Create appsettings.json for ClientAgent with logging config at src/UsbOverIp.ClientAgent/appsettings.json

### Unit Tests for Shared Models

- [ ] T025 [P] Create model validation tests at tests/UsbOverIp.Shared.Tests/Models/UsbDeviceTests.cs
- [ ] T026 [P] Create model serialization tests at tests/UsbOverIp.Shared.Tests/Models/SerializationTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Server Administrator Shares USB Device (Priority: P1) 🎯 MVP

**Goal**: Server can enumerate USB devices, share/unshare them via API

**Independent Test**: Connect USB device to server, start service, call GET /api/devices and verify device appears; call POST /api/devices/{id}/share and verify device becomes shared

### Tests for User Story 1

- [ ] T027 [P] [US1] Create contract test for usbipd state JSON parsing at tests/UsbOverIp.BackendService.Tests/Contract/UsbipdOutputParsingTests.cs
- [ ] T028 [P] [US1] Create unit test for DeviceManager at tests/UsbOverIp.BackendService.Tests/Unit/DeviceManagerTests.cs
- [ ] T029 [P] [US1] Create integration test for API endpoints at tests/UsbOverIp.BackendService.Tests/Integration/ApiEndpointsTests.cs

### Implementation for User Story 1

#### UsbipdClient (wrapper for usbipd-win CLI)

- [ ] T030 [US1] Create UsbipdStateParser to parse JSON from `usbipd state` at src/UsbOverIp.BackendService/UsbipdClient/UsbipdStateParser.cs
- [ ] T031 [US1] Implement UsbipdClient with ListDevicesAsync, BindDeviceAsync, UnbindDeviceAsync at src/UsbOverIp.BackendService/UsbipdClient/UsbipdClient.cs
- [ ] T032 [US1] Add usbipd-win availability check with clear error message at src/UsbOverIp.BackendService/UsbipdClient/UsbipdClient.cs

#### DeviceManager (state tracking)

- [ ] T033 [US1] Create DeviceStateStore for in-memory device state at src/UsbOverIp.BackendService/DeviceManager/DeviceStateStore.cs
- [ ] T034 [US1] Implement DeviceManager with device enumeration and share/unshare logic at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs

#### REST API Endpoints

- [ ] T035 [US1] Implement GET /api/health endpoint at src/UsbOverIp.BackendService/Api/HealthEndpoints.cs
- [ ] T036 [US1] Implement GET /api/devices endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs
- [ ] T037 [US1] Implement GET /api/devices/{busId} endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs
- [ ] T038 [US1] Implement POST /api/devices/{busId}/share endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs
- [ ] T039 [US1] Implement POST /api/devices/{busId}/unshare endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs

#### Service Host

- [ ] T040 [US1] Configure Kestrel and register services in Program.cs at src/UsbOverIp.BackendService/Program.cs
- [ ] T041 [US1] Create Worker.cs for background tasks (device polling) at src/UsbOverIp.BackendService/Worker.cs

**Checkpoint**: Server can enumerate and share USB devices via REST API

---

## Phase 4: User Story 2 - Client User Attaches to Remote USB Device (Priority: P1) 🎯 MVP

**Goal**: Client can query server for devices and attach/detach

**Independent Test**: Start client, manually enter server address, see device list, click attach on a shared device, verify device appears in Windows Device Manager

**Note**: US1 and US2 together form the complete MVP

### Tests for User Story 2

- [ ] T042 [P] [US2] Create unit test for ServerApiClient at tests/UsbOverIp.ClientAgent.Tests/Unit/ServerApiClientTests.cs
- [ ] T043 [P] [US2] Create unit test for UsbipdClient attach/detach at tests/UsbOverIp.BackendService.Tests/Unit/UsbipdClientTests.cs

### Server-side Implementation for User Story 2

- [ ] T044 [US2] Add AttachDeviceAsync and DetachDeviceAsync to UsbipdClient at src/UsbOverIp.BackendService/UsbipdClient/UsbipdClient.cs
- [ ] T045 [US2] Implement POST /api/devices/{busId}/attach endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs
- [ ] T046 [US2] Implement POST /api/devices/{busId}/detach endpoint at src/UsbOverIp.BackendService/Api/DevicesEndpoints.cs
- [ ] T047 [US2] Add exclusive access enforcement in DeviceManager at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs
- [ ] T048 [US2] Add client timeout detection (30s) for crashed clients at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs

### Client-side Implementation for User Story 2

- [ ] T049 [US2] Create ServerApiClient HTTP client service at src/UsbOverIp.ClientAgent/Services/ServerApiClient.cs
- [ ] T050 [US2] Create MainViewModel with server list and device operations at src/UsbOverIp.ClientAgent/ViewModels/MainViewModel.cs
- [ ] T051 [US2] Create DeviceTreeViewModel for hierarchical device display at src/UsbOverIp.ClientAgent/ViewModels/DeviceTreeViewModel.cs
- [ ] T052 [US2] Create MainWindow.xaml with device tree UI at src/UsbOverIp.ClientAgent/MainWindow.xaml
- [ ] T053 [US2] Implement MainWindow.xaml.cs with view initialization at src/UsbOverIp.ClientAgent/MainWindow.xaml.cs
- [ ] T054 [US2] Create DeviceTreeView.xaml user control at src/UsbOverIp.ClientAgent/Views/DeviceTreeView.xaml
- [ ] T055 [US2] Create App.xaml with resources and startup at src/UsbOverIp.ClientAgent/App.xaml
- [ ] T056 [US2] Implement App.xaml.cs with DI container setup at src/UsbOverIp.ClientAgent/App.xaml.cs
- [ ] T057 [US2] Add manual server entry dialog to client UI at src/UsbOverIp.ClientAgent/Views/AddServerDialog.xaml

**Checkpoint**: Complete MVP - Server shares devices, Client attaches/detaches to them

---

## Phase 5: User Story 3 - Automatic Server Discovery (Priority: P2)

**Goal**: Servers broadcast presence, clients automatically discover them

**Independent Test**: Start server, start client on same network, verify server appears in client within 10 seconds without manual configuration

### Tests for User Story 3

- [ ] T058 [P] [US3] Create unit test for DiscoveryBroadcaster at tests/UsbOverIp.BackendService.Tests/Unit/DiscoveryBroadcasterTests.cs
- [ ] T059 [P] [US3] Create unit test for ServerDiscoveryService at tests/UsbOverIp.ClientAgent.Tests/Unit/ServerDiscoveryServiceTests.cs

### Server-side Implementation for User Story 3

- [ ] T060 [US3] Implement DiscoveryBroadcaster with UDP broadcast at src/UsbOverIp.BackendService/Discovery/DiscoveryBroadcaster.cs
- [ ] T061 [US3] Register DiscoveryBroadcaster as hosted service in Program.cs at src/UsbOverIp.BackendService/Program.cs
- [ ] T062 [US3] Add broadcast interval configuration at src/UsbOverIp.BackendService/appsettings.json

### Client-side Implementation for User Story 3

- [ ] T063 [US3] Implement ServerDiscoveryService with UDP listener at src/UsbOverIp.ClientAgent/Services/ServerDiscoveryService.cs
- [ ] T064 [US3] Add discovered servers to MainViewModel at src/UsbOverIp.ClientAgent/ViewModels/MainViewModel.cs
- [ ] T065 [US3] Add server online/offline status tracking (30s timeout) at src/UsbOverIp.ClientAgent/Services/ServerDiscoveryService.cs
- [ ] T066 [US3] Update UI to show server discovery status at src/UsbOverIp.ClientAgent/MainWindow.xaml

**Checkpoint**: Servers automatically discovered without manual configuration

---

## Phase 6: User Story 4 - System Tray Integration for Client (Priority: P3)

**Goal**: Client runs in system tray with context menu for quick access

**Independent Test**: Start client, minimize window, verify tray icon appears, right-click shows device menu, can attach/detach from tray menu

### Implementation for User Story 4

- [ ] T067 [US4] Add Hardcodet.NotifyIcon.Wpf NuGet package reference at src/UsbOverIp.ClientAgent/UsbOverIp.ClientAgent.csproj
- [ ] T068 [US4] Create tray icon resources (icons for connected/disconnected states) at src/UsbOverIp.ClientAgent/Resources/
- [ ] T069 [US4] Add TaskbarIcon to MainWindow with context menu at src/UsbOverIp.ClientAgent/MainWindow.xaml
- [ ] T070 [US4] Implement MinimizeToTrayBehavior at src/UsbOverIp.ClientAgent/Behaviors/MinimizeToTrayBehavior.cs
- [ ] T071 [US4] Add tray context menu with device list and attach/detach options at src/UsbOverIp.ClientAgent/MainWindow.xaml
- [ ] T072 [US4] Implement tray icon tooltip showing connection status at src/UsbOverIp.ClientAgent/ViewModels/MainViewModel.cs
- [ ] T073 [US4] Add single-instance enforcement using Mutex at src/UsbOverIp.ClientAgent/App.xaml.cs

**Checkpoint**: Client runs in system tray with full device management

---

## Phase 7: User Story 5 - Server Runs as Background Service (Priority: P3)

**Goal**: Server runs as Windows Service, starts automatically, manageable via named pipes

**Independent Test**: Install as service, reboot machine, verify service starts before login, devices become shareable

### Implementation for User Story 5

- [ ] T074 [US5] Add Microsoft.Extensions.Hosting.WindowsServices package at src/UsbOverIp.BackendService/UsbOverIp.BackendService.csproj
- [ ] T075 [US5] Configure UseWindowsService() in Program.cs at src/UsbOverIp.BackendService/Program.cs
- [ ] T076 [US5] Implement --install and --uninstall CLI arguments at src/UsbOverIp.BackendService/Program.cs
- [ ] T077 [US5] Add graceful shutdown handling with stoppingToken at src/UsbOverIp.BackendService/Worker.cs
- [ ] T078 [US5] Create device-config.json for auto-share rules at src/UsbOverIp.BackendService/device-config.json
- [ ] T079 [US5] Implement auto-share on startup based on config at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs
- [ ] T080 [US5] Add GET/PUT /api/config endpoints at src/UsbOverIp.BackendService/Api/ConfigEndpoints.cs

**Checkpoint**: Server runs as Windows Service with auto-start

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Settings persistence, edge cases, documentation

### Settings Persistence

- [ ] T081 [P] Create SettingsViewModel for client preferences at src/UsbOverIp.ClientAgent/ViewModels/SettingsViewModel.cs
- [ ] T082 [P] Create SettingsView.xaml UI at src/UsbOverIp.ClientAgent/Views/SettingsView.xaml
- [ ] T083 Implement AutoStartManager for Windows startup registry at src/UsbOverIp.ClientAgent/Services/AutoStartManager.cs
- [ ] T084 Persist client preferences to appsettings.json at src/UsbOverIp.ClientAgent/Services/PreferencesService.cs

### Edge Case Handling

- [ ] T085 Handle USB device hot-unplug on server (notify attached client) at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs
- [ ] T086 Handle network disconnection detection at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs
- [ ] T087 Handle concurrent attach requests (first wins, reject others) at src/UsbOverIp.BackendService/DeviceManager/DeviceManager.cs

### Documentation & Validation

- [ ] T088 [P] Validate quickstart.md instructions end-to-end
- [ ] T089 [P] Update CLAUDE.md with final implementation notes
- [ ] T090 Code review and cleanup across all projects

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - Server device sharing
- **User Story 2 (Phase 4)**: Depends on US1 server API endpoints - Client attachment
- **User Story 3 (Phase 5)**: Depends on Foundational - Can run in parallel with US1/US2
- **User Story 4 (Phase 6)**: Depends on US2 client app - Tray integration
- **User Story 5 (Phase 7)**: Depends on US1 server - Service installation
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

```
Phase 1 (Setup)
    │
    v
Phase 2 (Foundational) ─────────────────────────────────┐
    │                                                    │
    v                                                    v
Phase 3 (US1 - Server Sharing) ──────> Phase 5 (US3 - Discovery)
    │
    v
Phase 4 (US2 - Client Attach) ──────────────────────────┤
    │                                                    │
    v                                                    │
Phase 6 (US4 - Tray)                                     │
                                                         │
Phase 7 (US5 - Windows Service) <────────────────────────┘
    │
    v
Phase 8 (Polish)
```

### MVP Path (Minimal)

1. Phase 1: Setup
2. Phase 2: Foundational
3. Phase 3: User Story 1 (Server shares devices)
4. Phase 4: User Story 2 (Client attaches)
5. **STOP**: Complete MVP delivered

### Parallel Opportunities

Within Phase 1 (Setup):
- T002, T003, T004, T005, T006, T007 can all run in parallel

Within Phase 2 (Foundational):
- T011-T018 (all models) can run in parallel
- T019, T020 (interfaces) can run in parallel
- T025, T026 (tests) can run in parallel

Within Phase 3 (US1):
- T027, T028, T029 (tests) can run in parallel
- T035, T036, T037 (GET endpoints) can run in parallel after DeviceManager

Within Phase 5 (US3):
- T058, T059 (tests) can run in parallel
- Can start US3 as soon as Foundational is done (parallel with US1/US2)

---

## Parallel Example: Foundation Phase

```bash
# Launch all shared model tasks in parallel:
Task: "Create UsbDevice model at src/UsbOverIp.Shared/Models/UsbDevice.cs"
Task: "Create ServerInfo model at src/UsbOverIp.Shared/Models/ServerInfo.cs"
Task: "Create ServerAnnouncement model at src/UsbOverIp.Shared/Models/ServerAnnouncement.cs"
Task: "Create AttachmentInfo model at src/UsbOverIp.Shared/Models/AttachmentInfo.cs"
Task: "Create ServerConfiguration model at src/UsbOverIp.Shared/Models/ServerConfiguration.cs"
Task: "Create ClientPreferences model at src/UsbOverIp.Shared/Models/ClientPreferences.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (~10 tasks)
2. Complete Phase 2: Foundational (~16 tasks)
3. Complete Phase 3: User Story 1 (~15 tasks)
4. Complete Phase 4: User Story 2 (~16 tasks)
5. **STOP and VALIDATE**: Full device sharing and attachment works
6. Deploy/demo MVP

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 → Server can share devices (partial value)
3. Add US2 → Client can attach (MVP complete!)
4. Add US3 → Auto-discovery (improved UX)
5. Add US4 → Tray integration (polish)
6. Add US5 → Windows Service (production-ready)

---

## Summary

| Phase | User Story | Priority | Task Count |
|-------|------------|----------|------------|
| 1 | Setup | - | 10 |
| 2 | Foundational | - | 16 |
| 3 | US1 - Server Sharing | P1 | 15 |
| 4 | US2 - Client Attach | P1 | 16 |
| 5 | US3 - Discovery | P2 | 9 |
| 6 | US4 - Tray | P3 | 7 |
| 7 | US5 - Windows Service | P3 | 7 |
| 8 | Polish | - | 10 |
| **Total** | | | **90** |

### MVP Scope

- **Minimum**: Phase 1-4 (57 tasks) - Server sharing + Client attachment
- **Recommended**: Phase 1-5 (66 tasks) - Add auto-discovery
- **Full**: Phase 1-8 (90 tasks) - All features

---

## Notes

- [P] tasks = different files, no dependencies on other tasks in same batch
- [Story] label maps task to specific user story for traceability
- Tests are written first for each user story, should fail until implementation
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
