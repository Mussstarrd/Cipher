/**
 * Backing up the only thing that is irreplaceable: what Hearth has learned.
 *
 * The droplet can be rebuilt from the repo in ten minutes. The API key can be
 * reissued. Memory cannot — it is months of watching one family, and it exists
 * on exactly one disk unless something copies it off.
 *
 * So: a separate PRIVATE git repo, pushed after every wake. Git rather than a
 * disk snapshot because history is the point — you can see what Hearth believed
 * last Tuesday, and roll back a bad inference instead of restoring a whole box.
 *
 * Deliberately a second repo, not the code repo. The code is public; the
 * family's address, school, medical notes and inbox never can be.
 */
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import fs from "node:fs";
import path from "node:path";
import { MEM, DATA } from "./memory.js";

const run = promisify(execFile);
const DIR = process.env.BACKUP_DIR || "/opt/hearth-backup";
const REMOTE = process.env.BACKUP_GIT_REMOTE || "";

/** True only when there is somewhere OFF this machine to push to. */
export const backupReady = () => Boolean(REMOTE);

/** Where the local history lives, for anything that needs to say so. */
export const backupDir = () => DIR;

const git = (args, cwd = DIR) =>
  run("git", args, {
    cwd, timeout: 60_000, maxBuffer: 4 << 20,
    // Without this a remote with a stale token makes git sit waiting for a
    // username on a terminal that does not exist, and the backup "hangs" for
    // the full timeout instead of failing with the reason.
    env: { ...process.env, GIT_TERMINAL_PROMPT: "0", GIT_ASKPASS: "" },
  });

async function ensureRepo() {
  if (!fs.existsSync(path.join(DIR, ".git"))) {
    fs.mkdirSync(DIR, { recursive: true });
    await git(["init", "-q", "-b", "main"]);
    await git(["config", "user.email", "hearth@local"]);
    await git(["config", "user.name", "Hearth"]);
    fs.writeFileSync(path.join(DIR, "README.md"),
      "# Hearth memory\n\nAutomatic backup. Private — contains household data.\n");
  }
  // Always reset the remote: the token in it may have been rotated.
  await git(["remote", "remove", "backup"]).catch(() => {});
  if (REMOTE) await git(["remote", "add", "backup", REMOTE]);
}

function copyDir(from, to) {
  if (!fs.existsSync(from)) return;
  fs.mkdirSync(to, { recursive: true });
  for (const e of fs.readdirSync(from, { withFileTypes: true })) {
    const a = path.join(from, e.name), b = path.join(to, e.name);
    if (e.isDirectory()) copyDir(a, b);
    else if (e.isFile()) fs.copyFileSync(a, b);
  }
}

/**
 * Copy memory and channel state into the backup repo and push.
 * Returns null on success, or a message worth telling a human about.
 */
export async function backup(note = "") {
  try {
    await ensureRepo();
    copyDir(MEM, path.join(DIR, "memory"));

    // Channel history, minus push subscriptions — those are device tokens, are
    // useless on a restored machine, and do not belong in a backup.
    const statePath = path.join(DATA, "state.json");
    if (fs.existsSync(statePath)) {
      const s = JSON.parse(fs.readFileSync(statePath, "utf8"));
      delete s.subs;
      fs.writeFileSync(path.join(DIR, "state.json"), JSON.stringify(s, null, 1));
    }

    await git(["add", "-A"]);
    const { stdout } = await git(["status", "--porcelain"]);
    if (!stdout.trim()) return null;                 // nothing changed; not a failure

    const stamp = new Date().toISOString().replace("T", " ").slice(0, 16);
    await git(["commit", "-q", "-m", `memory ${stamp}${note ? ` — ${note}` : ""}`]);

    // No remote yet: commit anyway. Local history is not a backup — one disk
    // failure still takes everything — but it is the defence against the more
    // likely accident, which is the 22:00 review rewriting a memory layer
    // badly. With history you roll back one file; without it, it is just gone.
    // And when a remote is finally set, every commit made up to then pushes.
    if (!REMOTE) return null;

    await git(["push", "-q", "--set-upstream", "backup", "main"]);
    return null;
  } catch (e) {
    // A silent backup failure is the worst kind — you find out when you need it.
    return String(e?.stderr || e?.message || e).slice(0, 400);
  }
}
