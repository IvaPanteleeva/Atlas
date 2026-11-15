#!/usr/bin/env bash
# PostToolUse (Edit|Write): build after C# edits, format after web/ edits.
# Exit 2 sends stderr back to Claude so it fixes the problem.
set -u

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"
file=$(jq -r '.tool_input.file_path // .tool_response.filePath // empty')
[ -z "$file" ] && exit 0

case "$file" in
  "$root"/src/*.cs|"$root"/tests/*.cs)
    out=$(cd "$root" && dotnet build Atlas.slnx --nologo -v q -clp:ErrorsOnly 2>&1)
    if [ $? -ne 0 ]; then
      echo "dotnet build failed after editing $file:" >&2
      echo "$out" | grep -E 'error|Error' | head -20 >&2
      exit 2
    fi
    ;;
  "$root"/web/src/*.ts|"$root"/web/src/*.html|"$root"/web/src/*.scss|"$root"/web/*.json)
    (cd "$root/web" && ./node_modules/.bin/prettier --write --log-level warn "$file") || true
    ;;
esac
exit 0
