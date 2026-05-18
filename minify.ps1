$appPath = "$PSScriptRoot\app.html"
$idxPath = "$PSScriptRoot\index.html"

Write-Host "Reading app.html..."
$content = [System.IO.File]::ReadAllText($appPath, [System.Text.Encoding]::UTF8)

# JS部分のコメント削除（正規表現）
Write-Host "Removing JS line comments..."
# テンプレートリテラル外の // コメントを削除
$content = [System.Text.RegularExpressions.Regex]::Replace(
    $content,
    '(?m)(?<!:)(?<!https?)//[^\n]*',
    ''
)

# 連続空行を2行に圧縮
Write-Host "Compressing blank lines..."
$content = [System.Text.RegularExpressions.Regex]::Replace(
    $content,
    '(\r?\n){3,}',
    "`n`n"
)

Write-Host "Writing index.html..."
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($idxPath, $content, $utf8NoBom)

$appSize = (Get-Item $appPath).Length
$idxSize = (Get-Item $idxPath).Length
$reduction = [math]::Round((1 - $idxSize / $appSize) * 100, 1)

Write-Host ""
Write-Host "=== 完了 ==="
Write-Host "app.html  : $([math]::Round($appSize / 1KB, 1)) KB"
Write-Host "index.html: $([math]::Round($idxSize / 1KB, 1)) KB"
Write-Host "削減率    : $reduction%"
