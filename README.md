# HookChime

<img src="assets/icon/hookchime.png" alt="HookChime bell icon" width="96" align="right">

A cross-platform (Windows / macOS / Linux) CLI that shows a desktop notification when
your AI coding agent (Claude Code, Gemini CLI, GitHub Copilot CLI, Codex) finishes a
task, by hooking into each agent's built-in hook/notify mechanism.

Written in .NET, distributed as a self-contained executable — no runtime install
required.

> HookChime is a fresh, independent implementation inspired by
> [shanselman/toasty](https://github.com/shanselman/toasty), a Windows-only C++/WinRT
> tool with the same idea. It shares no code with that project; it's cross-platform
> from the ground up.

## Quick start

```sh
hookchime "Hello World" -t "HookChime"
```

## Usage

```
hookchime <message> [options]
hookchime --install [agent]
hookchime --uninstall
hookchime --status

Options:
  -t, --title <text>   Set notification title (default: "Notification")
  --app <name>         Use an AI CLI preset (claude, copilot, gemini, codex, cursor)
  -i, --icon <path>    Use a custom icon (Windows and Linux only — see below)
  -v, --version        Show version and exit
  -h, --help           Show this help
  --install [agent]    Install hooks for AI CLI agents (claude, gemini, copilot, codex, or all)
  --uninstall          Remove hooks from all AI CLI agents
  --status             Show installation status
  --dry-run            Show what would happen without executing side effects
  --debug              Print agent auto-detection diagnostics to stderr
```

`hookchime` also auto-detects which agent invoked it (by walking the parent-process
chain) and applies the matching title automatically — no `--app` flag needed for the
supported agents.

## Push notifications (ntfy)

Set `HOOKCHIME_NTFY_TOPIC` (and optionally `HOOKCHIME_NTFY_SERVER`, default `ntfy.sh`)
to also push every notification to your phone via [ntfy.sh](https://ntfy.sh). Fire and
forget, 5s timeout, never blocks or fails the local notification.

## Platform capability matrix

Desktop notification models are genuinely different across the three OSes. HookChime
does not pretend otherwise:

| Feature | Windows | Linux | macOS |
|---|---|---|---|
| Basic toast (title/message) | Native WinRT toast | `notify-send` (libnotify) | `osascript display notification` (built-in) |
| Custom icon | Yes | Yes | **No** — the macOS notification API used here has no custom-icon support without a signed app bundle |
| Auto-detect calling agent | Yes | Yes | Yes |
| Click-to-focus the terminal | Not yet implemented | Not yet implemented | Not yet implemented |

Click-to-focus (jumping back to the terminal that triggered the notification) is
tracked as future work. It's straightforward on Windows (protocol-activation, the way
the original C++ toasty does it) but fundamentally harder on Linux (needs a short-lived
detached listener process waiting on a D-Bus signal; unreliable on Wayland by design)
and macOS (no click-callback at all without an extra dependency like
`terminal-notifier` or a signed app bundle). Rather than ship a half-working version of
this, it's left out of v1.

## Supported AI agents

| Agent | Hook type | Config location |
|-------|-----------|------------------|
| Claude Code | `Stop` | `~/.claude/settings.json` |
| Gemini CLI | `AfterAgent` | `~/.gemini/settings.json` |
| GitHub Copilot CLI | `sessionEnd` | `.github/hooks/hookchime.json` (repo-relative) |
| OpenAI Codex CLI | `notify` | `~/.codex/config.toml` |

`--install` backs up any existing config file (`.bak`) before writing, and only ever
adds/removes its own hook entry — everything else in the file is left untouched.

## Building

Requires the .NET 10 SDK.

```sh
dotnet build HookChime.slnx
```

## Testing

```sh
dotnet test HookChime.slnx
```

Unit tests cover the hook installers (including comment/formatting preservation in
Codex's TOML config), version comparison, and run against temp-directory fixtures —
never your real agent configs.

Toast rendering, `notify-send` delivery, and `osascript` notifications can't be
asserted in CI without a display; `--dry-run` prints what each platform's notifier
would do, and real rendering should be spot-checked manually per OS before a release.

## Distribution

Self-contained, single-file builds per OS/architecture (`win-x64`, `osx-x64`,
`osx-arm64`, `linux-x64`, `linux-arm64`) are published from CI — no .NET runtime
install required on the target machine.

## Icon

The bell icon (`assets/icon/`) is built from the "bell-ringing" glyph in
[Tabler Icons](https://tabler.io/icons) (MIT) — see [NOTICE.md](NOTICE.md).

## License

MIT (see [NOTICE.md](NOTICE.md) for third-party notices).
