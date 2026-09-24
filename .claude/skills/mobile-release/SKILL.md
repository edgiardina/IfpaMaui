---
name: mobile-release
description: Cut an IFPA Companion release end to end. Decide the version, write the release notes, tag to start the store build (Google Play production and TestFlight), then submit the iOS build to the App Store through the App Store Connect API. Nobody signs in to Apple. Use when Ed asks to cut, ship, or prepare a release, bump the version, or write release notes.
---

# mobile-release

This skill releases a new version of the app on both stores. Ed approves the version and the release notes one time. After that approval, the skill does all the steps. Nobody signs in to App Store Connect or the Play Console.

## Pipeline

| Step | What starts it | What it does |
|---|---|---|
| `publish.yml` | A pushed `vX.Y.Z` tag | **Android:** signs the AAB and sends it to the Google Play **production** track. Google reviews it, then it goes live. **iOS:** signs the IPA and uploads it to TestFlight. |
| `release-to-app-store.yml` | `gh workflow run` (manual only) | Waits until Apple processes the TestFlight build. Then it creates the App Store version, attaches the build, sets "What's New", and submits for App Review. The version goes live when Apple approves it. |

- The **tag** is the version. `publish.yml` gives the version to MAUI. Do not edit `ApplicationDisplayVersion` in the csproj.
- The **build number** is `1000 + run_number` of the `publish.yml` run. It is the iOS `CFBundleVersion` and the Android `versionCode`.
- `release-to-app-store.yml` uses fastlane (`fastlane/Fastfile`, lane `ios appstore_release`) and the App Store Connect API key in the `ASC_KEY_ID`, `ASC_ISSUER_ID` and `ASC_KEY_P8` repository secrets.
- Google Play gets **no release notes**. The r0adkll action in `publish.yml` sends none.

## Rules

- **Ed approves one time, then you do all of it.** Before you push the tag, show Ed the version and the exact release notes. Wait for a clear yes. That yes covers the tag push, the GitHub Release, and the App Store submission. Do not ask again for each step.
- **The tag push is the ship decision.** It sends Android to Google Play production at once. Thus the approval must come before the push.
- **Tag the `origin/main` SHA**, not the local checkout. The local branch can be older than the last merge.
- **Do not commit the release notes.** Keep them in a scratchpad file. The GitHub Release body and the App Store dispatch both use that file.
- If a step fails, stop and tell Ed. Do not delete a tag, retag, or run a store step again without his OK.

## 1. Decide the version

```bash
git fetch origin --tags
LAST=$(git tag --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -1); echo "last tag: $LAST"
git log $LAST..origin/main --oneline --no-merges
python .claude/skills/mobile-release/asc_status.py     # live App Store versions + latest builds
```

- A release can ship from a manual dispatch with no tag. Thus the live App Store version can be newer than the last tag. The new version must be **higher than both**.
- TestFlight test builds can already use the next version number (for example `3.4.12`). You can release that number. The build number keeps the builds apart.
- Use a **patch** bump for fixes and small changes. Use a **minor** bump when new features ship (a widget, a new screen). Tell Ed your choice and why. He can change it.
- Make sure the tag does not exist: `git ls-remote --tags origin "vX.Y.Z"`.

## 2. Write the release notes

Write **one** block of text to `<scratchpad>/notes.txt`. The App Store "What's New" and the GitHub Release both use it.

- Keep it at **500 characters or fewer**. Check with `wc -m < <scratchpad>/notes.txt`.
- Include only changes that a user can see. Examples: new features, fixed crashes, visible UI fixes. Leave out package updates, CI work, refactors and tests.
- Put related commits together in one line. Say what the user gets, not what the code does.
- Plain sentences. No markdown. The App Store shows the text as it is.

**Approval gate:** show Ed the version and the **exact** `notes.txt` text, not a summary or a file path. Make his changes and show it again until he says yes. Do not skip this gate.

## 3. Tag and publish the GitHub Release

Do this only after Ed says yes.

```bash
SHA=$(git rev-parse origin/main)
git tag -a vX.Y.Z $SHA -m "vX.Y.Z"
git push origin vX.Y.Z                                   # starts publish.yml
gh release create vX.Y.Z --verify-tag --title "vX.Y.Z" --notes-file <scratchpad>/notes.txt
```

## 4. Watch the build

```bash
RUN=$(gh run list --workflow=publish.yml --event push --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch $RUN --exit-status --interval 60            # about 30 minutes; run it in the background
gh run view $RUN --log | grep -m1 -o 'ApplicationVersion=[0-9]*'    # the build number
```

- Make sure that the run is for the new tag (`headBranch` is `vX.Y.Z`).
- If the run fails, get the log (`gh run view $RUN --log-failed`), find the cause, and tell Ed. Android can be on Google Play already if only the iOS job failed.

## 5. Submit to the App Store

```bash
gh workflow run release-to-app-store.yml -f version=X.Y.Z -f buildNumber=<n> -F releaseNotes=@<scratchpad>/notes.txt
RUN=$(gh run list --workflow=release-to-app-store.yml --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch $RUN --exit-status --interval 60
```

- The lane waits up to 60 minutes for Apple to process the build. Then it submits.
- For a dry run that only creates the version and "What's New" with no submission, add `-f submitForReview=false`.
- After the run, `python .claude/skills/mobile-release/asc_status.py` shows the new version as `WAITING_FOR_REVIEW`.

## Report

Tell Ed:

- The version and the build number.
- The GitHub Release URL.
- The `publish.yml` run: Android sent to Google Play production, iOS uploaded to TestFlight.
- The `release-to-app-store.yml` run: iOS submitted for review. It goes live when Apple approves it.

## If a step fails

- **deliver cannot find the build:** Apple is still processing it, or the build number is wrong. Check with `asc_status.py`.
- **401 from App Store Connect:** an `ASC_*` secret is wrong. `ASC_KEY_P8` must be the base64 of the `.p8` file. `ASC_ISSUER_ID` is the plain issuer UUID.
- **The version already exists as READY_FOR_SALE:** that version shipped already. Use a higher version.
- If the workflow cannot do a step, Ed can do the same step by hand in App Store Connect.
