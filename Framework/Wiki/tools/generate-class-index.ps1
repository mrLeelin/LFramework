param(
    [string]$FrameworkRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$Path)
    [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Write-Utf8Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Value
    )
    [System.IO.File]::WriteAllText($Path, $Value, $Utf8NoBom)
}

$files = Get-ChildItem -Path $FrameworkRoot -Recurse -Filter *.cs -File |
    Where-Object {
        $relative = $_.FullName.Substring($FrameworkRoot.Length + 1).Replace("\", "/")
        -not $relative.StartsWith("Wiki/") -and
        -not $relative.StartsWith(".understand-anything/")
    }

$typeRegex = [regex]'(?m)^\s*(?:\[[^\r\n]+\]\s*)*(?:(?:public|private|protected|internal|sealed|abstract|static|partial|readonly|unsafe|new)\s+)*(class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)(?:\s*<[^>{};]+>)?(?:\s*:\s*([^{\r\n]+))?'
$namespaceRegex = [regex]'(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)'
$methodRegex = [regex]'(?m)^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|partial|new|unsafe)\s+)+[A-Za-z_][A-Za-z0-9_<>,\[\]\.? ]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\('
$attrRegex = [regex]'\[([A-Za-z_][A-Za-z0-9_]*(?:Attribute)?)'

$types = New-Object System.Collections.Generic.List[object]
$byName = @{}

foreach ($file in $files) {
    $text = Read-Utf8Text -Path $file.FullName
    $namespaceMatch = $namespaceRegex.Match($text)
    $namespace = if ($namespaceMatch.Success) { $namespaceMatch.Groups[1].Value } else { "" }
    $relativePath = $file.FullName.Substring($FrameworkRoot.Length + 1).Replace("\", "/")
    $matches = $typeRegex.Matches($text)
    foreach ($match in $matches) {
        $kind = $match.Groups[1].Value
        $name = $match.Groups[2].Value
        $bases = @()
        if ($match.Groups[3].Success) {
            $bases = $match.Groups[3].Value.Split(",") |
                ForEach-Object { $_.Trim() -replace '\s+where\s+.*$', '' } |
                Where-Object { $_ }
        }
        $methods = @($methodRegex.Matches($text) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        $attributes = @($attrRegex.Matches($text) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        $qualifiedName = if ($namespace) { "$namespace.$name" } else { $name }
        $entry = [ordered]@{
            id = $types.Count
            name = $name
            qualifiedName = $qualifiedName
            kind = $kind
            namespace = $namespace
            path = $relativePath
            bases = @($bases)
            attributes = @($attributes)
            methods = @($methods)
            references = @()
            referencedBy = @()
        }
        $types.Add($entry)
        if (-not $byName.ContainsKey($name)) {
            $byName[$name] = New-Object System.Collections.Generic.List[object]
        }
        $byName[$name].Add($entry)
    }
}

$identifierRegex = [regex]'\b[A-Za-z_][A-Za-z0-9_]*\b'

foreach ($type in $types) {
    $filePath = Join-Path $FrameworkRoot $type.path
    $text = Read-Utf8Text -Path $filePath
    $refs = New-Object System.Collections.Generic.HashSet[string]
    $identifiers = New-Object System.Collections.Generic.HashSet[string]
    foreach ($identifierMatch in $identifierRegex.Matches($text)) {
        [void]$identifiers.Add($identifierMatch.Value)
    }
    foreach ($identifier in $identifiers) {
        if ($identifier -eq $type.name) {
            continue
        }
        if ($byName.ContainsKey($identifier)) {
            [void]$refs.Add($identifier)
        }
    }
    $type.references = @($refs | Sort-Object)
}

$byQualified = @{}
foreach ($type in $types) {
    $byQualified[$type.qualifiedName] = $type
}

foreach ($type in $types) {
    foreach ($refName in $type.references) {
        if (-not $byName.ContainsKey($refName)) {
            continue
        }
        foreach ($target in $byName[$refName]) {
            if ($target.qualifiedName -eq $type.qualifiedName) {
                continue
            }
            $existing = @($target.referencedBy)
            if ($existing -notcontains $type.qualifiedName) {
                $target.referencedBy = @($existing + $type.qualifiedName | Sort-Object)
            }
        }
    }
}

$stats = [ordered]@{
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    scope = "Assets/Frame/Framework/**/*.cs"
    fileCount = @($files).Count
    typeCount = $types.Count
}

$payload = [ordered]@{
    stats = $stats
    types = @($types | Sort-Object qualifiedName)
}

$json = $payload | ConvertTo-Json -Depth 8
$outFile = Join-Path $FrameworkRoot "Wiki\html\class-index-data.js"
Write-Utf8Text -Path $outFile -Value "window.LFRAMEWORK_CLASS_INDEX = $json;"

Write-Host "Generated $($stats.typeCount) types from $($stats.fileCount) files."
Write-Host $outFile
