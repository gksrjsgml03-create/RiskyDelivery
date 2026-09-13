# Project workflow

- The user directs product scope and makes final product decisions. Implement the requested next development step autonomously, including routine fixes, tests, local builds, documentation, commits, and pushes to the existing GitHub remote.
- Standing user authorization includes committing and pushing completed, reviewed changes. Do not ask for conversational confirmation for those routine actions. Tool sandbox permissions still apply.
- Run the relevant compile, player integration, and visual checks for gameplay changes. Fix failures before committing. Report build or account limitations accurately; an existing-player script check is not a fresh Unity build.
- Keep generated builds, logs, credentials, and local validation artifacts out of Git. Preserve unrelated user changes.
- Future deployment automation (such as GitHub Actions) is planned; implement deployment when the user requests that stage. Do not publish a release solely because a chapter was completed.
