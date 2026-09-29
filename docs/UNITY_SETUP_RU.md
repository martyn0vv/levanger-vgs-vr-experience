# VR Classroom v0.1: настройка Unity и Quest 3

Результат этой инструкции — одна тестовая комната, контроллеры, захват куба, переключение teleport/smooth movement и snap turn, затем Android APK на Quest 3. Реальный скан заменит окружение на следующем шаге.

Руководство ручной настройки проверено по документации Unity/Meta и исходникам Meta Interaction SDK **207.0.0** 29 сентября 2026 года. На этом Mac теперь установлен Unity **6000.6.3f1**, и проект создан в `UnityProject`. Для текущего проекта используйте именно эту версию Editor; шаги создания ниже нужны только для ручного восстановления с нуля. Актуальный результат сборки описан в [README](../README.md).

## 1. Создать проект

1. Установить [Unity Hub](https://docs.unity.com/en-us/hub/install-hub).
2. Использовать **Unity 6000.6.3f1** — установленную и проверенную для этого проекта версию. Для восстановления на другом компьютере установить её через Unity Hub; точная версия записана в `UnityProject/ProjectSettings/ProjectVersion.txt`. [Выпуски Unity 6](https://unity.com/releases/unity-6).
3. Через `Manage > Add modules` установить `Android Build Support`, `Android SDK & NDK Tools`, `OpenJDK`.
4. `Projects > New project`: шаблон **Universal 3D**, имя `UnityProject`, расположение — корень этого репозитория. Итоговый каталог: `PROJECT X/UnityProject`.
5. Дождаться первого открытия и импорта. Не копировать стартовые скрипты до установки SDK из следующего раздела.

Meta указывает минимум `6000.0.66f2`, рекомендуя Unity 6.1 и новее. Universal 3D создаёт URP-проект. [Настройка среды Meta](https://developers.meta.com/horizon/documentation/unity/unity-project-setup/).

## 2. Установить пакеты

Используем один набор взаимодействий: **Meta Interaction SDK с OVR Comprehensive Interaction Rig**. OpenXR отвечает за связь с устройством, Meta SDK — за риг, grab и locomotion. Название `OVR` у префабов не означает, что нужно включать старый Oculus XR provider.

| Пакет | Package ID | Как получить |
|---|---|---|
| OpenXR Plugin | `com.unity.xr.openxr` | Unity Registry / XR Plug-in Management |
| XR Plug-in Management | `com.unity.xr.management` | Project Settings / зависимости |
| Meta XR Core SDK | `com.meta.xr.sdk.core` | Meta / Unity Asset Store |
| Meta XR Interaction SDK | `com.meta.xr.sdk.interaction.ovr` | Meta / Unity Asset Store |
| Meta XR Interaction SDK Essentials | `com.meta.xr.sdk.interaction` | Зависимость Interaction SDK |
| Input System | `com.unity.inputsystem` | Зависимость OpenXR |
| Universal RP | `com.unity.render-pipelines.universal` | Уже в шаблоне Universal 3D |

Для воспроизводимого старта выбираем согласованный набор Core + Interaction + Essentials **207.0.0**. Наличие этого выпуска и связи зависимостей проверены в [реестре пакетов Unity](https://packages.unity.com/com.meta.xr.sdk.interaction.ovr). Это проверка пакетов, а не подтверждение успешной сборки их сочетания с Editor.

1. В браузере открыть страницы **Meta XR Core SDK** и **Meta XR Interaction SDK** из [официального каталога Meta](https://developers.meta.com/horizon/documentation/unity/unity-sdks-overview/), войти в Unity ID и нажать `Add to My Assets`.
2. В Editor: `Window > Package Management > Package Manager > My Assets`. В некоторых версиях меню короче: `Window > Package Manager`.
3. Установить Core SDK, принять `Enable Meta XR Feature Set`, перезапустить Editor, если предложено. Установить Interaction SDK и дождаться установки зависимостей. Essentials отдельно искать и добавлять не требуется. [Установка Interaction SDK](https://developers.meta.com/horizon/documentation/unity/unity-isdk-setup/).
4. В `In Project` проверить все три Meta-пакета. Они должны принадлежать одному выпуску. Если Asset Store предлагает другой выпуск, сначала сверить его release notes и API локального переключателя, а не смешивать версии.
5. Версию OpenXR брать из совместимых выпущенных версий Package Manager для выбранного Editor. Meta указывает `1.15.1` как рекомендуемую базу; не понижать более новую совместимую версию, которую требует SDK. [XR providers](https://developers.meta.com/horizon/documentation/unity/unity-xr-plugin/).

`com.unity.xr.meta-openxr` — отдельный пакет Unity с расширениями Meta; для этой комнаты через Core SDK он не нужен. XR Interaction Toolkit, `XR Origin (VR)`, XRI Starter Assets и старый Oculus Integration сюда не добавляем: перемещением игрока и взаимодействиями управляет один Meta rig. Hand tracking, Platform SDK, Scene API и MRUK сейчас тоже не требуются.

После успешного первого APK сохранить в Git созданные Unity файлы `Packages/manifest.json`, `Packages/packages-lock.json` и `ProjectSettings/ProjectVersion.txt`. Они зафиксируют фактически проверенный набор.

## 3. Включить OpenXR и настройки Quest

1. `File > Build Profiles > Platforms > Meta Quest > Enable Platform` либо `Switch Platform`. Если Meta Quest отсутствует, выбрать `Android`. Если предлагается установить OpenXR — согласиться.
2. `Edit > Project Settings > XR Plug-in Management`: во вкладке **Android / Meta Quest** включить `OpenXR` и `Initialize XR on Startup`. Oculus provider оставить выключенным.
3. `XR Plug-in Management > OpenXR`, та же вкладка: в `Enabled Interaction Profiles` добавить **Oculus Touch Controller Profile**. Он поддерживает контроллеры семейства Quest. [Профили OpenXR](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.18/manual/features/metaquest.html).
4. В `OpenXR Feature Groups` включить **Meta XR**, проверить **Meta XR Feature**. Для предусмотренных SDK оптимизаций использовать **Meta XR Foveation** и **Meta XR Subsampled Layout**, следуя Project Setup Tool. Не включать весь список экспериментальных функций. [Функции Meta](https://developers.meta.com/horizon/documentation/unity/unity-project-setup/).
5. Открыть `Window > Meta > Meta XR SDK`, затем **Project Setup Tool**. Старый путь в меню: `Meta > Tools > Project Setup Tool`. Для Android выполнить `Fix All`, затем проверить и применить рекомендуемые настройки через `Apply All`.
6. `Edit > Project Settings > XR Plug-in Management > Project Validation`: проверить Android, устранить ошибки. Если конкретная версия дополнительно требует `Meta Quest Support`, включить его через предложенный `Fix`; не обходить ошибки validation редактированием YAML.

Начальные настройки APK в `Edit > Project Settings > Player`, вкладка Android, `Other Settings`:

| Поле | Значение |
|---|---|
| Company / Product Name | Свои имена школы и проекта |
| Package Name | Например, `com.school.vrclassroom` |
| Scripting Backend | `IL2CPP` |
| Target Architectures | `ARM64`; снять `ARMv7` |
| Minimum API Level | Android API **32**, минимум выбранного Meta Core SDK 207 |
| Target API Level | Android API **34**, установленный вместе с Android Build Support |
| Active Input Handling | `Input System Package (New)`; применить запрос перезапуска |

IL2CPP, ARM64 и минимум API подтверждены [настройками сборки Meta](https://developers.meta.com/horizon/documentation/unity/unity-prepare-for-publish/). Итоговые graphics/API настройки после Project Setup Tool сохранить; в OpenXR использовать `Multi-View` / `Single Pass Instanced / Multi-view`, а не Multi-Pass. [Параметры рендеринга](https://developers.meta.com/horizon/documentation/unity/unity-openxr-settings/).

## 4. Создать тестовую комнату

1. Скопировать папку `StarterAssets/_Project` и соседний файл `_Project.meta` в `UnityProject/Assets`, сохраняя вложенные `.meta`.
2. Дождаться компиляции; Console должна быть без красных ошибок.
3. Нажать **`Tools > VR Classroom > Create Test Room`**.
4. Открыть созданную сцену `Assets/_Project/Scenes/Classroom.unity`. Если рядом открыта SampleScene, закрыть её в Hierarchy. Правой кнопкой по заголовку Classroom → `Set Active Scene`, если она ещё не активна. Quick Actions ниже должны создавать объекты именно в Classroom.

Генератор создаёт комнату 8 × 6 м, простой пол и стены с BoxCollider, стол, куб с Rigidbody, свет, метровую рейку и пустые узлы для будущего контента. У пола верхняя поверхность `Y = 0`; `1 unit = 1 м`. Настоящего скана, XR-рига и взаимодействий в сгенерированной комнате ещё нет. Повторный вызов открывает существующую Classroom, сохраняя её ручные изменения.

Назначить слои через поле `Layer > Add Layer` в Inspector:

| Объект | Physics Layer |
|---|---|
| `Classroom/Environment/Floor` | Новый слой `Walkable` |
| `Classroom/Environment/Walls/*`, `StaticFurniture/TestTable`, `ScaleReference` | Новый слой `WorldSolid` |
| `Classroom/InteractiveObjects/TestCube` | Новый слой `Interactable` |

Слои нужны для разных задач: `WorldSolid + Walkable` участвуют в столкновениях игрока и блокируют дугу, а куб и служебные цели телепорта исключены из маски игрока. Телепорт ниже разрешается только в три заданные точки. Builder сам не изменяет Project Settings и оставляет исходный слой Default.

## 5. Добавить один XR rig

1. Удалить `Main Camera`, если она есть в этой сцене.
2. Правой кнопкой в Hierarchy: **`Interaction SDK > Add OVR Comprehensive Interaction Rig`**.
3. В Project создать папки `Assets/_Project/Prefabs/XR`. В мастере: `Fix All`, включить `Generate As Editable Copy`; путь копии — `Assets/_Project/Prefabs/XR/ComprehensiveInteractors.prefab`, затем `Create`.
4. Полученный `OVRCameraRig` поместить в пустой узел `XR`. У рига выставить local Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`. Сам `XR` уже расположен в `(0, 0, -1.5)`.
5. У `OVRCameraRig > OVR Manager` выбрать `Tracking Origin Type = Floor Level`. В `Quest Features > General > Hand Tracking Support` выбрать `Controllers Only` для v0.1.
6. Сохранить сцену. Убедиться, что в ней ровно один `OVRCameraRig`, один `OVRComprehensiveInteractionRig` и один `Locomotor`.

Quick Action настраивает ссылки между ригом, контроллерами, визуальными моделями и locomotion; сохраняйте созданную иерархию. [Создание рига](https://developers.meta.com/horizon/documentation/unity/unity-isdk-add-comprehensive-interaction-rig/).

## 6. Настроить движение и столкновения

Найти внутри рига `Locomotor`:

- `Locomotor > PlayerController`: у **Interaction SDK CharacterController** в `Layer Mask` включить `Walkable` и `WorldSolid`. Это компонент Meta, а не встроенный Unity CharacterController.
- `Locomotor > LocomotionSettings > Moving`: `Controller Movement = Teleport`.
- `Locomotor > LocomotionSettings > Turning`: `Controller Turn = Snap`, `Rotation Snap Angle = 30`.

Пол под стартовой позицией должен иметь включённый collider с `Is Trigger = false`. Без пола в Layer Mask `FirstPersonLocomotor` прекращает slide-перемещение: при этом поворот и телепорт могут продолжать работать. [Grounding и collision](https://developers.meta.com/horizon/documentation/unity/unity-isdk-setup-slide-locomotion/).

Под обеими ветками `ComprehensiveInteractorsLeft` и `ComprehensiveInteractorsRight` находится `Interactors > Controller > LocomotionControllerInteractorGroup`. Там лежат `TeleportControllerInteractor`, `ControllerTurnerInteractor`, `ControllerSlideInteractor`, `ControllerLocomotionSlideActions`. **Не переключать их вручную:** MovingSetting управляет активностью режима. Через дочерние `Jump`, `Run`, `Crouch` можно отключить ненужные для экскурсии действия, сохранив ходьбу и поворот.

В префабе SDK 207 `PlayerController` содержит также **FirstPersonLocomotor** с назначенным CharacterController. В `LocomotionEventsConnection` штатного Locomotor этот FirstPersonLocomotor указан среди обработчиков. После Quick Action ссылки на Player Origin и Player Eyes должны быть заполнены. Не заменять эту цепочку отдельным PlayerLocomotor: у него нет такой проверки столкновений.

Подключить скрипт выбора режима из набора:

1. В `Managers` создать пустой объект `LocomotionControls`.
2. `Add Component > ClassroomLocomotionControls`.
3. На поле `Moving Setting` перетащить объект `Moving` из рига.
4. На поле `Turning Setting` перетащить объект `Turning`.
5. Скрипт применяет начальный teleport и snap `30°` после запуска. В шлеме отпустить оба стика и нажать **B на правом контроллере**: teleport ↔ smooth movement. Snap turn сохраняется в обоих режимах; выбор между запусками пока не сохраняется.

Скрипт меняет настройки штатного Meta locomotion, а положение XR Rig продолжает вычислять SDK. Не добавлять одновременно меню настроек из `LocomotionExamples`: оба интерфейса будут управлять одними параметрами. Исходники `MovingSetting`, `TurningSetting` и префаб `Locomotor` сверены с официальным SDK 207.0.0.

## 7. Добавить три точки телепорта

В v0.1 телепортируемся в заранее выбранные свободные точки. Блокировка дуги не проверяет, поместится ли капсула игрока в произвольной точке назначения. Поэтому свободный телепорт по всему полу добавим позднее вместе с проверкой свободного пространства.

В `TeleportTargets` создать три пустых дочерних объекта; Scale `(1,1,1)`, Rotation `(0,0,0)`, слой `Default`:

| Объект | Position, метры |
|---|---|
| `Start` | `(0, 0, -1.5)` |
| `FrontOfTable` | `(1.5, 0, 0)` |
| `LeftSide` | `(-2, 0, 1)` |

Для каждого объекта:

1. Правой кнопкой: **`Interaction SDK > Add Teleport Interaction`**.
2. `Interactable Type = Hotspot`, `Hotspot Snap = Snap Position`, `Allows Teleport` включён. Поле `Snap Head Transform` оставить пустым: цель задаёт положение ног.
3. В `Add Required Interactor(s)` включить controllers. `Fix All` заполнит `Target Transform` этим объектом и создаст дочерний `Teleport Collider` с trigger collider. Нажать `Create`.
4. Если мастер создал SphereCollider, удалить его и добавить BoxCollider; переназначить поле Collider у созданного ColliderSurface на новый компонент. У `Teleport Collider > BoxCollider` задать Size `(0.6, 0.2, 0.6)`, Center `(0, 0.1, 0)`, `Is Trigger = true`. У дочернего Transform — local Position `(0,0,0)`, Scale `(1,1,1)`. Автоматический `QuestClassroomSetup` сразу создаёт BoxCollider до Quick Action.
5. У созданного `ISDK_TeleportInteraction > TeleportInteractable` проверить `Target Point` = соответствующий маркер, `Eye Level` выключен, `Face Target Direction` выключен. Целевая позиция остаётся на высоте пола, хотя объём выбора немного выступает над ним.
6. Чтобы точка была видна до наведения, добавить к маркеру дочерний `3D Object > Cylinder`: local Position `(0, 0.006, 0)`, Rotation `(0,0,0)`, Scale `(0.3, 0.005, 0.3)`. Удалить у цилиндра его CapsuleCollider и назначить цветной материал `Assets/_Project/Materials/TestCube.mat`. Для наведения остаётся отдельный trigger из шага 4.

[Hotspot Quick Action](https://developers.meta.com/horizon/documentation/unity/unity-isdk-create-teleport-hotspot/). Имена полей и поведение пустого Snap Head Transform также проверены в `TeleportWizard.cs` SDK 207.

Затем в `TeleportTargets` создать `WorldBlocker`. Через тот же `Add Teleport Interaction` выбрать `Physics Layer`, маску **`Walkable + WorldSolid`**, **`Allows Teleport` выключить**, `Fix All > Create`. Так пол, стены и стол останавливают дугу, а находящиеся над полом trigger-объёмы точек остаются целями. Не создавать разрешающий телепорт на весь слой Walkable. [Блокирование через physics layer](https://developers.meta.com/horizon/documentation/unity/unity-isdk-teleport-quick-action/).

В режиме teleport: наклонить стик вперёд, навести дугу на одну из трёх точек, отпустить. Наклон стика в сторону выполняет snap turn. В режиме smooth левый стик перемещает игрока, правый поворачивает. Проверить все три позиции и повторный переход между ними. При перемещении мебели проверить свободное место вокруг каждой точки заново.

## 8. Сделать куб grabbable

1. Выбрать `Classroom/InteractiveObjects/TestCube`.
2. Проверить `BoxCollider`, `Rigidbody`, включённую gravity. Не добавлять второй Rigidbody.
3. Правой кнопкой по кубу: **`Interaction SDK > Add Grab Interaction`**.
4. В `Add Required Interactor(s)` выбрать вариант с **Controllers**. Нажать `Fix All > Create`.
5. Проверить, что Quick Action использовал существующие collider и Rigidbody, а Console не сообщает о пустых ссылках.

В шлеме поднести контроллер к кубу, зажать боковой grip, поднять, перенести и отпустить. Куб должен падать на стол или пол. Его масштаб и масса задаются на Rigidbody/Transform, а захватом управляет Meta SDK. [Grab Quick Action](https://developers.meta.com/horizon/documentation/unity/unity-isdk-grab-quick-action/), [создание grabbable](https://developers.meta.com/horizon/documentation/unity/unity-isdk-create-grabbable-object/).

## 9. Кнопки и UI — следующий небольшой шаг

Для первого APK достаточно кнопки B. Когда понадобится экран выбора режима, создать `Canvas` в режиме **World Space**, поместить его перед пользователем и добавить две обычные Unity UI Button. На Canvas вызвать `Interaction SDK > Add Ray Interaction to Canvas`, выбрать controllers, затем `Fix All > Create`.

В `On Click()` первой кнопки указать `LocomotionControls > ClassroomLocomotionControls.UseTeleport()`, второй — `UseSmoothMovement()`. Это подключает UI к тому же переключателю. Требуемый `PointableCanvasModule` должен остаться один на сцену; Quick Action помогает настроить его и ray interactable. [Ray Canvas](https://developers.meta.com/horizon/documentation/unity/unity-isdk-ray-quick-action/), [UI в Interaction SDK](https://developers.meta.com/horizon/documentation/unity/unity-isdk-getting-started/).

## 10. Собрать и запустить на Quest 3 с Mac

1. Включить Developer Mode для Quest через Meta Horizon mobile app: значок шлема → свой шлем → `Headset Settings > Developer Mode`. Нужен настроенный developer account.
2. Подключить шлем USB-C **кабелем с передачей данных**. Внутри шлема разрешить USB debugging для этого компьютера.
3. Дополнительный USB-драйвер на macOS не нужен. При необходимости проверить соединение командой `adb devices`: у устройства должен быть статус `device`, а не `unauthorized`. ADB входит в Android SDK, установленный Hub. Его каталог можно уточнить в `Unity > Settings/Preferences > External Tools > Android SDK`.

[Подключение устройства и ADB](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/).

4. В Unity открыть `File > Build Profiles`. Выбрать активный `Meta Quest` / Android профиль.
5. `Open Scene List > Add Open Scenes`; оставить только `Classroom`, включённую и первой в списке.
6. `Run Device`: выбрать Quest 3; если отсутствует — `Refresh`.
7. Включить `Development Build` для диагностики. `Build App Bundle` оставить выключенным: нужен APK.
8. `Build And Run`; сохранить результат, например, в `Builds/ClassroomPrototype-v0.1.apk` вне Assets. Дождаться установки и запуска.

[Профиль, Scene List и запуск](https://developers.meta.com/horizon/documentation/unity/unity-build/).

Meta Horizon Link работает только на Windows. На Mac проверять настоящий standalone APK через Build And Run; Meta XR Simulator можно добавить позднее для более быстрых проверок, но он не подтверждает производительность на Quest. [Ограничения Link](https://developers.meta.com/horizon/documentation/unity/unity-link/).

## Проверить результат

Пройти [критерии приёмки v0.1](ACCEPTANCE_RU.md) на самом Quest 3. Зафиксировать фактические версии Unity/SDK, успешный APK и результаты. Только после этого переходить к [импорту скана](SCAN_PIPELINE_RU.md).

Если изображение есть, а движения нет, сначала проверять пол и Layer Mask. Если контроллеры не видны — один rig, Controllers Only, Meta XR Feature и Android interaction profile. При ошибках классов Meta проверить установку всех трёх согласованных SDK-пакетов; не исправлять это установкой XRI или второго locomotion rig.
