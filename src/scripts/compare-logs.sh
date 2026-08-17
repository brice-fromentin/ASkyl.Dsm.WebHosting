#!/usr/bin/env bash
#
# Compares two deployment log sets and prints what changed between them.
#
# Reading a deployed log is what found the PR #38 logout call shape and the PR #39 validation-cache
# defect. Neither was visible in source: the cache class looks correct until you notice IDsmSession is
# Scoped. This script does not decide anything — it surfaces what moved, so the reading starts from a
# short list instead of a 300 KB file.
#
#   ./src/scripts/compare-logs.sh                  compares the two most recent sets in logs-review/
#   ./src/scripts/compare-logs.sh OLD NEW          each argument is a .zip, a directory, or a .txt
#
# The application produces the .zip itself, through the log download endpoint.
#
# OUTPUT IS LOCAL INFRASTRUCTURE DATA. It carries hostnames, ports and deployment paths, exactly what
# AGENTS.md section 3 forbids transcribing. Read it, act on it, never paste it into a document, a commit
# message or a pull request description.

set -eu

# Durations are written with a dot. Under a comma-decimal locale awk reads 12.5 as 12 and sort orders
# them wrongly, so every number below would be quietly false — the same trap the vulnerable-package
# check documents for its own output.
export LC_ALL=C

REVIEW_DIR="${LOG_REVIEW_DIR:-logs-review}"
EVENT_ID_REGISTRY="src/Askyl.Dsm.WebHosting.Constants/Logging/LogEventIds.cs"
SLOWEST_COUNT=10
BOOT_DIFF_LIMIT=40
WORK_DIR="$(mktemp -d)"

trap 'rm -rf "$WORK_DIR"' EXIT

# --- input resolution ---------------------------------------------------------------------------

# Turns a .zip, a directory or a single file into one flat text stream.
collect() {
    local source="$1" target="$2"

    if [ -d "$source" ]; then
        find "$source" -name '*.txt' -type f | sort | xargs cat > "$target" 2>/dev/null || true
    elif [ "${source##*.}" = "zip" ]; then
        local extracted="$WORK_DIR/$(basename "$source" .zip)-x"
        mkdir -p "$extracted"
        unzip -qo "$source" -d "$extracted"
        find "$extracted" -name '*.txt' -type f | sort | xargs cat > "$target" 2>/dev/null || true
    else
        cat "$source" > "$target"
    fi

    if [ ! -s "$target" ]; then
        printf 'No log content found in %s\n' "$source" >&2
        exit 1
    fi
}

if [ "$#" -eq 2 ]; then
    OLD_SOURCE="$1"
    NEW_SOURCE="$2"
elif [ "$#" -eq 0 ]; then
    if [ ! -d "$REVIEW_DIR" ]; then
        printf 'Create %s and drop the two most recent log archives in it, or pass two paths.\n' "$REVIEW_DIR" >&2
        exit 1
    fi

    # Newest two entries, whatever their form. Ordering is by modification time, so downloading in
    # deployment order is all that is required of the operator.
    #
    # Deliberately no `mapfile` and no `stat -f`: macOS ships bash 3.2, where mapfile does not exist,
    # and `stat -f` is BSD syntax that fails on Linux. `ls -t` sorts by mtime on both.
    NEW_SOURCE=""
    OLD_SOURCE=""

    while IFS= read -r entry; do
        [ -z "$entry" ] && continue

        if [ -z "$NEW_SOURCE" ]; then
            NEW_SOURCE="$entry"
        elif [ -z "$OLD_SOURCE" ]; then
            OLD_SOURCE="$entry"
        fi
    done <<EOF
$(find "$REVIEW_DIR" -maxdepth 1 \( -name '*.zip' -o -name '*.txt' \) -type f -exec ls -t {} + 2>/dev/null | head -2)
EOF

    if [ -z "$OLD_SOURCE" ]; then
        printf 'Need two log sets in %s. Drop the archive from the previous deployment beside the new one.\n' "$REVIEW_DIR" >&2
        exit 1
    fi
else
    printf 'Usage: %s [previous current]\n' "$0" >&2
    exit 1
fi

collect "$OLD_SOURCE" "$WORK_DIR/old.txt"
collect "$NEW_SOURCE" "$WORK_DIR/new.txt"

printf '\n══ Deployment log review ══\n'
printf '  previous : %s\n' "$OLD_SOURCE"
printf '  current  : %s\n\n' "$NEW_SOURCE"

# --- service attribution ------------------------------------------------------------------------

# Each service owns an EventId range declared in LogEventIds.cs as `public const int XxxBase = N;`.
# Parsing the constants rather than the region comments keeps this to plain ASCII.
if [ -f "$EVENT_ID_REGISTRY" ]; then
    grep -oE 'const int [A-Za-z]+Base = [0-9]+' "$EVENT_ID_REGISTRY" \
        | sed -E 's/const int ([A-Za-z]+)Base = ([0-9]+)/\2 \1/' \
        | sort -n > "$WORK_DIR/ranges.txt"
else
    : > "$WORK_DIR/ranges.txt"
fi

owner_of() {
    awk -v id="$1" '$1 <= id { name = $2 } END { print (name == "" ? "?" : name) }' "$WORK_DIR/ranges.txt"
}

ids_of() { grep -oE 'Id: [0-9]+' "$1" | awk '{print $2}'; }

name_of_id() {
    grep -oE "Id: $1, Name: \"[A-Za-z]+\"" "$WORK_DIR/new.txt" | head -1 | sed -E 's/.*Name: "([A-Za-z]+)"/\1/'
}

# --- 1. event ids that never appeared before ------------------------------------------------------

printf '── 1. New event ids ─────────────────────────────────────────\n'
ids_of "$WORK_DIR/old.txt" | sort -u > "$WORK_DIR/old-ids.txt"
ids_of "$WORK_DIR/new.txt" | sort -u > "$WORK_DIR/new-ids.txt"

if comm -13 "$WORK_DIR/old-ids.txt" "$WORK_DIR/new-ids.txt" | grep -q .; then
    comm -13 "$WORK_DIR/old-ids.txt" "$WORK_DIR/new-ids.txt" | while read -r id; do
        printf '  %-9s %-22s %s\n' "$id" "$(owner_of "$id")" "$(name_of_id "$id")"
    done
else
    printf '  none\n'
fi

# --- 2. how much each event id moved --------------------------------------------------------------

printf '\n── 2. Count changes, largest first ──────────────────────────\n'
ids_of "$WORK_DIR/old.txt" | sort | uniq -c | awk '{print $2, $1}' | sort > "$WORK_DIR/old-counts.txt"
ids_of "$WORK_DIR/new.txt" | sort | uniq -c | awk '{print $2, $1}' | sort > "$WORK_DIR/new-counts.txt"

join -a1 -a2 -e 0 -o 0,1.2,2.2 "$WORK_DIR/old-counts.txt" "$WORK_DIR/new-counts.txt" \
    | awk '{ delta = $3 - $2; if (delta != 0) printf "%d %d %d %d\n", (delta < 0 ? -delta : delta), $1, $2, $3 }' \
    | sort -rn | head -12 > "$WORK_DIR/deltas.txt"

if [ -s "$WORK_DIR/deltas.txt" ]; then
    while read -r _ id before after; do
        printf '  %-9s %-22s %6s -> %-6s %s\n' "$id" "$(owner_of "$id")" "$before" "$after" "$(name_of_id "$id")"
    done < "$WORK_DIR/deltas.txt"
else
    printf '  no change\n'
fi

# --- 3. warnings and errors -----------------------------------------------------------------------

printf '\n── 3. Warnings and errors in the current log ────────────────\n'
grep -E '\[(WRN|ERR|FTL)\]' "$WORK_DIR/new.txt" | grep -oE 'Id: [0-9]+' | awk '{print $2}' \
    | sort | uniq -c | sort -rn > "$WORK_DIR/problems.txt"

if [ -s "$WORK_DIR/problems.txt" ]; then
    while read -r count id; do
        printf '  %5sx  %-9s %-22s %s\n' "$count" "$id" "$(owner_of "$id")" "$(name_of_id "$id")"
    done < "$WORK_DIR/problems.txt"
else
    printf '  none\n'
fi

# --- 4. startup sequence --------------------------------------------------------------------------

# The last boot in each file. A registration that moved, a middleware that vanished or a configuration
# that resolved elsewhere all show up as a difference in this sequence.
printf '\n── 4. Startup sequence ──────────────────────────────────────\n'

last_boot() {
    local file="$1" end
    end=$(grep -n 'Application started' "$file" | tail -1 | cut -d: -f1)

    if [ -z "$end" ]; then
        return 0
    fi

    local start
    start=$(head -n "$((end - 1))" "$file" | grep -n 'Application started' | tail -1 | cut -d: -f1)
    sed -n "${start:-1},${end}p" "$file" | grep -oE 'Name: "[A-Za-z]+"' | sed -E 's/Name: "([A-Za-z]+)"/\1/'
}

last_boot "$WORK_DIR/old.txt" > "$WORK_DIR/old-boot.txt"
last_boot "$WORK_DIR/new.txt" > "$WORK_DIR/new-boot.txt"

if [ ! -s "$WORK_DIR/new-boot.txt" ]; then
    printf '  no startup found in the current log\n'
elif diff -q "$WORK_DIR/old-boot.txt" "$WORK_DIR/new-boot.txt" >/dev/null 2>&1; then
    printf '  identical (%s steps)\n' "$(wc -l < "$WORK_DIR/new-boot.txt" | tr -d ' ')"
else
    diff "$WORK_DIR/old-boot.txt" "$WORK_DIR/new-boot.txt" | grep -E '^[<>]' > "$WORK_DIR/boot-diff.txt" || true
    differing=$(wc -l < "$WORK_DIR/boot-diff.txt" | tr -d ' ')

    # Comparing two consecutive deployments yields a handful of lines. Hundreds means the two files
    # are not comparable — different sessions, or a boot marker the parser could not bracket — and
    # printing them all would bury the four sections that are still meaningful.
    if [ "$differing" -gt "$BOOT_DIFF_LIMIT" ]; then
        printf '  %s differences over %s and %s steps — too far apart to be read as a sequence.\n' \
            "$differing" \
            "$(wc -l < "$WORK_DIR/old-boot.txt" | tr -d ' ')" \
            "$(wc -l < "$WORK_DIR/new-boot.txt" | tr -d ' ')"
        printf '  Compare two consecutive deployments instead, or inspect the boot blocks by hand.\n'
    else
        sed 's/^</  removed:/; s/^>/  added:  /' "$WORK_DIR/boot-diff.txt"
    fi
fi

# --- 5. slowest requests --------------------------------------------------------------------------

# The PR #39 defect looked exactly like this: every authenticated request paying a DSM round trip that
# a working cache would have spared it.
printf '\n── 5. Slowest requests ──────────────────────────────────────\n'

durations_of() { grep -oE '[0-9]+(\.[0-9]+)?ms' "$1" | sed 's/ms$//' | sort -rn; }

summarise() {
    durations_of "$1" | awk -v label="$2" '
        { v[NR] = $1 }
        END {
            if (NR == 0) { printf "  %-10s no timed request\n", label; exit }
            printf "  %-10s max %8.1fms   median %8.1fms   count %d\n", label, v[1], v[int(NR/2) + 1], NR
        }'
}

summarise "$WORK_DIR/old.txt" "previous"
summarise "$WORK_DIR/new.txt" "current"

printf '\n  top %s in the current log:\n' "$SLOWEST_COUNT"
grep -E '[0-9]+(\.[0-9]+)?ms' "$WORK_DIR/new.txt" \
    | awk '{ match($0, /[0-9]+(\.[0-9]+)?ms/); printf "%s\t%s\n", substr($0, RSTART, RLENGTH - 2), $0 }' \
    | sort -rn | head -"$SLOWEST_COUNT" | cut -f2- | cut -c1-150 | sed 's/^/    /'

printf '\nRemember: this output is local infrastructure data. Do not transcribe it anywhere.\n\n'
