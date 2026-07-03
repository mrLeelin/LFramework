# LFramework handoff wiki

This wiki is the handoff entry for `Assets/Frame/Framework`.

Open the HTML version:

- `Framework/Wiki/html/index.html`

Maintain the source pages:

- `docs/00-overview.md`
- `docs/01-startup-flow.md`
- `docs/02-configuration.md`
- `docs/03-runtime-systems.md`
- `docs/04-resource-and-build.md`
- `docs/05-newcomer-checklist.md`
- `docs/06-worldhelper-guide.md`

Generated class index:

- `html/module-tree.html`
- `html/worldhelper-guide.html`
- `html/class-index.html`
- `html/class-index-data.js`
- refresh with `tools/generate-class-index.ps1`

The important path starts from `LSystemApplication`, then follows hotfix
components, procedures, providers, worlds, and world helpers until game logic
enters its active world.

The HTML pages intentionally mirror the Markdown pages, but they are split into
multiple files so each module can be maintained independently.
