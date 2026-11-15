#!/usr/bin/env bash
# Stop: run tests when code changed since the last commit.
# Exit 2 keeps Claude working until the tests pass.
set -u

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"
input=$(cat)

# Already continuing because of this hook: do not loop forever.
[ "$(echo "$input" | jq -r '.stop_hook_active // false')" = "true" ] && exit 0

cd "$root" || exit 0
changed=$(git status --porcelain -- src tests web/src)
[ -z "$changed" ] && exit 0

fail=0
if echo "$changed" | grep -qE ' (src|tests)/'; then
  out=$(dotnet test Atlas.slnx --nologo -v q 2>&1)
  if [ $? -ne 0 ]; then
    echo "dotnet test failed:" >&2
    echo "$out" | grep -E 'Failed|error|Error' | head -20 >&2
    fail=1
  fi
fi
if echo "$changed" | grep -q ' web/src/'; then
  out=$(cd web && NG_CLI_ANALYTICS=false npx ng test --watch=false 2>&1)
  if [ $? -ne 0 ]; then
    echo "ng test failed:" >&2
    echo "$out" | grep -E 'FAIL|Error|✗|×' | head -20 >&2
    fail=1
  fi
fi
[ $fail -eq 1 ] && exit 2
exit 0
