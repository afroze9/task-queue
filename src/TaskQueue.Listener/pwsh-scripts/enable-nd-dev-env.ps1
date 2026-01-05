# Enable ND Dev Environment
# Opens project directories in VS Code

$ProjectDir1 = "I:\source\repos\nd\APP09934-aura-platform-core"
$ProjectDir2 = "I:\source\repos\nd\APP09934-aura-platform-ui"

Write-Host "Opening VS Code for ND Dev Environment..."

code $ProjectDir1
code $ProjectDir2

Write-Host "Done!"
