# Release requirements

- Every game update must include a Windows release and an Intel macOS DMG. This is the user's standing release preference.
- Test the installed builds and review screenshots before publishing. Keep macOS downloads in a separate release, and keep the Windows release as GitHub's latest release.
- Preserve player decks, accounts, settings and progression. Never stop a user's game or interrupt active friend duels to install or deploy an update.
- Keep unrelated working-tree changes out of release commits.
- Keep release titles short: `VERSION [Windows]`, `VERSION [macOS Intel]`, or `VERSION [macOS Silicon]`. Include platform/processor tags and clear asset labels. Update DOWNLOADS.md in version order after publishing. Preserve existing release tags and download URLs.
