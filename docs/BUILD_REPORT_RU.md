# Сборка Classroom Prototype v0.1

29 сентября 2026 года создан настоящий Android APK для Meta Quest 3. Unity BuildPipeline вернул `Succeeded`; процесс Editor завершился с кодом `0`.

Локальный файл: `Builds/ClassroomPrototype-v0.1.apk`. APK и журналы сборки не включены в GitHub-репозиторий; повторная сборка описана ниже.

| Параметр | Значение |
|---|---|
| Имя приложения | VR Classroom Prototype |
| Package ID | `com.vrschool.classroomprototype` |
| Версия | `0.1.0`, versionCode `1` |
| Размер | 82,020,197 байт / 78.22 МиБ |
| Unity | `6000.6.3f1` (`45d8eee7de74`) |
| Meta XR Core / Interaction / Essentials | `207.0.0` |
| OpenXR / XR Plug-in Management | `1.18.0` / `4.7.0` |
| URP | `17.6.0` |
| Платформа | Android, ARM64, IL2CPP, Vulkan |
| Android API | минимум 32, target 34 |
| Сцена | `Assets/_Project/Scenes/Classroom.unity` |
| Тип сборки | Development Build, Android Debug certificate |

SHA-256 APK:

```text
3c8c18f45d43d3737da50f36faafaf95a802a99341729e9bcd25fb5150f931f8
```

## Что проверено

- C# скомпилирован в Editor и в Android IL2CPP; реальная сцена сохранена через Unity и Meta SDK Quick Actions.
- Проверены ссылки rig, locomotion, grab и teleport; отсутствуют missing scripts.
- Публичные проверки активных OpenXR features не вернули ошибок. Перед сборкой выполнены все **36 из 36 обязательных** проверок Meta Project Setup Tool.
- `apksigner verify --verbose --print-certs` завершился успешно: APK Signature Scheme v2, один Android Debug signer.
- `aapt dump badging` подтвердил package ID, версию, API32/34, GameActivity и единственную ABI `arm64-v8a`.
- Дамп собранного манифеста подтвердил обязательный `android.hardware.vr.headtracking`, VR intent category и `com.oculus.supportedDevices = quest3`.
- В APK присутствуют `libil2cpp.so`, `libunity.so`, `libOVRPlugin.so`, `libUnityOpenXR.so` и `libopenxr_loader.so`.
- Проверка CRC всех ZIP-записей прошла. `zipalign -c -P 16 4` завершился с кодом 0.

Полный журнал и исходный отчёт сохранены в `Builds/build.log`, `Builds/configure.log`, `Builds/meta-setup-report.json`; дамп манифеста — `Builds/AndroidManifest.dump.txt`. Папка Builds исключена из Git.

## Ограничения этой проверки

Установка и запуск на Quest 3, tracking, действия контроллеров, столкновения при движении, масштаб и производительность **ещё не проверены на устройстве**. Пользователь попросил пока подготовить только APK. В сцене тестовая комната и куб; реальный LiDAR-скан ещё не добавлен.

Это APK для локальной разработки. Перед публикацией потребуется отдельная release-сборка и собственная подпись. В предварительном отчёте Meta оставались 11 рекомендуемых и один необязательный пункт: среди них оптимизации производительности, настройки для hand tracking / Platform SDK и установка Simulator. Все обязательные пункты выполнены; рекомендации оцениваются при тестировании соответствующих возможностей.

## Повторить сборку

Открыть `UnityProject` в Unity 6000.6.3f1, выбрать Android и выполнить **Tools > VR Classroom > Build Development APK**. Результат перезаписывается по тому же пути. После изменений повторить **Validate Quest Project** и проверить Meta Project Setup Tool.

Управление и состав сцены — в [README](../README.md); аппаратные проверки — в [чек-листе](ACCEPTANCE_RU.md).
