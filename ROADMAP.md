# SkillMatch — Roadmap

SkillMatch is a .NET console application that takes the text of a job vacancy, extracts the skills it mentions, groups them into categories, and compares them with your own skills list. The result is a match report: matched skills, missing skills, and an overall match score.

---

## Goals

- Accept vacancy text from a file, from stdin (paste or pipe), or from a command-line argument.
- Extract skills, including tricky tokens such as `C#`, `.NET`, `ASP.NET Core`, `CI/CD` and `Node.js`.
- Sort the skills into categories (Languages, Frameworks, Databases, Cloud, DevOps, Tools, Methodologies, Soft Skills).
- Tell required skills apart from nice-to-have ones.
- Compare the result with the user's skills profile and print a clear report.
- Keep the code small, testable, and easy to extend, for example with an LLM-based parser later.

## Tech Stack

| Concern        | Choice                                   |
|----------------|------------------------------------------|
| Runtime        | .NET 8 (LTS) or later                    |
| CLI parsing    | `System.CommandLine`                     |
| Console output | `Spectre.Console` (tables, colors)       |
| Config / data  | JSON (`System.Text.Json`)                |
| DI / hosting   | `Microsoft.Extensions.Hosting` (optional)|
| Tests          | xUnit + FluentAssertions                 |

## Target Solution Structure

```
SkillMatch/
├── src/
│   ├── SkillMatch.Cli/          # Entry point, commands, console rendering
│   └── SkillMatch.Core/         # Domain models, parsing, matching (no I/O)
├── tests/
│   └── SkillMatch.Core.Tests/
├── data/
│   ├── skills-taxonomy.json     # Known skills + aliases + categories
│   └── my-skills.json           # Your personal skills profile
├── samples/
│   └── vacancies/               # Sample vacancy texts for testing
├── ROADMAP.md
└── README.md
```

---

## Phase 0 — Project Setup

- [x] Initialize the git repo and add `.gitignore` (`dotnet new gitignore`).
- [x] Create the solution and projects:
   ```bash
   dotnet new sln -n SkillMatch
   dotnet new console  -n SkillMatch.Cli        -o src/SkillMatch.Cli
   dotnet new classlib -n SkillMatch.Core       -o src/SkillMatch.Core
   dotnet new xunit    -n SkillMatch.Core.Tests -o tests/SkillMatch.Core.Tests
   dotnet sln add src/**/*.csproj tests/**/*.csproj
   dotnet add src/SkillMatch.Cli reference src/SkillMatch.Core
   dotnet add tests/SkillMatch.Core.Tests reference src/SkillMatch.Core
   ```
- [x] Turn on `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (you can put them in `Directory.Build.props`).
- [x] Add a minimal `README.md`.

**Done when:** `dotnet build` and `dotnet test` pass on an empty solution.

---

## Phase 1 — Domain Model (`SkillMatch.Core`)

- [x] `SkillCategory`, an enum: `Language`, `Framework`, `Database`, `Cloud`, `DevOps`, `Tool`, `Methodology`, `SoftSkill`, `Other`.
- [x] `SkillDefinition`, a record: `Name` (canonical), `Category`, `Aliases` (string list).
- [x] `ExtractedSkill`, a record: `Definition`, `Importance` (`Required` / `NiceToHave`), `Occurrences`.
- [x] `UserSkill`, a record: `Name`, `Level` (1–5 or `Beginner/Intermediate/Advanced/Expert`), `YearsOfExperience` (optional).
- [x] `MatchResult`, a record: `Matched`, `Missing`, `Extra` (skills you have that the vacancy doesn't ask for), `Score`, grouped by category.

**Done when:** the models compile and have no dependencies on I/O.

---

## Phase 2 — Skills Taxonomy

- [x] Create `data/skills-taxonomy.json` with about 100–150 common skills to start:
   ```json
   [
     { "name": "C#",           "category": "Language",  "aliases": ["c#", "csharp", "c sharp"] },
     { "name": ".NET",         "category": "Framework", "aliases": [".net", "dotnet", ".net core", ".net 8"] },
     { "name": "ASP.NET Core", "category": "Framework", "aliases": ["asp.net core", "asp.net", "web api"] },
     { "name": "PostgreSQL",   "category": "Database",  "aliases": ["postgres", "postgresql", "psql"] },
     { "name": "Docker",       "category": "DevOps",    "aliases": ["docker", "containers"] }
   ]
   ```
- [x] Add `ITaxonomyProvider` with a `JsonTaxonomyProvider` implementation.
- [x] Check the data at load time: no duplicate canonical names, and no alias that points to two different skills.

**Done when:** the taxonomy loads, and unit tests cover the validation rules.

---

## Phase 3 — Vacancy Input

- [ ] Add an `IVacancySource` abstraction with these implementations:
   - `FileVacancySource`: `--file vacancy.txt`
   - `StdinVacancySource`: piped or pasted input (end with `Ctrl+D` / `Ctrl+Z`)
   - `ArgumentVacancySource`: `--text "..."`
- [ ] Normalize the text: unify line endings, remove HTML tags if present, collapse whitespace.

**Done when:** all three input modes give the same normalized text for the same vacancy.

---

## Phase 4 — Skill Extraction (Parser)

- [ ] Define the `ISkillExtractor` interface: `IReadOnlyList<ExtractedSkill> Extract(string text)`.
- [ ] Build `DictionarySkillExtractor` (rule-based):
   - Build one compiled regex per alias, with custom word boundaries so that `C#`, `.NET`, `C++` and `Node.js` match correctly: `(?<![\w.#+])alias(?![\w#+])`.
   - Match case-insensitively, and try longer aliases first so that `ASP.NET Core` wins over `.NET`.
   - Remove duplicates by canonical name and count the occurrences.
- [ ] **Section detection** (required vs nice-to-have):
   - Split the text by headings such as *Requirements*, *Must have*, *Qualifications* → `Required`.
   - *Nice to have*, *Plus*, *Bonus*, *Preferred* → `NiceToHave`.
   - Fall back to `Required` when no section is found.
- [ ] Group the extracted skills by `SkillCategory`.

**Done when:** tests on 3–5 real vacancy samples in `samples/vacancies/` give the expected skill sets.

---

## Phase 5 — User Skills Profile

- [ ] Create `data/my-skills.json`:
   ```json
   {
     "skills": [
       { "name": "C#",         "level": "Expert",       "years": 6 },
       { "name": "ASP.NET Core","level": "Advanced",    "years": 5 },
       { "name": "PostgreSQL", "level": "Intermediate", "years": 3 }
     ]
   }
   ```
- [ ] Add `IUserProfileProvider` with a JSON implementation.
- [ ] Map each user skill to its canonical name through the taxonomy aliases, so that `postgres` matches `PostgreSQL`.
- [ ] Warn about user skills that are not in the taxonomy.

**Done when:** the profile loads and its skill names are mapped to canonical names.

---

## Phase 6 — Matching & Scoring

- [ ] Create `SkillMatcher.Compare(extracted, profile) → MatchResult`.
- [ ] Calculate:
   - **Matched**: in both the vacancy and the profile.
   - **Missing**: in the vacancy but not in the profile, split into required and nice-to-have.
   - **Extra**: in the profile but not in the vacancy (informational only).
- [ ] Score formula (first version):
   - Weight: Required = 1.0, NiceToHave = 0.5
   - `Score = Σ weight(matched) / Σ weight(all extracted) × 100%`
- [ ] Add a per-category breakdown, for example *Languages 100%, Cloud 33%*.

**Done when:** the score is deterministic and edge cases are tested (empty vacancy, empty profile, 100% match).

---

## Phase 7 — CLI & Report Output

- [ ] Build commands with `System.CommandLine`:
   ```bash
   skillmatch analyze --file vacancy.txt [--profile my-skills.json] [--format table|json|markdown]
   skillmatch skills list            # show your profile
   skillmatch skills add "Kubernetes" --level Beginner
   skillmatch taxonomy search "kube" # look up known skills/aliases
   ```
- [ ] Render the report with Spectre.Console:
   - A header with the overall match score, colored by threshold (green ≥ 75%, yellow ≥ 50%, red below that).
   - A table grouped by category with ✅ matched / ❌ missing / ➕ extra.
   - A "What to learn next" list: the missing required skills, ordered by how often the vacancy mentions them.
- [ ] Add `--format json` and `--format markdown` for saving or sharing reports.
- [ ] Return exit codes (`0` success, `1` invalid input, `2` file not found).

**Done when:** a full run against a sample vacancy prints a readable report.

---

## Phase 8 — Testing & Quality

- [ ] Unit tests for the normalizer, the extractor (edge tokens such as `C#`, `.NET`, `C++`, `Go` vs the word "go"), section detection, and the matcher.
- [ ] Golden-file tests: `samples/vacancies/*.txt` → expected `*.expected.json`.
- [ ] Ambiguous-word handling: add a `requiresContext` flag or a stop-list for skills like `Go`, `R` and `Rust` that are also common words.
- [ ] Add a GitHub Actions workflow that runs `dotnet build` + `dotnet test` on each push.

**Done when:** code coverage of `SkillMatch.Core` is at least 80% and CI is green.

---

## Phase 9 — Packaging & Distribution

- [ ] Package as a .NET global tool (`<PackAsTool>true</PackAsTool>`, `ToolCommandName=skillmatch`).
- [ ] Optionally publish a single-file self-contained binary: `dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true`.
- [ ] Store the default profile in a user config directory (`~/.skillmatch/`) instead of the repo.
- [ ] Finish the README with installation, usage examples, and a screenshot.

**Done when:** `dotnet tool install -g --add-source ./nupkg SkillMatch` works and `skillmatch analyze` runs from any directory.

---

## Phase 10 — Future Enhancements (Backlog)

- [ ] **LLM-based extractor**: an `LlmSkillExtractor : ISkillExtractor` that calls the Claude API to extract skills from free text and categorize them. It can find skills that are not in the taxonomy. You can run it together with the dictionary extractor and merge the results.
- [ ] **Fuzzy matching**: catch typos and variants with Levenshtein distance or Jaro-Winkler.
- [ ] **Seniority & years detection**: parse phrases like "5+ years of C#" and compare them with `YearsOfExperience` in the profile.
- [ ] **Level-aware scoring**: a partial score when your level is below what the vacancy implies.
- [ ] **Skill relationships**: implied skills, for example ASP.NET Core implies C# and .NET, and EF Core implies an ORM.
- [ ] **URL input**: fetch a vacancy straight from a job-board link.
- [ ] **Batch mode**: rank several vacancies against your profile.
- [ ] **History**: save analyses (SQLite) and track which skills come up most often across vacancies.
- [ ] **Taxonomy auto-growth**: suggest unknown capitalized terms as candidate skills.

---

## Milestone Summary

| Milestone | Phases | Outcome                                          |
|-----------|--------|--------------------------------------------------|
| **M1 — MVP**        | 0–6 | Core logic works and is covered by tests  |
| **M2 — Usable CLI** | 7   | You can analyze a real vacancy from the terminal |
| **M3 — Polished**   | 8–9 | CI, packaging, installable global tool           |
| **M4 — Smart**      | 10  | LLM extraction, fuzzy matching, analytics        |
