#!/usr/bin/env bash
# Smoke test for the plugin tester and the manictime-plugin skill.
#
# Builds everything that must build (tester, probe, both plugin templates) and exercises the
# tester's modes, asserting on exit codes and key output lines. Run it after changing the tester,
# the templates or the probe — and after generating a plugin, to check your own package.
#
#   ./smoke-test.sh                 # build + run every check
#   ./smoke-test.sh --no-build      # skip those builds (a small test package is still built)
#
# Exits 0 when everything passed, 1 on the first failure summary.
set -u

cd "$(dirname "$0")"
ROOT="$PWD"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

BUILD=1
case "${1:-}" in
    "") ;;
    --no-build) BUILD=0 ;;
    *) echo "usage: $0 [--no-build]" >&2; exit 2 ;;
esac

PASS=0
FAIL=0

pass() { PASS=$((PASS + 1)); printf '  ok   %s\n' "$1"; }
fail() { FAIL=$((FAIL + 1)); printf '  FAIL %s\n' "$1"; [ -n "${2:-}" ] && printf '       %s\n' "$2"; }

# check <name> <expected-exit> <grep-pattern|-> -- <command...>
# Every command runs under a hard timeout: a regression that makes the tester loop forever must
# fail this script, not hang it.
check() {
    local name=$1 wantExit=$2 pattern=$3
    shift 4 # name, exit, pattern, --
    local out status
    out=$(run_with_timeout 30 "$@" 2>&1)
    status=$?
    if [ "$status" = 124 ]; then
        fail "$name" "timed out — the tester did not exit"
        return
    fi
    if [ "$status" != "$wantExit" ]; then
        fail "$name" "exit $status, expected $wantExit"
        return
    fi
    if [ "$pattern" != "-" ] && ! printf '%s' "$out" | grep -qE "$pattern"; then
        fail "$name" "output did not match: $pattern"
        return
    fi
    pass "$name"
}

# Portable timeout: macOS ships no `timeout`. Runs the command in the background, kills it
# after N seconds and reports 124, like GNU timeout does.
run_with_timeout() {
    local seconds=$1
    shift
    "$@" &
    local cmd=$!
    # The watchdog must not inherit stdout/stderr: a caller using $(...) or a pipe would wait
    # for it to exit too, making every call take the full timeout.
    ( sleep "$seconds"; kill -9 $cmd 2>/dev/null ) >/dev/null 2>&1 &
    local killer=$!
    wait $cmd 2>/dev/null
    local status=$?
    kill $killer 2>/dev/null
    # 137 = killed by SIGKILL, i.e. the timeout fired.
    [ $status = 137 ] && return 124
    return $status
}

build() {
    local name=$1 project=$2
    local out
    out=$(dotnet build "$project" -v q --nologo 2>&1)
    if [ $? -ne 0 ]; then
        fail "build $name" "$(printf '%s' "$out" | grep -E 'error' | head -3)"
    elif printf '%s' "$out" | grep -q "warning"; then
        fail "build $name" "$(printf '%s' "$out" | grep -E 'warning' | head -3)"
    else
        pass "build $name (no warnings)"
    fi
}

echo "== builds =="
TESTER="$ROOT/source/tracker-plugin/ManicTimePluginTester.Cli"
if [ "$BUILD" = 1 ]; then
    build "tester" "$TESTER"
    build "probe-ax" "$ROOT/skills/manictime-plugin/scripts/probe-ax"

    # The templates live outside any project: build them the way the skill tells an agent to,
    # at <repo root>/myplugins/<App>Plugin/, so the template's HintPath depth is exercised too.
    for template in TitleRegexPlugin AxTreePlugin; do
        proj="$WORK/myplugins/$template"
        mkdir -p "$proj"
        cp "$ROOT/skills/manictime-plugin/templates/$template.cs" "$proj/"
        cp "$ROOT/skills/manictime-plugin/templates/PluginTemplate.csproj" "$proj/$template.csproj"
        ln -sfn "$ROOT/libs" "$WORK/libs"
        build "template $template" "$proj"
        if ls "$proj"/bin/Debug/net10.0/*.dll 2>/dev/null | xargs -n1 basename 2>/dev/null \
            | grep -qiE '^(Finkit\.ManicTime|ManicTime\.)'; then
            fail "template $template output excludes host dlls" "host assemblies found in build output"
        else
            pass "template $template output excludes host dlls"
        fi
    done
else
    echo "  (skipped)"
fi

CLI=(dotnet "$TESTER/bin/Debug/ManicTimePluginTester.Cli.dll")
if [ ! -f "$TESTER/bin/Debug/ManicTimePluginTester.Cli.dll" ]; then
    echo "tester is not built — run without --no-build"
    exit 1
fi

# A minimal valid package built from the title template, used by the package-mode checks.
PKG_SRC="$WORK/pkgsrc"
mkdir -p "$PKG_SRC"
cp "$ROOT/skills/manictime-plugin/templates/TitleRegexPlugin.cs" "$PKG_SRC/"
cp "$ROOT/skills/manictime-plugin/templates/PluginTemplate.csproj" "$PKG_SRC/Smoke.csproj"
sed -i.bak 's#\.\.\\\.\.\\libs#'"$ROOT"'/libs#g; s#<AssemblyName>AppPlugin#<AssemblyName>SmokePlugin#' "$PKG_SRC/Smoke.csproj"
if ! dotnet build "$PKG_SRC" -v q --nologo >"$WORK/pkgbuild.log" 2>&1; then
    fail "build smoke package" "$(grep -E 'error' "$WORK/pkgbuild.log" | head -3)"
fi
PKG="$WORK/Packages/Custom.DocumentTracker.Smoke"
mkdir -p "$PKG/Lib"
cp "$PKG_SRC"/bin/Debug/net10.0/*.dll "$PKG/Lib/" 2>/dev/null
cat > "$PKG/PluginSpec.json" <<'JSON'
{
  "Id": "Custom.DocumentTracker.Smoke",
  "Version": "1.0.0.0",
  "Type": "DocumentTracker",
  "AssemblyName": "SmokePlugin.dll",
  "Name": "Smoke test plugin",
  "MinHostVersion": "2025.2"
}
JSON

# A process to watch: this script's own shell is always alive and has a pid.
PID=$$

echo
echo "== tester modes =="
check "no arguments prints usage"            1 "Usage:"                  -- "${CLI[@]}"
check "unknown path is reported"             1 "NOT FOUND"               -- "${CLI[@]}" "$WORK/does-not-exist" --once
check "no package still reports the title"   0 "No document retrievers"  -- "${CLI[@]}" --pid $PID --once
check "package loads and samples"            0 "LOADED: Custom.DocumentTracker.Smoke" -- "${CLI[@]}" "$PKG" --pid $PID --once
# --all disables dedupe: a second poll of an unchanged window still prints. Run two polls
# (killed by the timeout) and compare sample counts with and without --all.
all_samples=$(run_with_timeout 6 "${CLI[@]}" "$PKG" --pid $PID --interval 0.4 --all 2>/dev/null \
    | grep -cE "^[0-9]{2}:[0-9]{2}:[0-9]{2} ")
deduped_samples=$(run_with_timeout 6 "${CLI[@]}" "$PKG" --pid $PID --interval 0.4 2>/dev/null \
    | grep -cE "^[0-9]{2}:[0-9]{2}:[0-9]{2} ")
if [ "$all_samples" -gt "$deduped_samples" ] && [ "$deduped_samples" -ge 1 ]; then
    pass "--all repeats unchanged samples ($all_samples vs $deduped_samples)"
else
    fail "--all repeats unchanged samples" "with --all: $all_samples, without: $deduped_samples"
fi
check "dead pid exits non-zero"              1 "not running"             -- "${CLI[@]}" "$PKG" --pid 999999 --once
check "packages root is enumerated"          0 "Packages root"           -- "${CLI[@]}" "$WORK/Packages" --pid $PID --once

PLAIN="$WORK/plaindir"
mkdir -p "$PLAIN"
cp "$PKG/Lib/SmokePlugin.dll" "$PLAIN/" 2>/dev/null
check "plain dll dir loads and warns"        0 "RUNS its code"           -- "${CLI[@]}" "$PLAIN" --pid $PID --once

echo
echo "== argument validation =="
check "--interval without value"             1 "Invalid arguments"       -- "${CLI[@]}" "$PKG" --once --interval
check "--interval not a number"              1 "Invalid arguments"       -- "${CLI[@]}" "$PKG" --once --interval abc
check "--interval zero"                      1 "Invalid arguments"       -- "${CLI[@]}" "$PKG" --once --interval 0
check "--interval negative"                  1 "Invalid arguments"       -- "${CLI[@]}" "$PKG" --once --interval -1
check "--pid not a number"                   1 "Invalid arguments"       -- "${CLI[@]}" "$PKG" --once --pid abc

echo
echo "== package validation (mistakes ManicTime ignores silently) =="
BAD="$WORK/bad"
mk_bad() { # mk_bad <dirname> <json>
    rm -rf "$BAD/$1"; mkdir -p "$BAD/$1/Lib"
    cp "$PKG/Lib/SmokePlugin.dll" "$BAD/$1/Lib/" 2>/dev/null
    printf '%s' "$2" > "$BAD/$1/PluginSpec.json"
}
mk_bad "Custom.DocumentTracker.NoVersion" '{"Id":"Custom.DocumentTracker.NoVersion","Type":"DocumentTracker","AssemblyName":"SmokePlugin.dll"}'
check "spec without Version rejected"        1 "parsable Version"        -- "${CLI[@]}" "$BAD/Custom.DocumentTracker.NoVersion" --pid $PID --once
mk_bad "Custom.DocumentTracker.NoType" '{"Id":"Custom.DocumentTracker.NoType","Version":"1.0.0.0","AssemblyName":"SmokePlugin.dll"}'
check "spec without Type rejected"           1 "must contain Type"       -- "${CLI[@]}" "$BAD/Custom.DocumentTracker.NoType" --pid $PID --once
mk_bad "WrongDirName" '{"Id":"Custom.DocumentTracker.Smoke","Version":"1.0.0.0","Type":"DocumentTracker","AssemblyName":"SmokePlugin.dll"}'
check "dir name != Id rejected"              1 "!= PluginSpec Id"        -- "${CLI[@]}" "$BAD/WrongDirName" --pid $PID --once
mk_bad "Custom.DocumentTracker.Traversal" '{"Id":"Custom.DocumentTracker.Traversal","Version":"1.0.0.0","Type":"DocumentTracker","AssemblyName":"../../evil.dll"}'
check "AssemblyName path rejected"           1 "bare file name"          -- "${CLI[@]}" "$BAD/Custom.DocumentTracker.Traversal" --pid $PID --once
mk_bad "Custom.DocumentTracker.IntHost" '{"Id":"Custom.DocumentTracker.IntHost","Version":"1.0.0.0","Type":"DocumentTracker","AssemblyName":"SmokePlugin.dll","MinHostVersion":"2026"}'
check "integer MinHostVersion rejected"      1 "not a valid version"     -- "${CLI[@]}" "$BAD/Custom.DocumentTracker.IntHost" --pid $PID --once
HOSTDLL="$WORK/Packages/Custom.DocumentTracker.Smoke/Lib/Finkit.ManicTime.Shared.dll"
cp "$ROOT/libs/Finkit.ManicTime.Shared.dll" "$HOSTDLL"
check "host dll in Lib warns"                0 "contains host assembly"  -- "${CLI[@]}" "$PKG" --pid $PID --once
rm -f "$HOSTDLL"

echo
echo "== json output =="
JSON_OUT="$WORK/out.json"
"${CLI[@]}" "$PKG" --pid $PID --once --json >"$JSON_OUT" 2>/dev/null
if [ -s "$JSON_OUT" ] && python3 -c "import json,sys; [json.loads(l) for l in open(sys.argv[1]) if l.strip()]" "$JSON_OUT" 2>/dev/null; then
    pass "--json stdout is valid NDJSON"
else
    fail "--json stdout is valid NDJSON" "$(head -c 200 "$JSON_OUT")"
fi
if ! grep -q '"processName"' "$JSON_OUT"; then
    fail "--json stdout carries only records" "no JSON record on stdout"
elif grep -qE '^(LOADED|Retrievers|Sampling|Watching)' "$JSON_OUT"; then
    fail "--json stdout carries only records" "diagnostics leaked into stdout"
else
    pass "--json stdout carries only records"
fi

echo
echo "== probe-ax =="
PROBE=(dotnet "$ROOT/skills/manictime-plugin/scripts/probe-ax/bin/Debug/probe-ax.dll")
if [ "$(uname)" = "Darwin" ] && [ -f "$ROOT/skills/manictime-plugin/scripts/probe-ax/bin/Debug/probe-ax.dll" ]; then
    check "probe without arguments prints usage" 1 "Usage:"              -- "${PROBE[@]}"
    check "probe with bad maxDepth prints usage" 1 "Usage:"              -- "${PROBE[@]}" $PID abc
else
    echo "  (macOS only, or not built — skipped)"
fi

echo
if [ "$FAIL" -eq 0 ]; then
    echo "All $PASS checks passed."
    exit 0
fi
echo "$FAIL of $((PASS + FAIL)) checks FAILED."
exit 1
