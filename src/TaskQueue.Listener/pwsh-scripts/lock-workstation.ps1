# Lock Workstation
# Locks the Windows workstation

Write-Host "Locking workstation..."

rundll32.exe user32.dll,LockWorkStation

Write-Host "Done!"
