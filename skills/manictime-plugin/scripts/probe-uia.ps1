# Dumps the UI Automation tree of a window so you can find the value you want and pick an anchor.
# One line per element, indented by depth:
#     ControlType  id='AutomationId'  name='Name'  class='ClassName'
#
#   powershell -ExecutionPolicy Bypass -File probe-uia.ps1 -ProcessName olk
#   powershell -ExecutionPolicy Bypass -File probe-uia.ps1 -ProcessId 1234 -MaxDepth 30
#
# -Filter prints only matching lines (case-insensitive substring), e.g. -Filter subject.
#
# This walks the tree reading properties one at a time, which is fine for a one-off dump but is
# exactly what your PLUGIN must not do — see references/windows-uia.md for the caching rules.
# Accessibility Insights for Windows and inspect.exe (Windows SDK) show the same tree interactively
# and are worth using alongside this script.
param(
    [string] $ProcessName,
    [int] $ProcessId = 0,
    [int] $MaxDepth = 25,
    [int] $MaxNodes = 4000,
    [string] $Filter
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

if ($ProcessId) {
    $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $proc) { Write-Error "No process with id $ProcessId."; exit 1 }
} else {
    if (-not $ProcessName) { Write-Error "Pass -ProcessName or -ProcessId."; exit 1 }
    $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) { Write-Error "No running '$ProcessName' with a window."; exit 1 }
}
if ($proc.MainWindowHandle -eq 0) { Write-Error "Process $($proc.Id) has no main window."; exit 1 }

Write-Host "# $($proc.ProcessName) (pid $($proc.Id)) — $($proc.MainWindowTitle)"

$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
if (-not $root) { Write-Error "Could not get an automation element for the window."; exit 1 }

$script:printed = 0
$script:truncated = $false
$walker = [System.Windows.Automation.TreeWalker]::RawViewWalker

function Dump($element, $depth) {
    if ($script:printed -ge $MaxNodes) { $script:truncated = $true; return }
    if ($depth -gt $MaxDepth) { return }

    try {
        $current = $element.Current
        $type = $current.ControlType.ProgrammaticName -replace '^ControlType\.', ''
        $line = (' ' * ($depth * 2)) + $type
        if ($current.AutomationId) { $line += " id='$($current.AutomationId)'" }
        if ($current.Name)         { $line += " name='$($current.Name)'" }
        if ($current.ClassName)    { $line += " class='$($current.ClassName)'" }
    } catch {
        # Element went away while we were reading it — normal in a live UI.
        return
    }

    if (-not $Filter -or $line -like "*$Filter*") { Write-Output $line }
    $script:printed++

    try { $child = $walker.GetFirstChild($element) } catch { return }
    while ($child) {
        Dump $child ($depth + 1)
        try { $child = $walker.GetNextSibling($child) } catch { break }
    }
}

Dump $root 0

if ($script:truncated) {
    Write-Host "# [$($script:printed) nodes — TRUNCATED at the $MaxNodes-node budget, the tree continues]"
} else {
    Write-Host "# [$($script:printed) nodes]"
}
if ($script:printed -le 1) {
    Write-Host "# Almost nothing in the tree — is this the right window? Some apps expose their"
    Write-Host "# content only under a Document element further down; try a larger -MaxDepth."
}
