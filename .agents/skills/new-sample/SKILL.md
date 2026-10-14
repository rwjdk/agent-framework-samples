---
name: new-sample
description: Create a new Microsoft Agent Framework .NET console sample in this repository using its shared library and sample conventions.
---

# New sample

Use this skill when adding a new console sample to this repository. Read the repository instructions and a nearby sample demonstrating the relevant feature before choosing the implementation.

## Required conventions

- Use Azure OpenAI unless the user specifies another provider.
- Use `gpt-6-luna` as the model unless the user specifies another model. For Azure OpenAI, use a deployment of that model; if its deployment name differs, make that configuration explicit rather than silently choosing another model.
- Put the project and its code in `src/<SampleName>/`, with `<SampleName>.csproj` and `Program.cs`.
- Never create a `README.md` (or a differently cased equivalent such as `Readme.md`) for the sample.
- Do not add comments in the sample code, including introductory, inline, block, or XML documentation comments. Use clear names and readable code; explain prerequisites and implementation choices in the response to the user.
- Create a .NET console app targeting the latest stable .NET release at the time the sample is created. Verify the current release and compare it with `global.json`, installed SDKs, and `src/Shared/Shared.csproj`. Do not interpret “latest” as a preview unless requested. If the required SDK is unavailable or incompatible with the repo, report the prerequisite rather than silently targeting an older version.
- Always reference the shared library from the sample project:

  ```xml
  <ProjectReference Include="..\Shared\Shared.csproj" />
  ```

- Use top-level statements in `Program.cs`. Its first executable statement must be `Utils.Init("<sample name>");`, with the actual sample name substituted. `using` directives and necessary compiler directives may precede it:

  ```csharp
  using Shared;

  Utils.Init("<sample name>");
  ```

- When using AgentFrameworkToolkit with Azure OpenAI or OpenAI, explicitly configure the agent options with `ClientType = ClientType.ResponsesApi`; do not rely on the toolkit's default. Use the API shape supported by the repository's package version. If an explicitly requested provider does not support Responses API, explain the incompatibility instead of inventing a setting.

## Repository integration

Reuse `Shared.ClientHelper.GetAzureOpenAIClient()` for ordinary Azure OpenAI client setup and `Shared.SecretsManager` for credentials when constructing toolkit factories. Inspect these helpers before using them; do not duplicate endpoint handling or embed credentials in source code.

Follow the repository's central package management: add versionless `PackageReference` entries to the sample project and reuse versions from `Directory.Packages.props`. Add a central version only when a required package is missing. Keep `ImplicitUsings` and `Nullable` enabled, as in existing console samples.

Add the new project to `Samples.slnx` in the relevant solution folder. Preserve existing entries and unrelated work. Build the new project and report the result, including any missing SDK, restore, or configuration prerequisites. Distinguish a successful build from a live model call.

## Suggested conventions (optional)

These are recommendations, not additional mandatory rules:

- Keep each sample focused on one feature and use a descriptive name consistent across its directory, project, and initialization title.
- Include deployment or service prerequisites and a clear run command in the response to the user when needed.
- Reuse `Shared.Utils` for console formatting and separators so output looks consistent across samples.
- Keep dependencies minimal and use small, readable inputs that make the demonstrated behavior easy to observe.
- Explain any intentional departure from the default provider, model, or API in the response to the user.
