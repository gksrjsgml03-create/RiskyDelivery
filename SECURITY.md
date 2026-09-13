# Security

Do not report passwords, tokens, personal information, or exploitable details in public issues.
Use this repository's **Security → Report a vulnerability** private reporting feature.

`Tools/Test-Secrets.ps1` checks all fetched Git history and publishable working files with a pinned, checksum-verified Gitleaks release. Reports are redacted and kept in ignored `Logs/Security`. GitHub Actions repeats this check on pushes, pull requests, and weekly. Release publication requires a passing scan.

The game does not implement account login, payment, analytics, or application-level network requests. It stores chapter stars, times, and sound preferences locally. Do not attach unredacted player logs to public reports: operating-system logs may contain local paths or machine details.

Secret scanning is pattern-based: a passing scan does not prove that every personal datum, unknown credential format, dependency vulnerability, or game bug is absent. Unity and GitHub Actions updates still require review. If a real secret is discovered, revoke/rotate it first; deleting the current file does not remove it from Git history.
