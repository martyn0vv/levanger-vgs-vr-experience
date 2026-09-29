# Levanger VGS VR Experience

VR-копия школы для Meta Quest 3. Первый этап — Classroom Prototype v0.1: минимальная тестовая комната, в которую затем можно импортировать подготовленный LiDAR-скан класса.

Рабочий Unity-проект находится в **`UnityProject`**. Он создан в установленном Unity **6000.6.3f1** из Universal 3D; сцена `Classroom` уже сгенерирована и настроена настоящими API редактора и Meta SDK.

**Статус:** Android APK успешно собран 29 сентября 2026 года: `Builds/ClassroomPrototype-v0.1.apk` — 78,22 МиБ, ARM64, Development Build. APK сохранён локально и не включён в репозиторий. Подпись и VR-манифест проверены; все 36 обязательных проверок Meta пройдены. Проверка на Quest 3 отложена по просьбе пользователя. Скан класса пока не требуется. [Отчёт о сборке](docs/BUILD_REPORT_RU.md).

## Открыть проект

В Unity Hub выберите **Projects > Add > Add project from disk** и папку `UnityProject` этого репозитория. Используйте Editor **6000.6.3f1**, указанный в `ProjectSettings/ProjectVersion.txt`. Откройте `Assets/_Project/Scenes/Classroom.unity`.

Повторно копировать `StarterAssets` или вручную добавлять второй XR rig не нужно. `StarterAssets` — предыдущая заготовка; рабочие исходники теперь только в `UnityProject/Assets/_Project`.

## Что находится в сцене

- Комната 8 × 6 м, пол на `y=0`, четыре стены и подиум с простыми BoxCollider; метровый ориентир для проверки масштаба.
- Один `OVRCameraRig` с Floor Level tracking и `OVRComprehensiveInteractionRig`.
- Quest Touch controllers, штатные Meta grab и locomotion, физический тестовый куб 20 см.
- Три точки teleport с видимыми маркерами; остальной пол и стены блокируют дугу.
- Плавное движение с Meta CharacterController / FirstPersonLocomotor; отдельные слои пола, препятствий и предметов.
- Стартовый режим teleport; **B** на правом контроллере при отпущенных стиках переключает teleport / smooth movement. Snap turn — **30°** в обоих режимах. Grab — боковой **grip**.

Сцена и ссылки проверены в редакторе. Работа контроллеров, удобство движения, масштаб в шлеме и производительность ещё требуют [проверки на Quest](docs/ACCEPTANCE_RU.md).

## Сборка

Android Build Support, SDK/NDK и OpenJDK установлены. Активная платформа — Android, backend — IL2CPP, архитектура — ARM64, графика — Vulkan, XR — OpenXR с Meta XR Feature и Oculus Touch profile. Минимум Android API32, target API34. Mobile URP: HDR выключен, MSAA 4×.

Для повторной сборки в Unity: **Tools > VR Classroom > Build Development APK**. Результат сохраняется в `Builds/ClassroomPrototype-v0.1.apk` в корне репозитория. APK предназначен для локальной разработки и подписывается debug-ключом.

Версии пакетов зафиксированы в `UnityProject/Packages/manifest.json` и `packages-lock.json`: Meta XR Core / Interaction / Essentials **207.0.0**, OpenXR **1.18.0**, XR Plug-in Management **4.7.0**, URP **17.6.0**.

## Структура и код

```text
UnityProject/
  Assets/_Project/
    Scenes/Classroom.unity
    Materials/
    Scripts/ClassroomLocomotionControls.cs
    Editor/ClassroomSceneBuilder.cs
    Editor/QuestClassroomSetup.cs
    Editor/QuestProjectSettings.cs
  Assets/InteractionSDK/ComprehensiveInteractors.prefab
  Assets/XR/
  Packages/
  ProjectSettings/
Builds/                       # локальные APK, вне Git
SourceScans/                  # оригиналы сканов, вне Git
```

Единственный собственный runtime-скрипт — `ClassroomLocomotionControls`: он переключает настройки SDK. Остальные три скрипта — инструменты редактора для комнаты, Meta-сцены, проверки настроек и сборки; в APK они не входят. Собственная иерархия InteractableObject пока не нужна.

`Classroom/Environment/ScanRoot` оставлен для подготовленного скана. Родители окружения и XR имеют scale `(1,1,1)`, **1 Unity unit = 1 м**. В v0.2 добавим отдельные prefab предметов; сложные взаимодействия и загрузку зон — после работающего класса.

[Ручная настройка и восстановление](docs/UNITY_SETUP_RU.md) · [Проверка на Quest](docs/ACCEPTANCE_RU.md) · [Blender / scan pipeline](docs/SCAN_PIPELINE_RU.md)

В Git добавляйте Assets с `.meta`, Packages и ProjectSettings. Library, UserSettings, SDK-кеши и APK исключены через `.gitignore`; исходные сканы требуют отдельного резервного хранения.
