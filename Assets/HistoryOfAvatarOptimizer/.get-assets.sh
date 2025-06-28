#!/bin/sh

# get assets for HistoryOfAvatarOptimizer

SCRIPT_DIR=$(dirname "$0")

gh api --paginate --slurp graphql -f query='
query ($endCursor: String){
  repository(owner: "anatawa12", name: "AvatarOptimizer") {
    refs(refPrefix: "refs/tags/", orderBy: {field: TAG_COMMIT_DATE, direction: ASC}, first: 100, after: $endCursor) {
      nodes {
        name
        target {
          ... on Commit {
            committedDate
          }
        }
      }
      pageInfo{ hasNextPage, endCursor }
    }
  }
}' | jq '.[].data.repository.refs.nodes.[] | .target.committedDate + " " + .name' --raw-output | grep 'Z v' > "$SCRIPT_DIR/tags.txt"

curl -L 'https://github.com/anatawa12/AvatarOptimizer/raw/refs/heads/master/CHANGELOG-PRERELEASE.md' > "$SCRIPT_DIR/ReleaseNoteCard/CHANGELOG-PRERELEASE.md"
curl -L 'https://github.com/anatawa12/AvatarOptimizer/raw/refs/heads/master/CHANGELOG.md' > "$SCRIPT_DIR/ReleaseNoteCard/CHANGELOG.md"
