/**
 * Diagnose the backup in isolation: `npm run backuptest`
 *
 * Backup is the one subsystem whose failure you discover at the worst possible
 * moment, so it gets a command that proves it end to end — including the push,
 * which is the part that actually fails (a token that expired, a repo renamed,
 * a fine-grained grant that never included Contents: write).
 */
import { backup, backupReady, backupDir } from "./backup.js";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import fs from "node:fs";
import path from "node:path";

const run = promisify(execFile);
const DIR = backupDir();

console.log(`local repo : ${DIR}`);
console.log(`remote     : ${backupReady()
  ? "configured (BACKUP_GIT_REMOTE is set)"
  : "NOT SET — memory exists on this disk only"}`);

const err = await backup("backuptest");
if (err) {
  console.log("\nRESULT: FAILED");
  console.log(`  ${err}`);
  if (/Authentication|403|401/i.test(err)) {
    console.log("  cause   : the token is wrong, expired, or lacks Contents: write on that repo");
  } else if (/could not read Username|terminal prompts disabled/i.test(err)) {
    console.log("  cause   : the remote URL has no token in it — it must be");
    console.log("            https://<token>@github.com/<you>/<repo>.git");
  } else if (/not found|does not appear to be a git repository/i.test(err)) {
    console.log("  cause   : that repository does not exist, or the token cannot see it");
  }
  process.exit(1);
}

if (fs.existsSync(path.join(DIR, ".git"))) {
  const { stdout } = await run("git", ["log", "--oneline", "-5"], { cwd: DIR });
  console.log(`\ncommits    :\n${stdout.trim().split("\n").map((l) => `  ${l}`).join("\n") || "  (none)"}`);
  const files = await run("git", ["ls-files"], { cwd: DIR });
  console.log(`tracked    : ${files.stdout.trim().split("\n").filter(Boolean).length} file(s)`);
}

console.log(backupReady()
  ? "\nRESULT: memory is backed up off this machine."
  : "\nRESULT: local history is working, but there is still NO off-machine copy."
    + "\n        This is not a backup yet. See docs/backup.md.");
