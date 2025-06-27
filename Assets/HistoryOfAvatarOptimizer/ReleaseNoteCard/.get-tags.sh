#!/bin/sh

SCRIPT_DIR=$(dirname "$0")

TZ=UTC0 git tag --format='%(committerdate:iso-local) %(refname:strip=2)' | grep '+0000 v' | sed -E 's/([0-9-]+) ([0-9:]+) ([+-][0-9]+)/\1T\2\3/' > "$SCRIPT_DIR/tags.txt"
