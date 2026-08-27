# Lists what is available for COM automation right now: every moniker in the Running Object Table,
# plus a check of common ProgIDs. Use it to find out whether the app you care about exposes a COM
# object model — see references/windows-com.md.
#
#   powershell -ExecutionPolicy Bypass -File probe-com.ps1
#   powershell -ExecutionPolicy Bypass -File probe-com.ps1 -ProgId InDesign.Application.2025
#
# Run it with Windows PowerShell (powershell.exe), not PowerShell 7: the attach check uses
# Marshal.GetActiveObject, which only exists on .NET Framework.
param(
    [string[]] $ProgId
)

Add-Type -Namespace Probe -Name Rot -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("ole32.dll")]
public static extern int GetRunningObjectTable(int reserved, out System.Runtime.InteropServices.ComTypes.IRunningObjectTable prot);
[System.Runtime.InteropServices.DllImport("ole32.dll")]
public static extern int CreateBindCtx(int reserved, out System.Runtime.InteropServices.ComTypes.IBindCtx ppbc);
'@

Write-Host "== Running Object Table =="
$rot = $null; $ctx = $null
[Probe.Rot]::GetRunningObjectTable(0, [ref] $rot) | Out-Null
[Probe.Rot]::CreateBindCtx(0, [ref] $ctx) | Out-Null
$enum = $null
$rot.EnumRunning([ref] $enum)
$enum.Reset()
$moniker = New-Object System.Runtime.InteropServices.ComTypes.IMoniker[] 1
while ($enum.Next(1, $moniker, [IntPtr]::Zero) -eq 0) {
    $name = ""
    try { $moniker[0].GetDisplayName($ctx, $null, [ref] $name) } catch { continue }
    # A moniker ending in :<pid> identifies one instance of a multi-instance app (e.g.
    # !VisualStudio.DTE:1234) — match on the pid to talk to the window you were asked about.
    Write-Output "  $name"
}

Write-Host ""
Write-Host "== ProgIDs that answer right now =="
$candidates = if ($ProgId) { $ProgId } else {
    @("Outlook.Application", "Word.Application", "Excel.Application", "PowerPoint.Application",
      "OneNote.Application", "Visio.Application", "MSProject.Application", "AutoCAD.Application",
      "Photoshop.Application", "Illustrator.Application", "InDesign.Application",
      "Acrobat.Application", "Shell.Application")
}
foreach ($id in $candidates) {
    $type = [Type]::GetTypeFromProgID($id)
    if (-not $type) { continue }
    try {
        # Attaches to an already-running instance; it never starts the app.
        # (Windows PowerShell 5.1 only — in PowerShell 7 this method does not exist.)
        $obj = [System.Runtime.InteropServices.Marshal]::GetActiveObject($id)
        Write-Output "  $id  -> RUNNING (attachable)"
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($obj) | Out-Null
    } catch [System.Management.Automation.MethodInvocationException] {
        Write-Output "  $id  -> registered, not running"
    } catch {
        Write-Output "  $id  -> registered; could not attach ($($_.Exception.GetType().Name))"
    }
}
Write-Host ""
Write-Host "# Nothing here does not always mean no COM: some apps (Adobe) never register in the ROT"
Write-Host "# but still attach via a versioned ProgID — see references/windows-com.md."
