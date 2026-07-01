# Contribution Workflow

## Branching

`main` is the releasable branch and should remain buildable. Work is performed
on short-lived branches created from an up-to-date `main`.

Branch naming:

```text
feature/123-priority-scoring
fix/245-circular-dependency
docs/architecture-baseline
```

When Azure Boards work-item numbers exist, include them in branch and pull
request context.

## Work Item Flow

Work is organised as:

```text
Epic -> Feature -> User Story -> Task
```

Before implementation, a user story should contain:

- User value.
- Acceptance criteria.
- Technical notes where necessary.
- Test considerations.
- Dependencies or known risks.

## Commit Style

Use small commits with an imperative Conventional Commit message:

```text
feat: implement task completion rules
test: specify blocked task behaviour
fix: reject circular task dependencies
refactor: extract task title value object
docs: record persistence architecture decision
ci: publish test coverage
```

Do not split work only to increase the commit count. Each commit should tell
one useful part of the engineering story and keep the repository coherent.

## Pull Requests

Pull requests should be focused and should include:

- What changed and why.
- The user story or work item.
- Testing performed.
- API or UI evidence when relevant.
- Risks, migrations, or deployment considerations.

Prefer squash-free history while commits are already intentional and coherent.
Fixup commits should be cleaned before merge.

## Review Checklist

- Acceptance criteria are met.
- Business rules are outside controllers and UI code.
- Tests cover success and important failure paths.
- Names communicate domain intent.
- API and persistence models do not leak into the domain.
- Logs contain useful context without secrets.
- Configuration works across development and deployment environments.
- Documentation reflects architectural or contract changes.

## Local Quality Checks

The exact commands will expand as projects are introduced. The expected
baseline is:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Frontend checks will later include formatting, linting, tests, and production
build commands.

## Definition Of Done

A work item is done when:

- Its acceptance criteria pass.
- Required automated tests exist and pass.
- The code has been reviewed.
- Documentation and migrations are included where needed.
- The CI pipeline is green.
- The change is merged and traceable to its work item.
