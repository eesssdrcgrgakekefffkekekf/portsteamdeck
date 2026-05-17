# NebulaAuth - Steam Deck Port

Linux-порт [NebulaAuth Steam Desktop Authenticator](https://github.com/achiez/NebulaAuth-Steam-Desktop-Authenticator-by-Achies) для **Steam Deck** (SteamOS / Arch Linux).

## Описание

NebulaAuth Linux — полноценный порт Windows-версии Steam Guard Authenticator на Linux с использованием Avalonia UI. Приложение оптимизировано для тачскрина Steam Deck с увеличенными элементами интерфейса.

## Возможности

- Генерация 2FA кодов Steam Guard
- Управление .maFile аккаунтами
- Подтверждение трейдов и лотов маркета
- Автоматическое подтверждение
- Поддержка прокси (HTTP, SOCKS4, SOCKS5)
- Группировка аккаунтов
- Интерфейс оптимизирован под тачскрин Steam Deck
- Поддержка геймпада (большие кнопки и элементы)

## Установка на Steam Deck

### Способ 1: Готовый бинарник (рекомендуется)

```bash
# Скачать релиз и распаковать
tar -xzf nebulaauth-steamdeck-1.8.4.tar.gz
cd NebulaAuth-SteamDeck

# Установить
chmod +x scripts/install-steamdeck.sh
./scripts/install-steamdeck.sh
```

### Способ 2: Сборка из исходников

```bash
# Установить зависимости (один раз)
chmod +x scripts/install-deps-steamdeck.sh
./scripts/install-deps-steamdeck.sh

# Собрать
chmod +x scripts/build.sh
./scripts/build.sh

# Установить
./scripts/install-steamdeck.sh
```

### Способ 3: Arch Linux пакет

```bash
cd packaging
makepkg -si
```

## Использование в Game Mode

После установки:
1. Переключитесь в Desktop Mode
2. Откройте Steam → Игры → Добавить стороннюю игру
3. Найдите и добавьте NebulaAuth
4. Приложение появится в библиотеке Steam в Game Mode

## maFiles

Поместите ваши `.maFile` файлы в директорию `~/.local/share/NebulaAuth/maFiles/`  
Или используйте импорт через интерфейс (drag & drop).

## Структура проекта

```
NebulaAuth-SteamDeck/
├── src/NebulaAuth.Linux/        # Исходный код
│   ├── Models/                  # Модели данных (Mafile, Settings, etc.)
│   ├── Services/                # Бизнес-логика (SteamGuard, Confirmations, etc.)
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Views/                   # Avalonia UI (AXAML)
│   └── Assets/                  # Иконки и ресурсы
├── scripts/                     # Скрипты сборки и установки
├── packaging/                   # PKGBUILD для Arch Linux
└── README.md
```

## Технологии

- **.NET 8.0** — runtime
- **Avalonia UI 11** — кроссплатформенный UI (замена WPF)
- **CommunityToolkit.Mvvm** — MVVM фреймворк
- **ReactiveUI** — реактивные расширения
- **Newtonsoft.Json** — работа с JSON
- **NLog** — логирование

## Отличия от Windows-версии

| Функция | Windows | Linux (Steam Deck) |
|---------|---------|-------------------|
| UI Framework | WPF | Avalonia |
| WebView | WebView2 | Не используется |
| Auto-update | AutoUpdater.NET | Ручное/через пакетный менеджер |
| Clipboard | WinAPI | xclip / wl-copy |
| Themes | MaterialDesign WPF | Fluent Theme (Avalonia) |

## Требования

- .NET 8.0 Runtime (включён в self-contained сборку)
- Steam Deck (SteamOS 3.x) или любой Arch Linux
- X11 или Wayland

## Лицензия

Основано на NebulaAuth by Achies. Коммерческое использование запрещено.
При распространении модифицированного кода указывайте оригинальное авторство.
