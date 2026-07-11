#!/usr/bin/env bash
#
# Replicates the claudedev/claudeuser sandbox setup used to run Claude Code
# against a repo while preventing it from writing to .git (no commits, no
# hook edits):
#
#   group  claudedev
#   user   claudeuser, secondary group claudedev
#   owner  the human user (e.g. "cihan"), also a member of claudedev
#   repo   owned <owner>:claudedev, working tree group-writable (setgid so
#          new files inherit the group), but .git/ has group+other write
#          stripped recursively so only <owner> (the directory owner) can
#          write git internals — claudeuser can read/edit source files but
#          cannot `git add`/`git commit`/touch .git/hooks.
#
# Must run as root (sudo). Idempotent — safe to re-run.
#
# Usage:
#   sudo scripts/setup-claudedev-sandbox.sh --owner cihan --claude-user claudeuser
#   sudo scripts/setup-claudedev-sandbox.sh --owner cihan --repo /path/to/repo
#
# --repo can be passed multiple times to harden several repos in one run.
# Also wired up as `make init` in this repo's Makefile (hardens this repo).

set -euo pipefail

OWNER_USER=""
CLAUDE_USER="claudeuser"
GROUP_NAME="claudedev"
REPOS=()

usage() {
    echo "Usage: $0 --owner <existing-user> [--claude-user <name>] [--group <name>] [--repo <path> ...]" >&2
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --owner) OWNER_USER="$2"; shift 2 ;;
        --claude-user) CLAUDE_USER="$2"; shift 2 ;;
        --group) GROUP_NAME="$2"; shift 2 ;;
        --repo) REPOS+=("$2"); shift 2 ;;
        -h|--help) usage ;;
        *) echo "Unknown argument: $1" >&2; usage ;;
    esac
done

if [[ "$(id -u)" -ne 0 ]]; then
    echo "Run as root (sudo)." >&2
    exit 1
fi

if [[ -z "$OWNER_USER" ]]; then
    echo "--owner is required (the existing human user who should keep write access to .git)." >&2
    usage
fi

if ! id "$OWNER_USER" >/dev/null 2>&1; then
    echo "Owner user '$OWNER_USER' does not exist on this machine — create it first." >&2
    exit 1
fi

echo "== Group: $GROUP_NAME =="
if getent group "$GROUP_NAME" >/dev/null 2>&1; then
    echo "Group '$GROUP_NAME' already exists, skipping."
else
    groupadd "$GROUP_NAME"
    echo "Created group '$GROUP_NAME'."
fi

in_group() {
    id -nG "$1" 2>/dev/null | tr ' ' '\n' | grep -qx "$2"
}

echo "== User: $CLAUDE_USER =="
if id "$CLAUDE_USER" >/dev/null 2>&1; then
    echo "User '$CLAUDE_USER' already exists, skipping creation."
else
    useradd --create-home --shell /bin/bash --user-group "$CLAUDE_USER"
    echo "Created user '$CLAUDE_USER' with home /home/$CLAUDE_USER."
fi
if in_group "$CLAUDE_USER" "$GROUP_NAME"; then
    echo "'$CLAUDE_USER' already a member of '$GROUP_NAME', skipping."
else
    usermod -aG "$GROUP_NAME" "$CLAUDE_USER"
    echo "Added '$CLAUDE_USER' to '$GROUP_NAME'."
fi

echo "== Owner: $OWNER_USER =="
if in_group "$OWNER_USER" "$GROUP_NAME"; then
    echo "'$OWNER_USER' already a member of '$GROUP_NAME', skipping."
else
    usermod -aG "$GROUP_NAME" "$OWNER_USER"
    echo "Added '$OWNER_USER' to '$GROUP_NAME'."
fi

repo_ownership_ok() {
    local repo="$1"
    [[ -z "$(find "$repo" \( ! -user "$OWNER_USER" -o ! -group "$GROUP_NAME" \) -print -quit 2>/dev/null)" ]]
}

worktree_perms_ok() {
    local repo="$1"
    # every non-.git directory should be setgid with group rwx (mode bits 2070)
    [[ -z "$(find "$repo" \( -path "$repo/.git" -prune \) -o \( -type d ! -perm -2070 -print \) -quit 2>/dev/null)" ]]
}

git_perms_ok() {
    local repo="$1"
    # nothing under .git should have group-write or other-write set
    [[ -z "$(find "$repo/.git" \( -perm -020 -o -perm -002 \) -print -quit 2>/dev/null)" ]]
}

harden_repo() {
    local repo="$1"

    if [[ ! -d "$repo/.git" ]]; then
        echo "  '$repo' is not a git repo (no .git dir) — skipping." >&2
        return 1
    fi

    echo "== Hardening $repo =="

    if repo_ownership_ok "$repo"; then
        echo "  Ownership already $OWNER_USER:$GROUP_NAME, skipping."
    else
        chown -R "$OWNER_USER:$GROUP_NAME" "$repo"
        echo "  Fixed ownership to $OWNER_USER:$GROUP_NAME."
    fi

    # Working tree: owner keeps full control, group can read/write/execute so
    # claudeuser can edit source, setgid so new files/dirs inherit the group.
    if worktree_perms_ok "$repo"; then
        echo "  Working tree already group-writable + setgid, skipping."
    else
        find "$repo" -mindepth 1 -maxdepth 1 ! -name .git -type d -exec chmod -R g+rwX,o-w {} \; 2>/dev/null || true
        find "$repo" -mindepth 1 -maxdepth 1 ! -name .git -type d -exec find {} -type d -exec chmod g+s {} \; \; 2>/dev/null || true
        echo "  Set working tree to group-writable + setgid."
    fi

    # .git internals: strip group and other write recursively. Read/execute
    # (needed for `git log`/`git diff`/`git status`) stays intact.
    if git_perms_ok "$repo"; then
        echo "  .git already has group/other write stripped, skipping."
    else
        chmod -R g-w,o-w "$repo/.git"
        echo "  Stripped group/other write from .git."
    fi

    echo "  Owner '$OWNER_USER' can still commit/push/edit hooks normally."
    echo "  '$CLAUDE_USER' (via group '$GROUP_NAME') can read the repo and read/write"
    echo "  tracked source files, but cannot write anywhere under .git — no commit,"
    echo "  no stage, no hook edits."
}

for repo in "${REPOS[@]}"; do
    harden_repo "$repo"
done

if [[ ${#REPOS[@]} -eq 0 ]]; then
    echo
    echo "No --repo given — user/group setup done. To lock down a specific repo later, run:"
    echo "  sudo $0 --owner $OWNER_USER --claude-user $CLAUDE_USER --repo /path/to/repo"
fi

echo
echo "Done. Verify with:"
echo "  id $CLAUDE_USER"
echo "  ls -ld <repo>/.git <repo>/.git/hooks"
