# Instructions for AI agents

This repository contains everything needed to create plugins for the ManicTime client.

**When the user asks to create, generate, or test a ManicTime plugin** — e.g. "get the repository
name from Fork", "track which file is open in Lightroom", "show the current conversation from the
Claude app on my timeline" — follow the instructions in
[skills/manictime-plugin/SKILL.md](skills/manictime-plugin/SKILL.md). It contains the full
workflow (probe the app, generate a plugin from a template, test it live, install it into
ManicTime), reference documentation for the plugin contract and packaging, probe tools for
Windows (COM, UI Automation) and macOS (accessibility), and working plugin templates.

Repository layout:

- `libs/` — the ManicTime contract assemblies plugins compile against (no NuGet package exists)
- `source/tracker-plugin/ManicTimePluginTester.Cli/` — command-line harness that loads a plugin
  package the way ManicTime does (a close mirror; the installed client is the final word) and
  prints what it returns for the active window; use it to verify every generated plugin before
  installing
- `source/tracker-plugin/Plugins.Notepad`, `Plugins.Outlook` — working document tracker examples
- `source/tag-plugin/`, `source/timeline-plugin/` — examples for the other plugin types
  (Windows-only, debugged by attaching to ManicTimeClient.exe — not covered by the skill)
- `skills/manictime-plugin/` — the plugin-generation skill (works with any coding agent)
- `smoke-test.sh` — builds the tester, probe and templates and exercises the tester's modes;
  run it after changing any of them to confirm nothing is broken
