# Makes the repository that holds this script match its .gitattributes, exactly as a fresh clone: stores every
# text file with LF, commits that together with .gitattributes, then writes every file again with the line
# endings a checkout gives it. Uncommitted changes to tracked files go into the same commit; commit them first
# to keep them apart.
Push-Location -LiteralPath $PSScriptRoot
git add .gitattributes
git add --renormalize .
git commit -m "Normalize line endings"
# Stop if the commit failed, so the reset below cannot drop the staged changes.
if (git diff --cached --name-only) { Pop-Location; throw 'The commit failed; nothing was reset.' }
git rm --cached -r -q .
git reset --hard
Pop-Location
