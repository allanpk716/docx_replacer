/**
 * /release — Evaluate recent changes, recommend stable or beta, and publish.
 *
 * Supports three targets: internal server, GitHub, or both.
 * Placed in .gsd/extensions/ for project-local scope.
 *
 * Interaction flow:
 *   1. Show change analysis + recommendation
 *   2. Ask: stable or beta?
 *   3. Ask: internal / github / both?
 *   4. Compute next version (user confirms / edits)
 *   5. Send agent prompt to execute
 *
 * Version logic:
 *   - beta→beta:   only increment N in -betaN, never touch x.x.x
 *   - stable→beta: bump x.x.x + "-beta1"
 *   - beta→stable: strip -betaN suffix, keep same x.x.x
 *   - stable→stable: bump minor (feat) or patch (fix)
 */
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";

// Server host/port read from env vars (UPDATE_SERVER_HOST, UPDATE_SERVER_PORT)
// or fall back to sensible defaults for the internal network.

export default function releaseCommand(pi) {
  pi.registerCommand("release", {
    description:
      "Evaluate recent changes and publish a release to internal server and/or GitHub",

    async handler(args, ctx) {
      // ── Helpers ──────────────────────────────────────────────────────────
      const run = async (cmd, cmdArgs) => {
        const r = await pi.exec(cmd, cmdArgs);
        return { ...r, stdout: (r.stdout || "").trim() };
      };
      const fail = (msg) => ctx.ui.notify(msg, "error");

      // Read internal server config from environment
      const serverHost = process.env.UPDATE_SERVER_HOST || "";
      const serverPort = process.env.UPDATE_SERVER_PORT || "";

      // ── Step 1: Detect project root ──────────────────────────────────────
      const rootResult = await run("git", ["rev-parse", "--show-toplevel"]);
      const rootDir = rootResult.stdout;
      if (!rootDir) { fail("Not inside a git repository."); return; }

      // ── Step 2: Get current version from csproj ──────────────────────────
      let currentVersion = "";
      try {
        const entries = readdirSync(rootDir);
        const csprojFile = entries.find((f) => f.endsWith(".csproj"));
        if (csprojFile) {
          const content = readFileSync(join(rootDir, csprojFile), "utf-8");
          const m = content.match(/<Version>([^<]+)<\/Version>/);
          if (m) currentVersion = m[1];
        }
      } catch { /* fall through */ }

      if (!currentVersion) { fail("Could not detect current version from .csproj <Version>."); return; }

      // ── Step 3: Get last tag and commits since ───────────────────────────
      const tagResult = await run("git", ["-C", rootDir, "describe", "--tags", "--abbrev=0"]);
      const lastTag = tagResult.stdout;

      let logOutput = "";
      if (lastTag) {
        const r = await run("git", ["-C", rootDir, "log", "--oneline", lastTag + "..HEAD"]);
        logOutput = r.stdout;
      } else {
        const r = await run("git", ["-C", rootDir, "log", "--oneline", "-20"]);
        logOutput = r.stdout;
      }

      if (!logOutput || logOutput.split("\n").filter(Boolean).length === 0) {
        ctx.ui.notify("No new commits since last tag. Nothing to release.", "info");
        return;
      }

      const commits = logOutput.split("\n").filter(Boolean).map((l) => l.trim());

      // ── Step 4: Analyse changes and recommend ────────────────────────────
      const logLower = logOutput.toLowerCase();
      const hasFeature = /\bfeat\b|\bfeature\b|\u65b0\u589e|\u5b9e\u73b0|\u6dfb\u52a0|\u4e3b\u754c\u9762|\u91cd\u6784/.test(logLower);
      const hasFix = /\bfix\b|\bbug\b|\bpatch\b|\u4fee\u590d|\u4fee\u6b63/.test(logLower);
      const hasBreaking = /\bbreaking\b|\bremove\b|\u5220\u9664|\u79fb\u9664|\u6e05\u7406/.test(logLower);

      const featCommits = commits.filter((c) => /^[\da-f]+\s+feat/i.test(c));

      let recommendation = "beta";
      let rationale = "";

      if (hasBreaking) {
        recommendation = "beta";
        rationale = "\u5305\u542b\u7834\u574f\u6027\u53d8\u66f4\u6216\u5220\u9664\u64cd\u4f5c \u2014 \u5efa\u8bae\u5148\u53d1 beta \u5185\u6d4b\u3002";
      } else if (hasFeature || featCommits.length > 0) {
        recommendation = "beta";
        rationale = "\u5305\u542b\u65b0\u529f\u80fd (" + featCommits.length + " \u4e2a feat \u63d0\u4ea4) \u2014 \u5efa\u8bae\u5148\u53d1 beta \u9a8c\u8bc1\u3002";
      } else if (hasFix && !hasFeature && !hasBreaking) {
        recommendation = "stable";
        rationale = "\u4ec5\u5305\u542b bug \u4fee\u590d \u2014 \u53ef\u4ee5\u76f4\u63a5\u53d1 stable\u3002";
      } else {
        recommendation = "beta";
        rationale = "\u6df7\u5408\u53d8\u66f4 \u2014 \u5efa\u8bae\u5148\u53d1 beta\u3002";
      }

      // ── Step 5: Show analysis ────────────────────────────────────────────
      const commitSummary = commits.slice(0, 10).join("\n  ");
      const moreCount = Math.max(0, commits.length - 10);

      const lines = [
        "\uD83D\uDCCB **\u53d1\u5e03\u5206\u6790**",
        "",
        "**\u5f53\u524d\u7248\u672c:** " + currentVersion,
        "**\u4e0a\u4e00\u4e2a tag:** " + (lastTag || "(\u65e0)"),
        "**\u65b0\u589e\u63d0\u4ea4:** " + commits.length + " \u4e2a",
        "",
        "**\u53d8\u66f4\u5185\u5bb9:**",
        "  " + commitSummary,
      ];
      if (moreCount > 0) lines.push("  ... \u8fd8\u6709 " + moreCount + " \u4e2a");
      lines.push(
        "",
        "**\u5efa\u8bae:** " + (recommendation === "beta" ? "\uD83D\uDFE1 beta" : "\uD83D\uDFE2 stable"),
        "**\u7406\u7531:** " + rationale,
      );

      ctx.ui.notify(lines.filter(Boolean).join("\n"), "info");

      // ── Step 6: Ask channel ──────────────────────────────────────────────
      const channelLabels = {
        "beta": "\uD83D\uDFE1 beta (\u5185\u6d4b)",
        "stable": "\uD83D\uDFE2 stable (\u7a33\u5b9a)",
        "cancel": "\u53d6\u6d88",
      };
      const channel = await ctx.ui.select(
        "\u53d1\u5e03\u54ea\u4e2a\u901a\u9053\uff1f(\u5efa\u8bae: " + recommendation + ")",
        Object.values(channelLabels),
      );
      if (!channel || channel === channelLabels.cancel) {
        ctx.ui.notify("\u53d1\u5e03\u5df2\u53d6\u6d88\u3002", "info");
        return;
      }
      const isBeta = channel === channelLabels.beta;

      // ── Step 7: Ask target ───────────────────────────────────────────────
      const targetLabels = {
        "internal": "\uD83C\uDFE0 \u5185\u7f51\u670d\u52a1\u5668",
        "github": "\uD83C\uDF10 GitHub",
        "both": "\uD83C\uDFE0+\uD83C\uDF10 \u4e24\u4e2a\u90fd\u53d1",
        "cancel": "\u53d6\u6d88",
      };
      const target = await ctx.ui.select(
        "\u53d1\u5e03\u5230\u54ea\u91cc\uff1f",
        Object.values(targetLabels),
      );
      if (!target || target === targetLabels.cancel) {
        ctx.ui.notify("\u53d1\u5e03\u5df2\u53d6\u6d88\u3002", "info");
        return;
      }
      const doInternal = target === targetLabels.internal || target === targetLabels.both;
      const doGithub = target === targetLabels.github || target === targetLabels.both;

      // ── Step 8: Compute next version ──────────────────────────────────────
      const baseMatch = currentVersion.match(/^(\d+)\.(\d+)\.(\d+)/);
      if (!baseMatch) { fail("\u65e0\u6cd5\u89e3\u6790\u7248\u672c\u53f7 \"" + currentVersion + "\""); return; }

      const major = parseInt(baseMatch[1], 10);
      const minor = parseInt(baseMatch[2], 10);
      const patch = parseInt(baseMatch[3], 10);
      const isAlreadyBeta = /-beta\d+$/.test(currentVersion);

      let nextVersion;

      if (isBeta) {
        if (isAlreadyBeta) {
          // beta→beta: ONLY increment N, never touch x.x.x
          const betaMatch = currentVersion.match(/-beta(\d+)$/);
          const currentN = betaMatch ? parseInt(betaMatch[1], 10) : 0;
          nextVersion = major + "." + minor + "." + patch + "-beta" + (currentN + 1);
        } else {
          // stable→beta: bump x.x.x + "-beta1"
          if (hasFeature || featCommits.length > 0) {
            nextVersion = major + "." + (minor + 1) + ".0-beta1";
          } else {
            nextVersion = major + "." + minor + "." + (patch + 1) + "-beta1";
          }
        }
      } else {
        if (isAlreadyBeta) {
          // beta→stable: just strip the -betaN suffix
          nextVersion = major + "." + minor + "." + patch;
        } else {
          // stable→stable: bump minor (feat) or patch (fix)
          if (hasFeature || featCommits.length > 0) {
            nextVersion = major + "." + (minor + 1) + ".0";
          } else {
            nextVersion = major + "." + minor + "." + (patch + 1);
          }
        }
      }

      // ── Step 9: Confirm version ──────────────────────────────────────────
      const confirmVersion = await ctx.ui.input(
        "\u4e0b\u4e00\u4e2a\u7248\u672c\u53f7: " + nextVersion + "\uff08\u53ef\u7f16\u8f91\uff0c\u6216\u76f4\u63a5\u56de\u8f66\u786e\u8ba4\uff09",
        nextVersion,
      );
      if (!confirmVersion || !confirmVersion.trim()) {
        ctx.ui.notify("\u53d1\u5e03\u5df2\u53d6\u6d88\u3002", "info");
        return;
      }
      nextVersion = confirmVersion.trim();

      // ── Step 10: Build the agent prompt ───────────────────────────────────
      const channelName = isBeta ? "beta" : "stable";
      const targetDesc = (doInternal ? "\u5185\u7f51" : "") + (doInternal && doGithub ? " + " : "") + (doGithub ? "GitHub" : "");

      let steps = [];
      steps.push("### 1. \u66F4\u65B0\u7248\u672C\u53F7\n\u7F16\u8F91 DocuFiller.csproj \u4E2D\u7684 `<Version>` \u4E3A `" + nextVersion + "`\u3002\u5982\u679C\u662F stable \u7248\u672C\uFF0C\u786E\u4FDD `<FileVersion>` \u4E5F\u540C\u6B65\u66F4\u65B0\u3002");
      steps.push("### 2. \u6784\u5EFA\u9A8C\u8BC1\n\u8FD0\u884C `dotnet build DocuFiller.csproj` \u786E\u8BA4\u7F16\u8BD1\u901A\u8FC7\u3002");
      steps.push("### 3. \u63D0\u4EA4\u548C\u6253 Tag\n```bash\ngit add DocuFiller.csproj\ngit commit -m \"chore: bump version to " + nextVersion + "\"\ngit tag v" + nextVersion + "\n```");

      let stepNum = 4;

      if (doInternal) {
        steps.push("### " + stepNum + ". \u6784\u5EFA\u5E76\u4E0A\u4F20\u5230\u5185\u7F51\u670D\u52A1\u5668 (" + channelName + " \u901A\u9053)\n```bash\nscripts\\\\build.bat --standalone " + channelName + "\n```\n\u8FD9\u4E2A\u811A\u672C\u4F1A\u81EA\u52A8\uFF1A\n- \u8BFB\u53D6 csproj \u4E2D\u7684\u7248\u672C\u53F7\n- dotnet publish + vpk pack \u751F\u6210 Velopack \u5305\uFF08.nupkg, Setup.exe, Portable.zip\uFF09\n- \u4E0A\u4F20\u5230\u5185\u7F51 " + serverHost + ":" + serverPort + " \u7684 " + channelName + " \u901A\u9053\n\n\u4E0A\u4F20\u524D\u52A0\u8F7D token\uFF1A`source .env; export UPDATE_SERVER_API_TOKEN`\n\u786E\u8BA4\u811A\u672C\u8F93\u51FA\u5305\u542B SUCCESS\u3002");
        stepNum++;
      }

      if (doGithub) {
        steps.push("### " + stepNum + ". \u63A8\u9001\u5230 GitHub\n```bash\ngit push origin master\ngit push origin v" + nextVersion + "\n```\n\u63A8\u9001 tag \u4F1A\u81EA\u52A8\u89E6\u53D1 GitHub Actions\uFF08`.github/workflows/build-release.yml`\uFF09\uFF1A\n- \u81EA\u52A8\u6784\u5EFA + Velopack \u6253\u5305\n- \u521B\u5EFA GitHub Release\uFF08\u5305\u542B Setup.exe, Portable.zip, .nupkg\uFF09\n\n\u786E\u8BA4\u547D\u4EE4\u6267\u884C\u6210\u529F\u3002");
        stepNum++;
      }

      const prompt = [
        "\u4f60\u6b63\u5728\u6267\u884c\u4e00\u4e2a\u7248\u672c\u53d1\u5e03\u6d41\u7a0b\u3002\u8bf7\u6309\u4ee5\u4e0b\u6b65\u9aa4\u64cd\u4f5c\uff1a",
        "",
        "## \u53d1\u5e03\u4fe1\u606f",
        "- **\u53d1\u5e03\u901a\u9053:** " + channelName,
        "- **\u65b0\u7248\u672c\u53f7:** " + nextVersion,
        "- **\u4e0a\u4e00\u4e2a\u7248\u672c:** " + currentVersion,
        "- **\u53d1\u5e03\u76ee\u6807:** " + targetDesc,
        "",
        "## \u6267\u884c\u6b65\u9aa4",
        "",
        steps.join("\n\n"),
        "",
        "## \u6ce8\u610f\u4e8b\u9879",
        "- \u5982\u679c tag \u5df2\u5b58\u5728\uff08\u51b2\u7a81\uff09\uff0c\u63d0\u793a\u7528\u6237\u5e76\u505c\u6b62",
        "- \u5982\u679c\u6784\u5efa\u5931\u8d25\uff0c\u4e0d\u8981\u7ee7\u7eed\u4e0a\u4f20\u6216\u63a8\u9001",
        "- \u5982\u679c\u4e0a\u4f20\u5931\u8d25\uff0c\u62a5\u544a\u9519\u8bef\u4fe1\u606f",
        "- \u4f7f\u7528\u4e2d\u6587\u56de\u590d",
      ].join("\n");

      ctx.ui.notify(
        "\u27A1 \u5f00\u59cb\u53d1\u5e03: v" + nextVersion +
        " | " + channelName +
        " | " + targetDesc,
        "info",
      );
      pi.sendUserMessage(prompt);
    },
  });
}
