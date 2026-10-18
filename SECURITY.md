# Security Policy

## Supported versions

Security fixes are applied to the latest release on the default branch. Older tags may not receive backports unless announced otherwise.

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Use **GitHub Private Vulnerability Reporting** on this repository:

1. Open the repository on GitHub.
2. Go to **Security** → **Advisories** → **Report a vulnerability** (or use the “Report a vulnerability” button if enabled).
3. Include impact, reproduction steps, affected versions/commits, and any suggested fix.

If Private Vulnerability Reporting is not yet enabled on the published fork, enable it under repository **Settings → Security → Code security**, then submit the report there.

We will acknowledge reports as soon as practical and coordinate disclosure after a fix is available.

## Scope notes

- Treat cookie files, auth headers, and user download history as sensitive.
- Do not include production secrets or personal credentials in issues, PRs, or sample fixtures.
