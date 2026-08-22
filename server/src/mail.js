/**
 * Mail, the simple way: IMAP in, SMTP out, one Gmail app password.
 *
 * No OAuth, no Google Cloud project, no verification, nothing to apply for.
 * Turn on 2FA, generate a 16-character app password, done. For one household
 * this is strictly better than OAuth — nothing expires, nothing gets revoked by
 * a policy change, and there is no consent screen to re-approve.
 */
import { ImapFlow } from "imapflow";
import nodemailer from "nodemailer";
import { simpleParser } from "mailparser";

const USER = process.env.GMAIL_USER;
// Google shows app passwords as four groups of four. People paste the spaces;
// SMTP does not want them. Strip rather than fail with "invalid credentials".
const PASS = (process.env.GMAIL_APP_PASSWORD || "").replace(/\s+/g, "");

export const mailReady = () => Boolean(USER && PASS);

/**
 * A Gmail app password is always exactly 16 lowercase letters. Anything else
 * means a character was lost or added in transit, and Google will answer with
 * a flat "Invalid credentials" that tells you nothing about which of the two
 * dozen possible causes it was. Catch it here instead.
 */
export function credentialWarning() {
  if (!USER || !PASS) return null;
  if (PASS.length !== 16) {
    return `GMAIL_APP_PASSWORD is ${PASS.length} characters after removing spaces; a Gmail app password is always 16. A character was lost or added when it was pasted — retype it as one block with no spaces.`;
  }
  if (!/^[a-z]{16}$/.test(PASS)) {
    return "GMAIL_APP_PASSWORD contains something other than lowercase letters. Google issues only a-z — check for a stray character from the paste.";
  }
  return null;
}

/** Noise that should never reach a family brief. */
const JUNK = [
  /no[-_.]?reply@.*(kohls|walmart|amazon|adobe|linkedin|upstart|cymatics)/i,
  /(unsubscribe|newsletter|promo|deal of the day|% off)/i,
];
const isJunk = (m) =>
  JUNK.some((r) => r.test(m.from || "") || r.test(m.subject || ""));

/**
 * Everything new since the last check. Tracks the highest UID seen so a
 * restart never re-reads the inbox and never skips a message.
 */
export async function fetchNew(sinceUid = 0, max = 40) {
  if (!mailReady()) return { messages: [], lastUid: sinceUid, error: "no credentials" };
  const client = new ImapFlow({
    host: "imap.gmail.com", port: 993, secure: true,
    auth: { user: USER, pass: PASS }, logger: false,
    // Bound every stage. Without these a half-open socket sits for minutes
    // before anything notices, which is exactly how the crash below happened.
    connectionTimeout: 20_000,
    greetingTimeout: 20_000,
    socketTimeout: 90_000,
  });

  // ImapFlow is an EventEmitter, and a dropped connection arrives as an 'error'
  // EVENT, not a rejected promise — often minutes after the call that caused it
  // has already returned. In Node an 'error' event with no listener is a hard
  // process crash, so the try/catch below could never have caught it: the
  // service died five minutes after each failed fetch, in a restart loop whose
  // stack trace pointed at a timer and looked nothing like a mail problem.
  // This one listener is the entire fix. Keep it.
  client.on("error", (e) => {
    console.error(`[hearth] imap connection error (handled): ${e?.message || e}`);
  });

  const out = [];
  let lastUid = sinceUid;
  try {
    await client.connect();
    const lock = await client.getMailboxLock("INBOX");
    try {
      // A brand-new or freshly-emptied mailbox has nothing to fetch, and asking
      // for a UID range against zero messages is an IMAP error, not an empty
      // result. Check before asking.
      if (!client.mailbox || client.mailbox.exists === 0) {
        return { messages: [], lastUid: sinceUid, error: null };
      }
      const range = sinceUid > 0 ? `${sinceUid + 1}:*` : "1:*";
      for await (const msg of client.fetch({ uid: range }, { uid: true, source: true })) {
        if (msg.uid <= sinceUid) continue;        // gmail returns the anchor
        lastUid = Math.max(lastUid, msg.uid);
        const p = await simpleParser(msg.source);
        const m = {
          uid: msg.uid,
          from: p.from?.text || "",
          subject: p.subject || "(no subject)",
          at: (p.date || new Date()).toISOString(),
          text: (p.text || "").replace(/\s+\n/g, "\n").trim().slice(0, 4000),
        };
        if (!isJunk(m)) out.push(m);
      }
    } finally { lock.release(); }
    await client.logout();
  } catch (e) {
    // Never silently return an empty inbox — that reads as "quiet day".
    // imapflow's bare "Command failed" says nothing on its own. Carry every
    // field that might name the real cause, including the error code — a
    // timeout and a rejected password produce the same useless message.
    const detail = [
      e?.message,
      e?.responseText,
      e?.code && `code=${e.code}`,
      e?.serverResponseCode && `server=${e.serverResponseCode}`,
      e?.authenticationFailed && "authentication failed — check the app password",
    ].filter(Boolean).join(" | ");
    return { messages: out, lastUid, error: detail || String(e) };
  } finally {
    // logout() is a courtesy the server may never get to answer; close() is the
    // guarantee. Skipping it is what left the socket alive to time out later.
    try { client.close(); } catch {}
  }
  return { messages: out.slice(-max), lastUid, error: null };
}

let tx = null;
const transport = () =>
  (tx ||= nodemailer.createTransport({
    host: "smtp.gmail.com", port: 465, secure: true,
    auth: { user: USER, pass: PASS },
  }));

/** Send as the household. Callers are responsible for asking a human first. */
export async function send({ to, subject, body, replyTo }) {
  if (!mailReady()) throw new Error("mail not configured");
  return transport().sendMail({
    from: USER, to, subject, text: body,
    ...(replyTo ? { inReplyTo: replyTo, references: replyTo } : {}),
  });
}
