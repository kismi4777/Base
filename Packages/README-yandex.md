# Yandex Games SDK

Скопируйте содержимое `com.bananaparty.yandexgames` в:

```
Packages/com.bananaparty.yandexgames/
```

В `manifest.json` укажите:

```json
"com.bananaparty.yandexgames": "file:com.bananaparty.yandexgames"
```

Не используйте абсолютный путь к папке Downloads — он ломается на других машинах и в CI.
