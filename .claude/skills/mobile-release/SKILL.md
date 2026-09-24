---
name: mobile-release
description: Cut an IFPA Companion release end to end. Decide the version, write the release notes, and create the GitHub Release. The release starts publish.yml, which sends Android to Google Play production, uploads iOS to TestFlight, and submits the iOS build to the App Store through the App Store Connect API. Nobody signs in to Apple. Use when Ed asks to cut, ship, or prepare a release, bump the version, or write release notes.
---

# mobile-release

This skill releases a new version of the app on both stores. Ed approves the version and the release notes one time. After that approval, the skill creates the GitHub Release, and CI does the rest. Nobody signs in to App Store Connect or the Play Console.

## Pipeline

Creating a GitHub Release with a new `vX.Y.Z` tag pushes that tag. The tag push starts `publish.yml`:

| Job | What it does |
|---|---|
| `publish-android` | Signs the AAB and sends it to the Google Play **production** track. Google reviews it, then it goes live. |
| `publish-ios` | Signs the IPA and uploads it to TestFlight. |
| `submit-app-store` | Runs after `publish-ios`, only on a tag push. It waits until Apple processes the build, then creates the App Store version, attaches the build, sets "What's New" from the **GitHub Release body**, and submits for App Review. The version goes live when Apple approves it. |

- The **tag** is the version. `publish.yml` gives the version to MAUI. Do not edit `ApplicationDisplayVersion` in the csproj.
- The **build number** is `1000 + run_number` of the `publish.yml` run. It is the iOS `CFBundleVersion` and the Android `versionCode`.
- `submit-app-store` uses fastlane (`fastlane/Fastfile`, lane `ios appstore_release`) and the App Store Connect API key in the `ASC_KEY_ID`, `ASC_ISSUER_ID` and `ASC_KEY_P8` repository secrets.
- A manual `publish.yml` dispatch (a TestFlight test build) does not run `submit-app-store`.
- Google Play gets **no release notes**. The r0adkll action in `publish.yml` sends none.

## Rules

- **Ed approves one time, then you do all of it.** Before you create the release, show Ed the version and the exact release notes. Wait for a clear yes.
- **Creating the release is the ship decision.** It sends Android to Google Play production and iOS to App Review with no further step. Thus the approval must come first.
- **Target the `origin/main` SHA**, not the local checkout. The local branch can be older than the last merge.
- **Do not commit the release notes.** The GitHub Release body is the only record, and the App Store job reads it from there.
- If a step fails, stop and tell Ed. Do not delete a tag or release, recreate it, or re-run a store job without his OK.

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

Write **one** block of text to `<scratchpad>/notes.txt`. It becomes the GitHub Release body and the App Store "What's New".

- Keep it at **500 characters or fewer**. Check with `wc -m < <scratchpad>/notes.txt`.
- Include only changes that a user can see. Examples: new features, fixed crashes, visible UI fixes. Leave out package updates, CI work, refactors and tests.
- Put related commits together in one line. Say what the user gets, not what the code does.
- **Plain text only. No markdown.** The App Store shows the text as it is, so `##` or `**` would show up in "What's New".

**Approval gate:** show Ed the version, the release title, and the **exact** `notes.txt` text, not a summary or a file path. Make his changes and show it again until he says yes. Do not skip this gate.

## 3. Create the GitHub Release

Do this only after Ed says yes. The title follows the earlier releases: `vX.Y.Z - <short summary>`.

```bash
SHA=$(git rev-parse origin/main)
gh release create vX.Y.Z --target $SHA --title "vX.Y.Z - <short summary>" --notes-file <scratchpad>/notes.txt
```

This creates the tag at `$SHA`, and the tag push starts `publish.yml`.

## 4. Watch the run

```bash
RUN=$(gh run list --workflow=publish.yml --event push --limit 1 --json databaseId,headBranch --jq '.[] | select(.headBranch=="vX.Y.Z") | .databaseId')
gh run watch $RUN --exit-status --interval 60            # about 45 minutes; run it in the background
gh run view $RUN --log | grep -m1 -o 'ApplicationVersion=[0-9]*'    # the build number
```

- The run can take a minute to appear after the release is created.
- `submit-app-store` starts after `publish-ios` and waits up to 60 minutes for Apple to process the build.
- After the run, `python .claude/skills/mobile-release/asc_status.py` shows the new version as `WAITING_FOR_REVIEW`.
- If a job fails, get the log (`gh run view $RUN --log-failed`), find the cause, and tell Ed. To retry only the failed jobs after a fix: `gh run rerun $RUN --failed`. Android can be on Google Play already if only an iOS job failed.

## Report

Tell Ed:

- The version and the build number.
- The GitHub Release URL.
- The `publish.yml` run URL and the result of each job: Android sent to Google Play production, iOS uploaded to TestFlight, iOS submitted for App Review. It goes live when Apple approves it.

## If a step fails

- **`submit-app-store` cannot find the build:** Apple is still processing it, or the build number is wrong. Check with `asc_status.py`, then re-run the failed job.
- **401 from App Store Connect:** an `ASC_*` secret is wrong. `ASC_KEY_P8` must be the base64 of the `.p8` file. `ASC_ISSUER_ID` is the plain issuer UUID.
- **No GitHub Release body:** the tag was pushed with no release. Create the release for the tag with notes, then re-run the failed job.
- **The version already exists as READY_FOR_SALE:** that version shipped already. Use a higher version.
- If the job cannot do a step, Ed can do the same step by hand in App Store Connect.
