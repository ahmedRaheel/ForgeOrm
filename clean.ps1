```powershell
Write-Host "Cleaning solution..."

Get-ChildItem -Path . -Include bin,obj -Recurse -Directory |
ForEach-Object {
    Write-Host "Deleting $($_.FullName)"
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path ".\artifacts") {
    Remove-Item ".\artifacts" -Recurse -Force
}

Write-Host "Clean completed."
```
