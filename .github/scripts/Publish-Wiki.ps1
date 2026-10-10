<#
.SYNOPSIS
    Writes the project wiki from the docs folder: every page, with links that work in the wiki, a sidebar and a
    footer.
.DESCRIPTION
    docs is the only place the documents are written. The program shows the same pages as its help, and the wiki
    is made from them by this script, so nothing is written twice. It empties the wiki folder, all but its .git,
    and writes:

    - each Markdown page at the top of docs under its own name: docs/Help.v4.md becomes the wiki page Help.v4.
      Pages in sub-folders, such as docs/plans, are not published.
    - links that work in the wiki. A link to another page goes to that wiki page. A link to a file goes to the
      file in the repository, and a picture is served from the repository, both at the commit published, so the
      wiki holds no second copy of any file.
    - _Sidebar.md: the pages in the order docs/.order gives, then any page it does not name.
    - _Footer.md, under every page: the source of the wiki is docs, and the commit it was published from.
    - a short page for each old wiki page that links elsewhere may still name, pointing to where it went. They are
      listed in docs/.moved, one "Old-Name = Page#heading" a line.

    A link to a page, heading or file that docs does not hold stops the script, so a broken link is never
    published. The pages are written either way, to look at.

    .github/workflows/publish-wiki.yml runs it when docs change on master, and pushes what it writes.
.PARAMETER Docs
    The docs folder. Defaults to the one in this repository.
.PARAMETER Wiki
    The folder to write the wiki into: the workflow's clone of the wiki repository, or a folder to look at the
    result in. Nobody keeps a clone of the wiki: docs is the only copy anyone edits.
.PARAMETER Commit
    The commit published, which the links to files and the footer name.
.PARAMETER Repository
    The repository, as owner/name.
.EXAMPLE
    PS> .\.github\scripts\Publish-Wiki.ps1 -Wiki "$env:TEMP\x360ce-wiki-preview" -Commit (git rev-parse HEAD)
    Writes the wiki pages into a folder to look at, and fails on any broken link, before anything is pushed.
.OUTPUTS
    [string] What was written.
#>
[CmdletBinding()]
param(
    [string]$Docs = (Join-Path $PSScriptRoot "..\..\docs"),
    [Parameter(Mandatory = $true)] [string]$Wiki,
    [Parameter(Mandatory = $true)] [string]$Commit,
    [string]$Repository = "x360ce/x360ce"
)

$ErrorActionPreference = "Stop"

$Docs = (Resolve-Path -LiteralPath $Docs).Path
if (-not (Test-Path -LiteralPath $Wiki)) { New-Item -ItemType Directory -Path $Wiki | Out-Null }
$Wiki = (Resolve-Path -LiteralPath $Wiki).Path
$utf8 = New-Object System.Text.UTF8Encoding $false
$fileUrl = "https://github.com/$Repository/blob/$Commit/docs/"
$pictureUrl = "https://raw.githubusercontent.com/$Repository/$Commit/docs/"
$link = '!?\[([^\]]*)\]\(([^)\s]+)(?:\s+"[^"]*")?\)'
$problems = New-Object System.Collections.Generic.List[string]

# The pages, and the anchors of their headings as GitHub makes them: lower case, spaces to hyphens, and every other
# character that is not a letter, a digit, a hyphen or an underscore left out.
$pages = @{}
$anchors = @{}
foreach ($file in Get-ChildItem -LiteralPath $Docs -Filter *.md -File) {
    $pages[$file.BaseName] = [IO.File]::ReadAllText($file.FullName, $utf8)
    $anchors[$file.BaseName] = @([regex]::Matches($pages[$file.BaseName], '(?m)^#{1,6}[ \t]+(.+?)[ \t]*#*[ \t]*\r?$') |
        ForEach-Object { ($_.Groups[1].Value.ToLowerInvariant() -replace '[^\p{L}\p{N}\-_ ]', '') -replace ' ', '-' })
}
if (-not $pages.ContainsKey("Home")) { throw "docs has no Home.md, the page the wiki opens on." }

# Whether a wiki page, and the heading after its #, exist.
function Test-Page([string]$Target) {
    $hash = $Target.IndexOf("#")
    $page = if ($hash -lt 0) { $Target } else { $Target.Substring(0, $hash) }
    if (-not $pages.ContainsKey($page)) { return "$page, which docs does not hold" }
    if ($hash -ge 0 -and $anchors[$page] -notcontains $Target.Substring($hash + 1)) { return "$Target, a heading $page.md does not have" }
    return $null
}

# Everything but the wiki's own git folder goes, so a page removed from docs is removed from the wiki.
Get-ChildItem -LiteralPath $Wiki -Force | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force

foreach ($name in @($pages.Keys | Sort-Object)) {
    $text = [regex]::Replace($pages[$name], $link, [Text.RegularExpressions.MatchEvaluator] {
        param($m)
        $target = $m.Groups[2]
        $value = $target.Value
        if ($value.StartsWith("#") -or $value -match '^[A-Za-z][A-Za-z0-9+.-]*:') { return $m.Value }
        $hash = $value.IndexOf("#")
        $path = if ($hash -lt 0) { $value } else { $value.Substring(0, $hash) }
        if ($path -like "*.md" -and $path -notmatch '/') {
            $resolved = $path.Substring(0, $path.Length - 3) + $(if ($hash -lt 0) { "" } else { $value.Substring($hash) })
            $missing = Test-Page $resolved
            if ($missing) { $problems.Add("$name.md links to $missing.") }
        }
        else {
            if (-not (Test-Path -LiteralPath (Join-Path $Docs $path) -PathType Leaf)) { $problems.Add("$name.md links to $path, which docs does not hold.") }
            $resolved = if ($m.Value.StartsWith("!")) { $pictureUrl + $path } else { $fileUrl + $value }
        }
        $start = $target.Index - $m.Index
        return $m.Value.Substring(0, $start) + $resolved + $m.Value.Substring($start + $target.Length)
    })
    [IO.File]::WriteAllText((Join-Path $Wiki "$name.md"), $text, $utf8)
}

$order = @()
$orderFile = Join-Path $Docs ".order"
if (Test-Path -LiteralPath $orderFile) {
    foreach ($entry in Get-Content -LiteralPath $orderFile -Encoding UTF8) {
        $entry = $entry.Trim()
        if ($entry.Length -eq 0) { continue }
        if ($pages.ContainsKey($entry)) { $order += $entry } else { $problems.Add("docs/.order names $entry, which docs does not hold.") }
    }
}
$order += @($pages.Keys | Sort-Object | Where-Object { $order -notcontains $_ })
[IO.File]::WriteAllText((Join-Path $Wiki "_Sidebar.md"), (($order | ForEach-Object { "- [$_]($_)" }) -join "`n") + "`n", $utf8)

$short = $Commit.Substring(0, [Math]::Min(8, $Commit.Length))
[IO.File]::WriteAllText((Join-Path $Wiki "_Footer.md"),
    "The source of this wiki is the [docs](https://github.com/$Repository/tree/master/docs) folder of the repository, " +
    "published here from commit [$short](https://github.com/$Repository/commit/$Commit). Change a page there: a " +
    "change made in the wiki is overwritten by the next publish.`n", $utf8)

$moved = 0
$movedFile = Join-Path $Docs ".moved"
if (Test-Path -LiteralPath $movedFile) {
    foreach ($entry in Get-Content -LiteralPath $movedFile -Encoding UTF8) {
        if ($entry.Trim().Length -eq 0) { continue }
        $old, $to = $entry -split '=', 2 | ForEach-Object { $_.Trim() }
        if (-not $to) { $problems.Add("docs/.moved has a line that is not Old-Name = Page: $entry"); continue }
        if ($pages.ContainsKey($old)) { $problems.Add("The old wiki page $old is a page of docs again: take it out of docs/.moved.") }
        $missing = Test-Page $to
        if ($missing) { $problems.Add("The old wiki page $old points to $missing.") }
        [IO.File]::WriteAllText((Join-Path $Wiki "$old.md"), "# $old`n`nThis page is no longer kept here. See [$($to.Split('#')[0])]($to).`n", $utf8)
        $moved++
    }
}

if ($problems.Count -gt 0) { throw ("The wiki would carry broken links:`n" + ($problems -join "`n")) }
"Wrote $($pages.Count) pages, $moved moved pages, _Sidebar.md and _Footer.md to $Wiki."
