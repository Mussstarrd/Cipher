/** Diagnose the mailbox in isolation: `npm run mailtest` */
import { ImapFlow } from "imapflow";

const USER = process.env.GMAIL_USER;
const PASS = (process.env.GMAIL_APP_PASSWORD || "").replace(/\s+/g, "");

if (!USER || !PASS) {
  console.log("FAIL: GMAIL_USER or GMAIL_APP_PASSWORD missing from .env");
  process.exit(1);
}
console.log(`user     : ${USER}`);
console.log(`password : ${PASS.length} chars after stripping spaces (expect 16)`);

const client = new ImapFlow({
  host: "imap.gmail.com", port: 993, secure: true,
  auth: { user: USER, pass: PASS }, logger: false,
});

try {
  await client.connect();
  console.log("connect  : OK");
  const lock = await client.getMailboxLock("INBOX");
  console.log(`INBOX    : ${client.mailbox.exists} message(s)`);
  if (client.mailbox.exists > 0) {
    let n = 0;
    for await (const m of client.fetch({ uid: "1:*" }, { uid: true, envelope: true })) {
      if (n++ < 3) console.log(`  uid ${m.uid} — ${m.envelope?.subject || "(no subject)"}`);
    }
    console.log(`fetch    : OK, read ${n}`);
  } else {
    console.log("fetch    : skipped, mailbox is empty");
  }
  lock.release();
  await client.logout();
  console.log("\nRESULT: mail is working.");
} catch (e) {
  console.log("\nRESULT: FAILED");
  console.log(`  message : ${e?.message}`);
  if (e?.responseText) console.log(`  server  : ${e.responseText}`);
  if (e?.authenticationFailed) console.log("  cause   : authentication rejected — regenerate the app password");
  process.exit(1);
}
